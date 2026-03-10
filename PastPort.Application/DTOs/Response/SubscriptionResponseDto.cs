using PastPort.Domain.Enums;

namespace PastPort.Application.DTOs.Response;

public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsActive { get; set; }
}

public class SubscriptionPlanInfoDto
{
    public SubscriptionPlan Plan { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public List<string> Features { get; set; } = new();
}