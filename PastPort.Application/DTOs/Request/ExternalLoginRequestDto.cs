using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class ExternalLoginRequestDto
{
    [Required]
    public string Provider { get; set; } = string.Empty; // "Google", "Facebook", "Apple"
    
    [Required]
    public string AccessToken { get; set; } = string.Empty; // Token من Provider
    
    public string? IdToken { get; set; } // للـ Apple/Google
}

public class ExternalLoginCallbackDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}