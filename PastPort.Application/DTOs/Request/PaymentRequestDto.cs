using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class CreatePaymentRequestDto
{
    [Required]
    public Guid SubscriptionId { get; set; }

    [Required]
    public string PaymentMethodId { get; set; } = string.Empty; // Stripe Payment Method ID

    public bool SavePaymentMethod { get; set; }
    public bool SetAsDefault { get; set; }
}