namespace PastPort.Application.DTOs.Response;

public class ConversationResponseDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string CharacterResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ConversationHistoryDto
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public List<ConversationResponseDto> Messages { get; set; } = new();
}