using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PastPort.Application.Interfaces;
using Stripe;


namespace PastPort.Infrastructure.ExternalServices.Payment;

public class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Configure Stripe
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<string> CreateCustomerAsync(string userId, string email, string name)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId }
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation("Stripe customer created: {CustomerId} for user {UserId}",
                customer.Id, userId);

            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string customerId,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency.ToLower(),
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Payment intent created: {PaymentIntentId} for {Amount} {Currency}",
                paymentIntent.Id, amount, currency);

            return paymentIntent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment intent");
            throw;
        }
    }

    public async Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId, string paymentMethodId)
    {
        try
        {
            var options = new PaymentIntentConfirmOptions
            {
                PaymentMethod = paymentMethodId
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId, options);

            var success = paymentIntent.Status == "succeeded";

            _logger.LogInformation("Payment intent {PaymentIntentId} confirmed: {Status}",
                paymentIntentId, paymentIntent.Status);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment intent {PaymentIntentId}",
                paymentIntentId);
            return false;
        }
    }

    public async Task<string> CreateRefundAsync(string paymentIntentId, decimal? amount = null)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            _logger.LogInformation("Refund created: {RefundId} for payment {PaymentIntentId}",
                refund.Id, paymentIntentId);

            return refund.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create refund for {PaymentIntentId}",
                paymentIntentId);
            throw;
        }
    }

    public async Task<string> AttachPaymentMethodAsync(string paymentMethodId, string customerId)
    {
        try
        {
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            var service = new PaymentMethodService();
            var paymentMethod = await service.AttachAsync(paymentMethodId, options);

            _logger.LogInformation("Payment method {PaymentMethodId} attached to customer {CustomerId}",
                paymentMethodId, customerId);

            return paymentMethod.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach payment method {PaymentMethodId}",
                paymentMethodId);
            throw;
        }
    }

    public async Task<bool> DetachPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            var service = new PaymentMethodService();
            await service.DetachAsync(paymentMethodId);

            _logger.LogInformation("Payment method {PaymentMethodId} detached", paymentMethodId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detach payment method {PaymentMethodId}",
                paymentMethodId);
            return false;
        }
    }

    public async Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId)
    {
        var service = new PaymentMethodService();
        return await service.GetAsync(paymentMethodId);
    }

    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        return await service.GetAsync(paymentIntentId);
    }
}

