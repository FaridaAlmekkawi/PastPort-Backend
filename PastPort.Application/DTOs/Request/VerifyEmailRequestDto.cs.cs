using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class VerifyEmailRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class ResendVerificationCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}