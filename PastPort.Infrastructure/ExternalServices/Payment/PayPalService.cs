using PayPalCheckoutSdk.Orders;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface IPaymentService
{
    Task<PayPalPaymentResponseDto> CreateOrderAsync(
        string userId,
        PayPalPaymentRequestDto request,
        decimal amount);

    Task<PayPalPaymentResponseDto> CaptureOrderAsync(string orderId);

    Task<Order?> GetOrderDetailsAsync(string orderId);
}