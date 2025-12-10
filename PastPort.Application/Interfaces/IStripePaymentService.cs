using PastPort.Domain.Enums;
using Stripe;
using PastPort.Domain.Entities;

public interface IStripePaymentService
{
    Task<string> CreateCustomerAsync(string userId, string email, string name);

    Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string customerId,
        Dictionary<string, string>? metadata = null);

    Task<bool> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        string paymentMethodId);

    Task<string> CreateRefundAsync(
        string paymentIntentId,
        decimal? amount = null);

    Task<string> AttachPaymentMethodAsync(
        string paymentMethodId,
        string customerId);

    Task<bool> DetachPaymentMethodAsync(string paymentMethodId);

     Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);

    Task<PastPort.Domain.Enums.PaymentMethod> GetPaymentMethodAsync(string paymentMethodId);
}
