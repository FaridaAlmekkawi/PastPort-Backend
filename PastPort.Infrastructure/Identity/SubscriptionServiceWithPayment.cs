using Microsoft.Extensions.Logging;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;

namespace PastPort.Application.Identity;

public class SubscriptionServiceWithPayment : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;
    private readonly ILogger<SubscriptionServiceWithPayment> _logger;

    public SubscriptionServiceWithPayment(
        ISubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        IPaymentService paymentService,
        IUserService userService,
        ILogger<SubscriptionServiceWithPayment> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
        _userService = userService;
        _logger = logger;
    }

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
            // حساب السعر
            var price = CalculatePrice(subscriptionRequest.Plan, subscriptionRequest.DurationInMonths);

            // إنشاء أمر PayPal
            var paymentResult = await _paymentService.CreateOrderAsync(
                userId,
                paymentRequest,
                price);

            if (!paymentResult.Success)
            {
                _logger.LogError("Failed to create PayPal order for user {UserId}", userId);
                return paymentResult;
            }

            // حفظ سجل الدفع المعلق
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PayPalOrderId = paymentResult.OrderId ?? string.Empty,
                PayerEmail = paymentRequest.PayerEmail,
                PayerName = paymentRequest.PayerName,
                Amount = price,
                Status = PaymentStatus.Pending
            };

            await _paymentRepository.AddAsync(payment);

            _logger.LogInformation("Payment initiated for user {UserId}, OrderId: {OrderId}",
                userId, paymentResult.OrderId);

            return paymentResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return new PayPalPaymentResponseDto
            {
                Success = false,
                Message = $"Error: {ex.Message}"
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
            // التقاط الدفع من PayPal
            var captureResult = await _paymentService.CaptureOrderAsync(payPalOrderId);

            if (!captureResult.Success)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Payment capture failed"
                };
            }

            // تحديث سجل الدفع
            var payment = await _paymentRepository.GetPaymentByPayPalOrderIdAsync(payPalOrderId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            // إنشاء الاشتراك
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Plan = subscriptionRequest.Plan,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(subscriptionRequest.DurationInMonths),
                Price = payment?.Amount ?? 0,
                StripeSubscriptionId = payPalOrderId // نستخدم PayPal Order ID
            };

            await _subscriptionRepository.AddAsync(subscription);

            _logger.LogInformation("Subscription created for user {UserId} after PayPal payment", userId);

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

    // ... باقي الـ methods السابقة

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
}