public class CompletePaymentResponseDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public required string SubscriptionId { get; set; }
}
