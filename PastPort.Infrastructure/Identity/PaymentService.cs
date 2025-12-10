using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;
using Stripe;


namespace PastPort.Infrastructure.Identity;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISavedPaymentMethodRepository _savedPaymentMethodRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IStripePaymentService _stripeService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ISavedPaymentMethodRepository savedPaymentMethodRepository,
        ISubscriptionRepository subscriptionRepository,
        IStripePaymentService stripeService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _savedPaymentMethodRepository = savedPaymentMethodRepository;
        _subscriptionRepository = subscriptionRepository;
        _stripeService = stripeService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        string userId,
        CreatePaymentIntentRequestDto request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            // Get or create Stripe customer
            var stripeCustomerId = await GetOrCreateStripeCustomerAsync(user);

            // Prepare metadata
            var metadata = request.Metadata ?? new Dictionary<string, string>();
            metadata["user_id"] = userId;

            if (request.SubscriptionId.HasValue)
                metadata["subscription_id"] = request.SubscriptionId.Value.ToString();

            // Create payment intent in Stripe
            var paymentIntentId = await _stripeService.CreatePaymentIntentAsync(
                request.Amount,
                request.Currency,
                stripeCustomerId,
                metadata
            );

            // Get payment intent details
            var paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);

            // Save payment record
            var payment = new Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = request.SubscriptionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = PaymentStatus.Pending,
                Method = Domain.Enums.PaymentMethod.CreditCard,
                Provider = PaymentProvider.Stripe,
                ProviderPaymentId = paymentIntentId,
                ProviderCustomerId = stripeCustomerId,
                Description = request.Description,
                SubtotalAmount = request.Amount,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            _logger.LogInformation("Payment intent created: {PaymentIntentId} for user {UserId}",
                paymentIntentId, userId);

            return new PaymentIntentResponseDto
            {
                PaymentIntentId = paymentIntentId,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = paymentIntent.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment intent for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PaymentResponseDto> ConfirmPaymentAsync(
        string userId,
        ConfirmPaymentRequestDto request)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentByProviderIdAsync(
                request.PaymentIntentId);

            if (payment == null)
                throw new Exception("Payment not found");

            if (payment.UserId != userId)
                throw new UnauthorizedAccessException("Unauthorized");

            // Confirm with Stripe
            var confirmed = await _stripeService.ConfirmPaymentIntentAsync(
                request.PaymentIntentId,
                request.PaymentMethodId ?? string.Empty
            );

            if (confirmed)
            {
                payment.Status = PaymentStatus.Succeeded;
                payment.PaidAt = DateTime.UtcNow;
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = "Payment confirmation failed";
            }

            await _paymentRepository.UpdateAsync(payment);

            // Update subscription if exists
            if (payment.SubscriptionId.HasValue)
            {
                await UpdateSubscriptionAfterPaymentAsync(
                    payment.SubscriptionId.Value,
                    confirmed);
            }

            // Generate invoice
            if (confirmed)
            {
                await GenerateInvoiceAsync(payment.Id);
            }

            return MapToPaymentResponseDto(payment, payment.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment {PaymentIntentId}",
                request.PaymentIntentId);
            throw;
        }
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(
        string userId,
        CreatePaymentRequestDto request)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId);
            if (subscription == null)
                throw new Exception("Subscription not found");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            // Get or create Stripe customer
            var stripeCustomerId = await GetOrCreateStripeCustomerAsync(user);

            // Attach payment method if requested
            if (request.SavePaymentMethod)
            {
                await _stripeService.AttachPaymentMethodAsync(
                    request.PaymentMethodId,
                    stripeCustomerId);
            }

            // Create and confirm payment intent
            var metadata = new Dictionary<string, string>
            {
                { "user_id", userId },
                { "subscription_id", subscription.Id.ToString() }
            };

            var paymentIntentId = await _stripeService.CreatePaymentIntentAsync(
                subscription.Price,
                "USD",
                stripeCustomerId,
                metadata
            );

            var confirmed = await _stripeService.ConfirmPaymentIntentAsync(
                paymentIntentId,
                request.PaymentMethodId
            );

            // Create payment record
            var payment = new Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = subscription.Id,
                Amount = subscription.Price,
                Currency = "USD",
                Status = confirmed ? PaymentStatus.Succeeded : PaymentStatus.Failed,
                Method = Domain.Enums.PaymentMethod.CreditCard,
                Provider = PaymentProvider.Stripe,
                ProviderPaymentId = paymentIntentId,
                ProviderCustomerId = stripeCustomerId,
                Description = $"Payment for {subscription.Plan} subscription",
                SubtotalAmount = subscription.Price,
                PaidAt = confirmed ? DateTime.UtcNow : null,
                FailedAt = confirmed ? null : DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            // Update subscription
            if (confirmed)
            {
                await UpdateSubscriptionAfterPaymentAsync(subscription.Id, true);
                await GenerateInvoiceAsync(payment.Id);

                // Save payment method
                if (request.SavePaymentMethod)
                {
                    await SavePaymentMethodInternalAsync(
                        userId,
                        request.PaymentMethodId,
                        request.SetAsDefault);
                }
            }

            return MapToPaymentResponseDto(payment, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ApiResponseDto> RefundPaymentAsync(
        string adminUserId,
        RefundRequestDto request)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
                return new ApiResponseDto { Success = false, Message = "Payment not found" };

            if (payment.Status != PaymentStatus.Succeeded)
                return new ApiResponseDto { Success = false, Message = "Payment cannot be refunded" };

            // Create refund in Stripe
            var refundAmount = request.Amount ?? payment.Amount;
            var stripeRefundId = await _stripeService.CreateRefundAsync(
                payment.ProviderPaymentId!,
                refundAmount
            );

            // Create refund record
            var refund = new Domain.Entities.Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Amount = refundAmount,
                Status = RefundStatus.Succeeded,
                Reason = request.Reason,
                ProviderRefundId = stripeRefundId,
                Notes = request.Notes,
                RequestedBy = adminUserId,
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            // Update payment status
            payment.Status = refundAmount >= payment.Amount
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundReason = request.Reason.ToString();

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Payment {PaymentId} refunded: {Amount}",
                payment.Id, refundAmount);

            return new ApiResponseDto
            {
                Success = true,
                Message = "Payment refunded successfully",
                Data = new { RefundId = refund.Id, Amount = refundAmount }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund payment {PaymentId}", request.PaymentId);
            return new ApiResponseDto { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<PaymentResponseDto>> GetUserPaymentsAsync(string userId)
    {
        var payments = await _paymentRepository.GetUserPaymentsAsync(userId);
        var user = await _userManager.FindByIdAsync(userId);

        return payments.Select(p => MapToPaymentResponseDto(p, user!)).ToList();
    }

    public async Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
            return null;

        var user = await _userManager.FindByIdAsync(payment.UserId);
        return MapToPaymentResponseDto(payment, user!);
    }

    public async Task<List<SavedPaymentMethodDto>> GetUserPaymentMethodsAsync(string userId)
    {
        var methods = await _savedPaymentMethodRepository.GetUserPaymentMethodsAsync(userId);
        return methods.Select(MapToPaymentMethodDto).ToList();
    }

    public async Task<SavedPaymentMethodDto> SavePaymentMethodAsync(
        string userId,
        string providerPaymentMethodId)
    {
        return await SavePaymentMethodInternalAsync(userId, providerPaymentMethodId, false);
    }

    public async Task<ApiResponseDto> DeletePaymentMethodAsync(string userId, Guid paymentMethodId)
    {
        try
        {
            var method = await _savedPaymentMethodRepository.GetByIdAsync(paymentMethodId);

            if (method == null || method.UserId != userId)
                return new ApiResponseDto { Success = false, Message = "Payment method not found" };

            // Detach from Stripe
            await _stripeService.DetachPaymentMethodAsync(method.ProviderPaymentMethodId);

            // Soft delete
            method.IsActive = false;
            method.DeletedAt = DateTime.UtcNow;
            await _savedPaymentMethodRepository.UpdateAsync(method);

            return new ApiResponseDto { Success = true, Message = "Payment method deleted" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete payment method {PaymentMethodId}",
                paymentMethodId);
            return new ApiResponseDto { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponseDto> SetDefaultPaymentMethodAsync(
        string userId,
        Guid paymentMethodId)
    {
        try
        {
            await _savedPaymentMethodRepository.SetDefaultPaymentMethodAsync(
                userId,
                paymentMethodId);

            return new ApiResponseDto
            {
                Success = true,
                Message = "Default payment method updated"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default payment method");
            return new ApiResponseDto { Success = false, Message = ex.Message };
        }
    }

    // Invoice methods implementation continued in next part...

    public async Task<List<InvoiceResponseDto>> GetUserInvoicesAsync(string userId)
    {
        var invoices = await _invoiceRepository.GetUserInvoicesAsync(userId);
        return invoices.Select(MapToInvoiceDto)
            .ToList();
    }

    public async Task<InvoiceResponseDto?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        var invoice = await _invoiceRepository.GetInvoiceByNumberAsync(invoiceNumber);

        // Ensure the correct type is passed to MapToInvoiceDto  
        if (invoice != null)
        {
            return MapToInvoiceDto(invoice);
        }

        return null;
    }

    public async Task<InvoiceResponseDto> GenerateInvoiceAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new Exception("Payment not found");

        var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();

        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            UserId = payment.UserId,
            PaymentId = paymentId,
            SubscriptionId = payment.SubscriptionId,
            Amount = payment.SubtotalAmount,
            TaxAmount = payment.TaxAmount ?? 0,
            TotalAmount = payment.Amount,
            Currency = payment.Currency,
            Status = InvoiceStatus.Paid,
            IssuedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            PaidAt = payment.PaidAt,
            Items = new List<Domain.Entities.InvoiceItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = payment.Description ?? "Subscription Payment",
                    Quantity = 1,
                    UnitPrice = payment.SubtotalAmount,
                    Amount = payment.SubtotalAmount
                }
            }
        };

        await _invoiceRepository.AddAsync(invoice);

        return MapToInvoiceDto(invoice);
    }

    public async Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddYears(-1);
        endDate ??= DateTime.UtcNow;

        var payments = await _paymentRepository.GetPaymentsByDateRangeAsync(
            startDate.Value,
            endDate.Value);

        var totalRevenue = await _paymentRepository.GetTotalRevenueAsync();
        var monthlyRevenue = await _paymentRepository.GetRevenueByDateRangeAsync(
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow);

        return new PaymentStatisticsDto
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            TotalPayments = payments.Count(),
            SuccessfulPayments = payments.Count(p => p.Status == PaymentStatus.Succeeded),
            FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed),
            RefundedPayments = payments.Count(p => p.Status == PaymentStatus.Refunded),
            AveragePaymentAmount = payments.Any() ? payments.Average(p => p.Amount) : 0
        };
    }

    public Task HandlePaymentWebhookAsync(string payload, string signature)
    {
        // Implemented in WebhookController
        throw new NotImplementedException("Use WebhookController");
    }

    // Helper Methods
    private async Task<string> GetOrCreateStripeCustomerAsync(ApplicationUser user)
    {
        // Check if user already has Stripe customer ID (you need to add this field to User)
        // For now, create new customer
        return await _stripeService.CreateCustomerAsync(
            user.Id,
            user.Email!,
            $"{user.FirstName} {user.LastName}"
        );
    }

    private async Task UpdateSubscriptionAfterPaymentAsync(Guid subscriptionId, bool success)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null) return;

        if (success)
        {
            subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        }

        await _subscriptionRepository.UpdateAsync(subscription);
    }

    private async Task<SavedPaymentMethodDto> SavePaymentMethodInternalAsync(
        string userId,
        string providerPaymentMethodId,
        bool setAsDefault)
    {
        var paymentMethod = await _stripeService.GetPaymentMethodAsync(providerPaymentMethodId);

        SavedPaymentMethod saved = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = PaymentProvider.Stripe,
            ProviderPaymentMethodId = providerPaymentMethodId,
            Type = Domain.Enums.PaymentMethod.CreditCard,
            CardLast4 = paymentMethod.Card?.Last4,
            CardBrand = paymentMethod.Card?.Brand,
            CardExpMonth = paymentMethod.Card?.ExpMonth,
            CardExpYear = paymentMethod.Card?.ExpYear,
            IsDefault = setAsDefault,
            CreatedAt = DateTime.UtcNow
        };

        await _savedPaymentMethodRepository.AddAsync(saved);

        if (setAsDefault)
        {
            await _savedPaymentMethodRepository.SetDefaultPaymentMethodAsync(userId, saved.Id);
        }

        return MapToPaymentMethodDto(saved);
    }

    private static PaymentResponseDto MapToPaymentResponseDto(
        Domain.Entities.Payment payment,
        ApplicationUser user)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            UserEmail = user.Email ?? string.Empty,
            SubscriptionId = payment.SubscriptionId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            Method = payment.Method.ToString(),
            Provider = payment.Provider.ToString(),
            Description = payment.Description,
            InvoiceUrl = payment.InvoiceUrl,
            ReceiptUrl = payment.ReceiptUrl,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt,
            FailureReason = payment.FailureReason
        };
    }

    private static SavedPaymentMethodDto MapToPaymentMethodDto(SavedPaymentMethod method)
    {
        return new SavedPaymentMethodDto
        {
            Id = method.Id,
            Type = method.Type.ToString(),
            Provider = method.Provider.ToString(),
            CardBrand = method.CardBrand,
            CardLast4 = method.CardLast4,
            CardExpMonth = method.CardExpMonth,
            CardExpYear = method.CardExpYear,
            IsDefault = method.IsDefault,
            IsActive = method.IsActive,
            CreatedAt = method.CreatedAt
        };
    }

    private static InvoiceResponseDto MapToInvoiceDto(Domain.Entities.Invoice invoice)
    {
        return new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            UserId = invoice.UserId,
            UserEmail = invoice.User?.Email ?? string.Empty,
            Amount = invoice.Amount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            Status = invoice.Status.ToString(),
            IssuedAt = invoice.IssuedAt,
            DueDate = invoice.DueDate,
            PaidAt = invoice.PaidAt,
            PdfUrl = invoice.PdfUrl,
            Items = invoice.Items.Select(i => new InvoiceItemDto
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Amount = i.Amount
            }).ToList()
        };

    }
    private readonly IPayPalPaymentService _paypalService;

    public PaymentService(
        // ... existing parameters
        IPayPalPaymentService paypalService)
    {
        // ... existing assignments
        _paypalService = paypalService;
    }
    public async Task<PayPalOrderResponseDto> CreatePayPalOrderAsync(
    string userId,
    CreatePaymentIntentRequestDto request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            // Build return URLs
            var returnUrl = $"{_settings.FrontendUrl}/payment/paypal/success";
            var cancelUrl = $"{_settings.FrontendUrl}/payment/paypal/cancel";

            // Prepare metadata
            var metadata = new Dictionary<string, string>
        {
            { "user_id", userId },
            { "description", request.Description ?? "PastPort Subscription" }
        };

            if (request.SubscriptionId.HasValue)
                metadata["subscription_id"] = request.SubscriptionId.Value.ToString();

            // Create PayPal order
            var orderId = await _paypalService.CreateOrderAsync(
                request.Amount,
                request.Currency,
                returnUrl,
                cancelUrl,
                metadata
            );

            // Get order details to get approval URL
            var orderDetails = await _paypalService.GetOrderDetailsAsync(orderId);
            var approvalUrl = orderDetails.Links
                .FirstOrDefault(l => l.Rel == "approve")?.Href;

            // Save payment record
            var payment = new Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = request.SubscriptionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.PayPal,
                Provider = PaymentProvider.PayPal,
                ProviderPaymentId = orderId,
                Description = request.Description,
                SubtotalAmount = request.Amount,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            _logger.LogInformation("PayPal order created: {OrderId} for user {UserId}",
                orderId, userId);

            return new PayPalOrderResponseDto
            {
                OrderId = orderId,
                ApprovalUrl = approvalUrl ?? string.Empty,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "CREATED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayPal order for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PaymentResponseDto> CapturePayPalOrderAsync(
        string userId,
        string orderId)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentByProviderIdAsync(orderId);

            if (payment == null)
                throw new Exception("Payment not found");

            if (payment.UserId != userId)
                throw new UnauthorizedAccessException("Unauthorized");

            // Capture payment with PayPal
            var captureResponse = await _paypalService.CaptureOrderAsync(orderId);

            var captureStatus = captureResponse.Status.ToUpper();
            var captureId = captureResponse.PurchaseUnits
                .FirstOrDefault()?.Payments?.Captures?.FirstOrDefault()?.Id;

            if (captureStatus == "COMPLETED")
            {
                payment.Status = PaymentStatus.Succeeded;
                payment.PaidAt = DateTime.UtcNow;
                payment.ProviderPaymentId = captureId ?? orderId;
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = $"PayPal capture failed: {captureStatus}";
            }

            await _paymentRepository.UpdateAsync(payment);

            // Update subscription if exists
            if (payment.SubscriptionId.HasValue && captureStatus == "COMPLETED")
            {
                await UpdateSubscriptionAfterPaymentAsync(payment.SubscriptionId.Value, true);
                await GenerateInvoiceAsync(payment.Id);
            }

            _logger.LogInformation("PayPal order captured: {OrderId} - Status: {Status}",
                orderId, captureStatus);

            return MapToPaymentResponseDto(payment, payment.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture PayPal order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<ApiResponseDto> RefundPayPalPaymentAsync(
        string adminUserId,
        RefundRequestDto request)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
                return new ApiResponseDto { Success = false, Message = "Payment not found" };

            if (payment.Provider != PaymentProvider.PayPal)
                return new ApiResponseDto { Success = false, Message = "Not a PayPal payment" };

            if (payment.Status != PaymentStatus.Succeeded)
                return new ApiResponseDto { Success = false, Message = "Payment cannot be refunded" };

            // Create refund in PayPal
            var refundAmount = request.Amount ?? payment.Amount;
            var captureId = payment.ProviderPaymentId!;

            var paypalRefundId = await _paypalService.CreateRefundAsync(
                captureId,
                refundAmount
            );

            // Create refund record
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Amount = refundAmount,
                Status = RefundStatus.Succeeded,
                Reason = request.Reason,
                ProviderRefundId = paypalRefundId,
                Notes = request.Notes,
                RequestedBy = adminUserId,
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            // Update payment status
            payment.Status = refundAmount >= payment.Amount
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundReason = request.Reason.ToString();

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("PayPal payment {PaymentId} refunded: {Amount}",
                payment.Id, refundAmount);

            return new ApiResponseDto
            {
                Success = true,
                Message = "PayPal payment refunded successfully",
                Data = new { RefundId = refund.Id, PayPalRefundId = paypalRefundId, Amount = refundAmount }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund PayPal payment {PaymentId}", request.PaymentId);
            return new ApiResponseDto { Success = false, Message = ex.Message };
        }
    }
}