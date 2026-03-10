namespace PastPort.Domain.Entities;

public class EmailVerificationCode
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }

    // Navigation Property
    public ApplicationUser User { get; set; } = null!;
}