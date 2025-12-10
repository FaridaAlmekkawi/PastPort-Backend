using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface IPaymentService
{
    // Payment Intent (for Stripe Checkout)
    Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        string userId,
        CreatePaymentIntentRequestDto request);

    Task<PaymentResponseDto> ConfirmPaymentAsync(
        string userId,
        ConfirmPaymentRequestDto request);

    // Direct Payment
    Task<PaymentResponseDto> ProcessPaymentAsync(
        string userId,
        CreatePaymentRequestDto request);

    // Refunds
    Task<ApiResponseDto> RefundPaymentAsync(
        string adminUserId,
        RefundRequestDto request);

    // Payment History
    Task<List<PaymentResponseDto>> GetUserPaymentsAsync(string userId);
    Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid paymentId);

    // Payment Methods
    Task<List<SavedPaymentMethodDto>> GetUserPaymentMethodsAsync(string userId);
    Task<SavedPaymentMethodDto> SavePaymentMethodAsync(
        string userId,
        string providerPaymentMethodId);
    Task<ApiResponseDto> DeletePaymentMethodAsync(string userId, Guid paymentMethodId);
    Task<ApiResponseDto> SetDefaultPaymentMethodAsync(string userId, Guid paymentMethodId);

    // Invoices
    Task<List<InvoiceResponseDto>> GetUserInvoicesAsync(string userId);
    Task<InvoiceResponseDto?> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<InvoiceResponseDto> GenerateInvoiceAsync(Guid paymentId);

    // Admin: Statistics
    Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Webhooks (for Stripe events)
    Task HandlePaymentWebhookAsync(string payload, string signature);
}
