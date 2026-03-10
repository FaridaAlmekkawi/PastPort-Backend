using System.ComponentModel.DataAnnotations;

public class ConfirmPaymentRequestDto
{
    [Required]
    public string PaymentIntentId { get; set; } = string.Empty;

    public string? PaymentMethodId { get; set; }
}
