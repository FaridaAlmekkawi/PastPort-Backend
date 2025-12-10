using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// إنشاء أمر دفع PayPal
    /// </summary>
    Task<PayPalPaymentResponseDto> CreateOrderAsync(
        string userId,
        PayPalPaymentRequestDto request,
        decimal amount);

    /// <summary>
    /// التقاط الدفع بعد موافقة المستخدم
    /// </summary>
    Task<PayPalPaymentResponseDto> CaptureOrderAsync(string orderId);

    /// <summary>
    /// الحصول على تفاصيل الأمر
    /// </summary>
    Task<PayPalPaymentResponseDto> GetOrderDetailsAsync(string orderId);
}