using Microsoft.AspNetCore.Identity;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;

namespace PastPort.Application.Identity;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        UserManager<ApplicationUser> userManager)
    {
        _subscriptionRepository = subscriptionRepository;
        _userManager = userManager;
    }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
        string userId,
        CreateSubscriptionRequestDto request)
    {
        // Check if user has active subscription
        var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        if (activeSubscription != null)
        {
            throw new Exception("User already has an active subscription");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        // Calculate price based on plan and duration
        var price = CalculatePrice(request.Plan, request.DurationInMonths);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Plan = request.Plan,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(request.DurationInMonths),
            Price = price,
            StripeSubscriptionId = string.Empty // سيتم ربطه مع Stripe لاحقاً
        };

        await _subscriptionRepository.AddAsync(subscription);

        return MapToResponseDto(subscription, user);
    }

    public async Task<SubscriptionResponseDto?> GetActiveSubscriptionAsync(string userId)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        if (subscription == null)
            return null;

        return MapToResponseDto(subscription, subscription.User);
    }

    public async Task<List<SubscriptionResponseDto>> GetUserSubscriptionsAsync(string userId)
    {
        var subscriptions = await _subscriptionRepository.GetUserSubscriptionsAsync(userId);
        return subscriptions.Select(s => MapToResponseDto(s, s.User)).ToList();
    }

    public async Task<bool> CancelSubscriptionAsync(string userId)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        if (subscription == null)
            throw new Exception("No active subscription found");

        subscription.Status = SubscriptionStatus.Cancelled;
        await _subscriptionRepository.UpdateAsync(subscription);

        return true;
    }

    public Task<List<SubscriptionPlanInfoDto>> GetAvailablePlansAsync()
    {
        var plans = new List<SubscriptionPlanInfoDto>
        {
            new()
            {
                Plan = SubscriptionPlan.Free,
                Name = "Free",
                Description = "Basic access to historical scenes",
                MonthlyPrice = 0,
                YearlyPrice = 0,
                Features = new()
                {
                    "Access to 5 historical scenes",
                    "Limited character interactions",
                    "Standard resolution",
                    "Community support"
                }
            },
            new()
            {
                Plan = SubscriptionPlan.Individual,
                Name = "Individual",
                Description = "Full access for personal use",
                MonthlyPrice = 9.99m,
                YearlyPrice = 99.99m,
                Features = new()
                {
                    "Access to all historical scenes",
                    "Unlimited character interactions",
                    "HD resolution",
                    "Priority support",
                    "Offline mode"
                }
            },
            new()
            {
                Plan = SubscriptionPlan.School,
                Name = "School",
                Description = "Educational institutions package",
                MonthlyPrice = 49.99m,
                YearlyPrice = 499.99m,
                Features = new()
                {
                    "Up to 50 student accounts",
                    "Teacher dashboard",
                    "Custom educational content",
                    "Progress tracking",
                    "Dedicated support"
                }
            },
            new()
            {
                Plan = SubscriptionPlan.Museum,
                Name = "Museum",
                Description = "Museums and cultural centers",
                MonthlyPrice = 199.99m,
                YearlyPrice = 1999.99m,
                Features = new()
                {
                    "Unlimited visitor access",
                    "Custom branded experience",
                    "Exhibition mode",
                    "Analytics dashboard",
                    "24/7 premium support"
                }
            },
            new()
            {
                Plan = SubscriptionPlan.Enterprise,
                Name = "Enterprise",
                Description = "Large organizations and institutions",
                MonthlyPrice = 999.99m,
                YearlyPrice = 9999.99m,
                Features = new()
                {
                    "Unlimited users",
                    "White-label solution",
                    "Custom development",
                    "API access",
                    "Dedicated account manager",
                    "SLA guarantee"
                }
            }
        };

        return Task.FromResult(plans);
    }

    public async Task<bool> CheckSubscriptionAccessAsync(string userId, SubscriptionPlan requiredPlan)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

        if (subscription == null)
            return requiredPlan == SubscriptionPlan.Free;

        return subscription.Plan >= requiredPlan;
    }

    private static decimal CalculatePrice(SubscriptionPlan plan, int months)
    {
        var monthlyPrices = new Dictionary<SubscriptionPlan, decimal>
        {
            { SubscriptionPlan.Free, 0 },
            { SubscriptionPlan.Individual, 9.99m },
            { SubscriptionPlan.School, 49.99m },
            { SubscriptionPlan.Museum, 199.99m },
            { SubscriptionPlan.Enterprise, 999.99m }
        };

        var monthlyPrice = monthlyPrices[plan];
        var totalPrice = monthlyPrice * months;

        // خصم 20% للاشتراك السنوي
        if (months >= 12)
            totalPrice *= 0.8m;

        return totalPrice;
    }

    private static SubscriptionResponseDto MapToResponseDto(Subscription subscription, ApplicationUser user)
    {
        var daysRemaining = (subscription.EndDate - DateTime.UtcNow).Days;

        return new SubscriptionResponseDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            UserEmail = user.Email ?? string.Empty,
            Plan = subscription.Plan,
            PlanName = subscription.Plan.ToString(),
            Status = subscription.Status,
            StatusName = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Price = subscription.Price,
            DaysRemaining = daysRemaining > 0 ? daysRemaining : 0,
            IsActive = subscription.Status == SubscriptionStatus.Active && subscription.EndDate > DateTime.UtcNow
        };
    }
}