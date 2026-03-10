using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface IConversationService
{
    Task<ConversationResponseDto> CreateConversationAsync(
        string userId,
        CreateConversationRequestDto request);
    Task<List<ConversationResponseDto>> GetUserConversationsAsync(string userId);
    Task<ConversationHistoryDto> GetConversationHistoryWithCharacterAsync(
        string userId,
        Guid characterId);
}