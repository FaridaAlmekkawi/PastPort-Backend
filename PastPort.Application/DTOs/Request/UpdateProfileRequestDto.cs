using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class UpdateProfileRequestDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Phone]
    public string? PhoneNumber { get; set; }
}