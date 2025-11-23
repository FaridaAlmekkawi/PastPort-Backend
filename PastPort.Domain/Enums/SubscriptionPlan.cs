namespace PastPort.Domain.Enums;

public enum SubscriptionPlan
{
    Free = 0,
    Individual = 1,
    School = 2,
    Museum = 3,
    Enterprise = 4
}

public enum SubscriptionStatus
{
    Active = 0,
    Cancelled = 1,
    Expired = 2,
    PendingPayment = 3
}