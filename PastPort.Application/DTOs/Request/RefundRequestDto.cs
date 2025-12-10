using PastPort.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public class RefundRequestDto
{
    [Required]
    public Guid PaymentId { get; set; }

    [Range(0.01, 999999)]
    public decimal? Amount { get; set; } // null = full refund

    [Required]
    public RefundReason Reason { get; set; }

    public string? Notes { get; set; }
}