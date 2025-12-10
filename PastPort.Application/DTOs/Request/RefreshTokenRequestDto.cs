
using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}