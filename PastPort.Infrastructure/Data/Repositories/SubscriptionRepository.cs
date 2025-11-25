using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;

namespace PastPort.Infrastructure.Data.Repositories;

public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId
                && s.Status == SubscriptionStatus.Active
                && s.EndDate > DateTime.UtcNow);
    }

    public async Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(string userId)
    {
        return await _dbSet
            .Where(s => s.UserId == userId)
            .Include(s => s.User)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsByStatusAsync(SubscriptionStatus status)
    {
        return await _dbSet
            .Where(s => s.Status == status)
            .Include(s => s.User)
            .ToListAsync();
    }

   
    public async Task<IEnumerable<Subscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);

        return await _dbSet
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.EndDate <= expiryDate
                && s.EndDate > DateTime.UtcNow)
            .Include(s => s.User)
            .ToListAsync();
    }
}