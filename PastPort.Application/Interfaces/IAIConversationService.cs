using PastPort.Domain.Entities;

namespace PastPort.Application.Interfaces;

public interface IAIConversationService
{
    Task<string> GenerateCharacterResponseAsync(
        Character character,
        string userMessage,
        List<string>? conversationHistory = null);
}