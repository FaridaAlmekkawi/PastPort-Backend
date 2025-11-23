using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.Infrastructure.Data.Repositories;

public class SceneRepository : Repository<HistoricalScene>, ISceneRepository
{
    public SceneRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<HistoricalScene>> GetByEraAsync(string era)
    {
        return await _dbSet
            .Where(s => s.Era.ToLower() == era.ToLower())
            .Include(s => s.Characters)
            .ToListAsync();
    }

    public async Task<IEnumerable<HistoricalScene>> SearchScenesAsync(string searchTerm)
    {
        return await _dbSet
            .Where(s => s.Title.Contains(searchTerm) ||
                       s.Description.Contains(searchTerm) ||
                       s.Era.Contains(searchTerm))
            .Include(s => s.Characters)
            .ToListAsync();
    }

    public async Task<HistoricalScene?> GetSceneWithCharactersAsync(Guid sceneId)
    {
        return await _dbSet
            .Include(s => s.Characters)
            .FirstOrDefaultAsync(s => s.Id == sceneId);
    }
}