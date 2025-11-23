using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class UpdateSceneRequestDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(100)]
    public string? Era { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public string? EnvironmentPrompt { get; set; }

    public string? Model3DUrl { get; set; }
}