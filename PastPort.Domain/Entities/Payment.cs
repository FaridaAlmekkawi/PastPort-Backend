using PastPort.Domain.Enums;

namespace PastPort.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }

    // User & Subscription
    public string UserId { get; set; } = string.Empty;
    public Guid? SubscriptionId { get; set; }

    // Payment Provider (PayPal / Stripe)
    public string ProviderPaymentId { get; set; } = string.Empty;

    // PayPal Details
    public string PayPalOrderId { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;

    // Amount Breakdown
    public decimal Amount { get; set; }         
    public decimal SubtotalAmount { get; set; } 
    public decimal TaxAmount { get; set; }      
    public decimal DiscountAmount { get; set; } 
    public string Currency { get; set; } = "USD";

    // Status
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Subscription? Subscription { get; set; }
}
