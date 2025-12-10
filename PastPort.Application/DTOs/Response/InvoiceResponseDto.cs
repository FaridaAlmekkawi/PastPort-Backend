public class InvoiceResponseDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }

    public string? PdfUrl { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
}