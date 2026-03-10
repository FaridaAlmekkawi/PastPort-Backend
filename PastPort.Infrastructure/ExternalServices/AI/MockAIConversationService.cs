using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;

namespace PastPort.Infrastructure.ExternalServices.AI;

public class MockAIConversationService : IAIConversationService
{
    public Task<string> GenerateCharacterResponseAsync(
        Character character,
        string userMessage,
        List<string>? conversationHistory = null)
    {
        // Mock response - في Phase 4 هنربطها بـ OpenAI أو Claude
        var response = $"[{character.Name} responds]: I am {character.Name}, {character.Role}. " +
                      $"You asked: '{userMessage}'. " +
                      $"Based on my background as {character.Background}, " +
                      $"I would say this is a fascinating question from my time period.";

        return Task.FromResult(response);
    }
}