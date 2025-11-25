using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class ExternalAuthRequestDto
{
    [Required]
    public string Provider { get; set; } = string.Empty; // "Google", "Facebook", "Apple"

    [Required]
    public string IdToken { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Name { get; set; }
}