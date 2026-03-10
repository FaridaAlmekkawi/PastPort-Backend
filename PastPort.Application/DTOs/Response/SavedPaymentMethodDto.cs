public class SavedPaymentMethodDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;

    public string? CardBrand { get; set; }
    public string? CardLast4 { get; set; }
    public int? CardExpMonth { get; set; }
    public int? CardExpYear { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}