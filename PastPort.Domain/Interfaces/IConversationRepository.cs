using PastPort.Domain.Entities;

namespace PastPort.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
    Task<IEnumerable<Conversation>> GetConversationsByCharacterAsync(Guid characterId);
    Task<IEnumerable<Conversation>> GetUserConversationsWithCharacterAsync(string userId, Guid characterId);
}