public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    public Guid? SubscriptionId { get; set; }
    public string? SubscriptionPlan { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? InvoiceUrl { get; set; }
    public string? ReceiptUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public string? FailureReason { get; set; }
}