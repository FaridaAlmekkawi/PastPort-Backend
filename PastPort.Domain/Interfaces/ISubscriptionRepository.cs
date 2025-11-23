using PastPort.Domain.Entities;
using PastPort.Domain.Enums;

namespace PastPort.Domain.Interfaces;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId);
    Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(string userId);
    Task<IEnumerable<Subscription>> GetSubscriptionsByStatusAsync(SubscriptionStatus status);
    Task<IEnumerable<Subscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry);
}