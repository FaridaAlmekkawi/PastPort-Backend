namespace PastPort.Application.DTOs.Response;

public class VerifyCodeResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? RemainingAttempts { get; set; }
}