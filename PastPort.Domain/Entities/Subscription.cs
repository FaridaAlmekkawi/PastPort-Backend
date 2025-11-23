using PastPort.Domain.Enums;  

namespace PastPort.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
}