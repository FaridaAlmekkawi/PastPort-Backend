using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class PayPalPaymentRequestDto
{
    [Required]
    public Guid SubscriptionPlanId { get; set; }

    [Required]
    public int DurationInMonths { get; set; }

    [Required]
    [EmailAddress]
    public string PayerEmail { get; set; } = string.Empty;

    [Required]
    public string PayerName { get; set; } = string.Empty;

    [Required]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required]
    public string CancelUrl { get; set; } = string.Empty;
}

public class PayPalApprovalDto
{
    [Required]
    public string OrderId { get; set; } = string.Empty;
}

public class PayPalPaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ApprovalLink { get; set; } // URL تحويل المستخدم
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