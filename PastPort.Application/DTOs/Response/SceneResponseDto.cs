namespace PastPort.Application.DTOs.Response;

public class SceneResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Era { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EnvironmentPrompt { get; set; } = string.Empty;
    public string? Model3DUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CharactersCount { get; set; }
    public List<CharacterSummaryDto> Characters { get; set; } = new();
}

public class CharacterSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}