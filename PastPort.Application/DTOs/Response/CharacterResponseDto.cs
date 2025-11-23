namespace PastPort.Application.DTOs.Response;

public class CharacterResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string? VoiceId { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid SceneId { get; set; }
    public string SceneTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}