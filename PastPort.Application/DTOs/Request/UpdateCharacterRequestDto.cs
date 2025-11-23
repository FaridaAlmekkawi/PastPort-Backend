using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class UpdateCharacterRequestDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Role { get; set; }

    public string? Background { get; set; }

    public string? Personality { get; set; }

    [MaxLength(100)]
    public string? VoiceId { get; set; }

    public string? AvatarUrl { get; set; }
}