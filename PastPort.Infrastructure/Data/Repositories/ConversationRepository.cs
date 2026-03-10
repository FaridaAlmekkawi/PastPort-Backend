using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.Infrastructure.Data.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Conversation>> GetConversationsByCharacterAsync(Guid characterId)
    {
        return await _dbSet
            .Where(c => c.CharacterId == characterId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsWithCharacterAsync(
        string userId,
        Guid characterId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }
}