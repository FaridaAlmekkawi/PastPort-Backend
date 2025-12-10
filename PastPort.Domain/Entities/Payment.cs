
using PastPort.Domain.Enums;

namespace PastPort.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }

    // PayPal Details
    public string PayPalOrderId { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;

    // Payment Info
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
}