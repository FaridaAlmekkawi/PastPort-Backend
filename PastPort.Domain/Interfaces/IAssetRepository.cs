using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
namespace PastPort.Domain.Interfaces;

public interface IAssetRepository : IRepository<Asset>
{
    Task<List<Asset>> GetAssetsBySceneIdAsync(Guid sceneId);
    Task<Asset?> GetAssetByFileNameAsync(string fileName);
    Task<List<Asset>> GetAssetsByTypeAsync(AssetType type);
    Task<bool> AssetExistsAsync(string fileName, string fileHash);
}