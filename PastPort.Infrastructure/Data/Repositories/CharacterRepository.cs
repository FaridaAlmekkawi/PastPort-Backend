using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.Infrastructure.Data.Repositories;

public class CharacterRepository : Repository<Character>, ICharacterRepository
{
    public CharacterRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Character>> GetCharactersBySceneIdAsync(Guid sceneId)
    {
        return await _dbSet
            .Where(c => c.SceneId == sceneId)
            .Include(c => c.Scene)
            .ToListAsync();
    }

    public async Task<Character?> GetCharacterWithSceneAsync(Guid characterId)
    {
        return await _dbSet
            .Include(c => c.Scene)
            .FirstOrDefaultAsync(c => c.Id == characterId);
    }
}