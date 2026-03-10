using PastPort.Domain.Entities;

namespace PastPort.Domain.Interfaces;

public interface ICharacterRepository : IRepository<Character>
{
    Task<IEnumerable<Character>> GetCharactersBySceneIdAsync(Guid sceneId);
    Task<Character?> GetCharacterWithSceneAsync(Guid characterId);
}