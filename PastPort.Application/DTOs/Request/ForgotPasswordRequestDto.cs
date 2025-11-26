using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class VerifyResetCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(5, MinimumLength = 5,ErrorMessage = "\"Code must be exactly 5 digits\"")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Code must contain only numbers")]
    public string Code { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword",ErrorMessage ="Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}