using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;

namespace PastPort.Application.Identity;

public class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    IPaymentRepository paymentRepository,
    IPaymentService paymentService,
    UserManager<ApplicationUser> userManager,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<SubscriptionService> _logger = logger;
    private object? subscription;

    /// <summary>
    /// بدء عملية الدفع عبر PayPal
    /// </summary>
    public async Task<PayPalPaymentResponseDto> InitiatePaymentAsync(
        string userId,
        CreateSubscriptionRequestDto subscriptionRequest,
        PayPalPaymentRequestDto paymentRequest)
    {
        try
        {
            var price = CalculatePrice(subscriptionRequest.Plan, subscriptionRequest.DurationInMonths);

            var paymentResult = await _paymentService.CreateOrderAsync(
                userId,
                paymentRequest,
                price);

            if (!paymentResult.Success)
                return paymentResult;

            // حفظ سجل الدفع
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PayPalOrderId = paymentResult.OrderId ?? string.Empty,
                PayerEmail = paymentRequest.PayerEmail,
                PayerName = paymentRequest.PayerName,
                Amount = price,
                Status = (Domain.Enums.PaymentStatus)DTOs.Response.PaymentStatus.Pending
            };

            await _paymentRepository.AddAsync(payment);

            _logger.LogInformation("Payment initiated for user {UserId}", userId);
            return paymentResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return new PayPalPaymentResponseDto
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Status = DTOs.Response.PaymentStatus.Failed
            };
        }
    }

    /// <summary>
    /// إكمال الاشتراك بعد نجاح الدفع
    /// </summary>
    public async Task<ApiResponseDto> CompletePaymentAsync(
        string userId,
        string payPalOrderId,
        CreateSubscriptionRequestDto subscriptionRequest)
    {
        try
        {
            var captureResult = await _paymentService.CaptureOrderAsync(payPalOrderId);

            if (!captureResult.Success)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Payment capture failed"
                };
            }

            var payment = await _paymentRepository.GetPaymentByPayPalOrderIdAsync(payPalOrderId);
            if (payment != null)
            {
                payment.Status = (Domain.Enums.PaymentStatus)DTOs.Response.PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Plan = subscriptionRequest.Plan,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(subscriptionRequest.DurationInMonths),
                Price = payment?.Amount ?? 0,
                StripeSubscriptionId = payPalOrderId
            };

            await _subscriptionRepository.AddAsync(subscription);

            _logger.LogInformation("Subscription created for user {UserId}", userId);

            return new ApiResponseDto
            {
                Success = true,
                Message = "Payment completed and subscription activated",
                Data = new { subscriptionId = subscription.Id }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment");
            return new ApiResponseDto
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    // الـ Methods الأخرى بدون تغيير
    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
        string userId,
        CreateSubscriptionRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        var price = CalculatePrice(request.Plan, request.DurationInMonths);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Plan = request.Plan,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(request.DurationInMonths),
            Price = price,
            StripeSubscriptionId = string.Empty
        };

        await _subscriptionRepository.AddAsync(subscription);

        return MapToResponseDto(subscription, user);
    }

    public async Task<SubscriptionResponseDto?> GetActiveSubscriptionAsync(string userId)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        if (subscription == null)
            return null;

        return MapToResponseDto(subscription, subscription.User);
    }

    public async Task<List<SubscriptionResponseDto>> GetUserSubscriptionsAsync(string userId)
    {
        var subscriptions = await _subscriptionRepository.GetUserSubscriptionsAsync(userId);
        return subscriptions.Select(s => MapToResponseDto(s, s.User)).ToList();
    }

    public async Task<bool> CancelSubscriptionAsync(string userId)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        if (subscription == null)
            throw new Exception("No active subscription found");

        subscription.Status = SubscriptionStatus.Cancelled;
        await _subscriptionRepository.UpdateAsync(subscription);

        return true;
    }

    public Task<List<SubscriptionPlanInfoDto>> GetAvailablePlansAsync()
    {
        var plans = new List<SubscriptionPlanInfoDto>
        {
            new()
            {
                Plan = SubscriptionPlan.Free,
                Name = "Free",
                Description = "Basic access",
                MonthlyPrice = 0,
                YearlyPrice = 0,
                Features = new() { "5 scenes", "Basic support" }
            },
            new()
            {
                Plan = SubscriptionPlan.Individual,
                Name = "Individual",
                Description = "Full access",
                MonthlyPrice = 9.99m,
                YearlyPrice = 99.99m,
                Features = new() { "All scenes", "HD quality", "Priority support" }
            },
            new()
            {
                Plan = SubscriptionPlan.School,
                Name = "School",
                Description = "Educational",
                MonthlyPrice = 49.99m,
                YearlyPrice = 499.99m,
                Features = new() { "50 students", "Dashboard", "Custom content" }
            },
            new()
            {
                Plan = SubscriptionPlan.Museum,
                Name = "Museum",
                Description = "Museums",
                MonthlyPrice = 199.99m,
                YearlyPrice = 1999.99m,
                Features = new() { "Unlimited visitors", "Analytics", "24/7 support" }
            },
            new()
            {
                Plan = SubscriptionPlan.Enterprise,
                Name = "Enterprise",
                Description = "Large organizations",
                MonthlyPrice = 999.99m,
                YearlyPrice = 9999.99m,
                Features = new() { "White-label", "API access", "Dedicated manager" }
            }
        };

        return Task.FromResult(plans);
    }

    public async Task<bool> CheckSubscriptionAccessAsync(string userId, SubscriptionPlan requiredPlan)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

        if (subscription == null)
            return requiredPlan == SubscriptionPlan.Free;

        return subscription.Plan >= requiredPlan;
    }

    private static decimal CalculatePrice(SubscriptionPlan plan, int months)
    {
        var monthlyPrices = new Dictionary<SubscriptionPlan, decimal>
        {
            { SubscriptionPlan.Free, 0 },
            { SubscriptionPlan.Individual, 9.99m },
            { SubscriptionPlan.School, 49.99m },
            { SubscriptionPlan.Museum, 199.99m },
            { SubscriptionPlan.Enterprise, 999.99m }
        };

        var monthlyPrice = monthlyPrices[plan];
        var totalPrice = monthlyPrice * months;

        if (months >= 12)
            totalPrice *= 0.8m;

        return totalPrice;
    }

    private static SubscriptionResponseDto MapToResponseDto(Subscription subscription, ApplicationUser user)
    {
        var daysRemaining = (subscription.EndDate - DateTime.UtcNow).Days;

        return new SubscriptionResponseDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            UserEmail = user.Email ?? string.Empty,
            Plan = subscription.Plan,
            PlanName = subscription.Plan.ToString(),
            Status = subscription.Status,
            StatusName = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Price = subscription.Price,
            DaysRemaining = daysRemaining > 0 ? daysRemaining : 0,
            IsActive = subscription.Status == SubscriptionStatus.Active && subscription.EndDate > DateTime.UtcNow
        };
    }
    public CompletePaymentResponseDto CompletePayment(
       string userId,
       string orderId,
       SubscriptionRequestDto request)
    {
        // Ensure the `subscription` object is properly initialized and cast to the correct type
        if (subscription is not Subscription validSubscription)
        {
            throw new InvalidOperationException("Subscription object is not properly initialized.");
        }

        return new CompletePaymentResponseDto
        {
            Success = true,
            Message = "Payment completed",
            SubscriptionId = validSubscription.Id.ToString()
        };
    }

    Task<object?> ISubscriptionService.CompletePaymentAsync(string userId, string orderId, CreateSubscriptionRequestDto subscriptionRequest)
    {
        throw new NotImplementedException();
    }

    Task<object?> ISubscriptionService.InitiatePaymentAsync(string userId, CreateSubscriptionRequestDto subscriptionRequest, PayPalPaymentRequestDto paymentRequest)
    {
        throw new NotImplementedException();
    }

    Task<CompletePaymentResponseDto> ISubscriptionService.CompletePayment(string userId, string orderId, SubscriptionRequestDto request)
    {
        throw new NotImplementedException();
    }
}