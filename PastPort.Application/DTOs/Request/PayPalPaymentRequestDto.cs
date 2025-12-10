using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class PayPalPaymentRequestDto
{
    [Required]
    public Guid SubscriptionPlanId { get; set; }

    [Required]
    public int DurationInMonths { get; set; } = 1;

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