namespace PastPort.Application.DTOs.Response;

public class PayPalPaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ApprovalLink { get; set; }
    public string? OrderId { get; set; }
    public PaymentStatus Status { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Approved = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}