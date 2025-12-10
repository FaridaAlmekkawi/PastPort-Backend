using System.ComponentModel.DataAnnotations;

public class CreatePaymentIntentRequestDto
{
    [Required]
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "USD";

    public Guid? SubscriptionId { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}