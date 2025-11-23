using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class CreateCharacterRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Background { get; set; } = string.Empty;

    [Required]
    public string Personality { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? VoiceId { get; set; }

    public string? AvatarUrl { get; set; }

    [Required]
    public Guid SceneId { get; set; }
}