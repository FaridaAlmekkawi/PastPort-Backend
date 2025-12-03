using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;

namespace PastPort.Infrastructure.Data.Repositories;

public class AssetRepository : Repository<Asset>, IAssetRepository
{
    public AssetRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Asset>> GetAssetsBySceneIdAsync(Guid sceneId)
    {
        return await _dbSet
            .Where(a => a.SceneId == sceneId && a.Status == AssetStatus.Available)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Asset?> GetAssetByFileNameAsync(string fileName)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.FileName == fileName);
    }

    public async Task<List<Asset>> GetAssetsByTypeAsync(AssetType type)
    {
        return await _dbSet
            .Where(a => a.Type == type && a.Status == AssetStatus.Available)
            .ToListAsync();
    }

    public async Task<bool> AssetExistsAsync(string fileName, string fileHash)
    {
        return await _dbSet
            .AnyAsync(a => a.FileName == fileName && a.FileHash == fileHash);
    }
}
