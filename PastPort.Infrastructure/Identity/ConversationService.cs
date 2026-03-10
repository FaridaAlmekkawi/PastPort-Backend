using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IAIConversationService _aiService;

    public ConversationService(
        IConversationRepository conversationRepository,
        ICharacterRepository characterRepository,
        IAIConversationService aiService)
    {
        _conversationRepository = conversationRepository;
        _characterRepository = characterRepository;
        _aiService = aiService;
    }

    public async Task<ConversationResponseDto> CreateConversationAsync(
        string userId,
        CreateConversationRequestDto request)
    {
        // Get character
        var character = await _characterRepository.GetByIdAsync(request.CharacterId);
        if (character == null)
            throw new Exception("Character not found");

        // Get conversation history
        var history = await _conversationRepository
            .GetUserConversationsWithCharacterAsync(userId, request.CharacterId);

        var conversationHistory = history
            .SelectMany(h => new[] { h.UserMessage, h.CharacterResponse })
            .ToList();

        // Generate AI response
        var characterResponse = await _aiService.GenerateCharacterResponseAsync(
            character,
            request.UserMessage,
            conversationHistory);

        // Save conversation
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CharacterId = request.CharacterId,
            UserMessage = request.UserMessage,
            CharacterResponse = characterResponse,
            CreatedAt = DateTime.UtcNow
        };

        await _conversationRepository.AddAsync(conversation);

        return new ConversationResponseDto
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            CharacterId = conversation.CharacterId,
            CharacterName = character.Name,
            UserMessage = conversation.UserMessage,
            CharacterResponse = conversation.CharacterResponse,
            CreatedAt = conversation.CreatedAt
        };
    }

    public async Task<List<ConversationResponseDto>> GetUserConversationsAsync(string userId)
    {
        var conversations = await _conversationRepository.GetUserConversationsAsync(userId);

        var result = new List<ConversationResponseDto>();
        foreach (var conv in conversations)
        {
            var character = await _characterRepository.GetByIdAsync(conv.CharacterId);
            result.Add(new ConversationResponseDto
            {
                Id = conv.Id,
                UserId = conv.UserId,
                CharacterId = conv.CharacterId,
                CharacterName = character?.Name ?? "Unknown",
                UserMessage = conv.UserMessage,
                CharacterResponse = conv.CharacterResponse,
                CreatedAt = conv.CreatedAt
            });
        }

        return result;
    }

    public async Task<ConversationHistoryDto> GetConversationHistoryWithCharacterAsync(
        string userId,
        Guid characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
            throw new Exception("Character not found");

        var conversations = await _conversationRepository
            .GetUserConversationsWithCharacterAsync(userId, characterId);

        return new ConversationHistoryDto
        {
            CharacterId = characterId,
            CharacterName = character.Name,
            Messages = conversations.Select(c => new ConversationResponseDto
            {
                Id = c.Id,
                UserId = c.UserId,
                CharacterId = c.CharacterId,
                CharacterName = character.Name,
                UserMessage = c.UserMessage,
                CharacterResponse = c.CharacterResponse,
                CreatedAt = c.CreatedAt
            }).ToList()
        };
    }
}