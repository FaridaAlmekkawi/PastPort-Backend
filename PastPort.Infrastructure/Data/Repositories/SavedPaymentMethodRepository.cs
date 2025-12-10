using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;
using PastPort.Infrastructure.Data.Repositories;
using PastPort.Infrastructure.Data;

public class SavedPaymentMethodRepository : Repository<SavedPaymentMethod>,
    ISavedPaymentMethodRepository
{
    public SavedPaymentMethodRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SavedPaymentMethod>> GetUserPaymentMethodsAsync(string userId)
    {
        return await _dbSet
            .Where(spm => spm.UserId == userId && spm.IsActive)
            .OrderByDescending(spm => spm.IsDefault)
            .ThenByDescending(spm => spm.CreatedAt)
            .ToListAsync();
    }

    public async Task<SavedPaymentMethod?> GetDefaultPaymentMethodAsync(string userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(spm => spm.UserId == userId
                && spm.IsDefault
            && spm.IsActive);
    }

    public async Task SetDefaultPaymentMethodAsync(string userId, Guid paymentMethodId)
    {
        // Remove default from all other payment methods
        var userPaymentMethods = await _dbSet
            .Where(spm => spm.UserId == userId)
            .ToListAsync();

        foreach (var method in userPaymentMethods)
        {
            method.IsDefault = method.Id == paymentMethodId;
        }

        await _context.SaveChangesAsync();
    }
}