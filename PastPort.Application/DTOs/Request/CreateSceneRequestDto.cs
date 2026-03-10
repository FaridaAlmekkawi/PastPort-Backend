using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class CreateSceneRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Era { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string EnvironmentPrompt { get; set; } = string.Empty;

    public string? Model3DUrl { get; set; }
}