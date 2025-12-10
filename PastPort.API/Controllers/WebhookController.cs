using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;
using PastPort.Infrastructure.ExternalServices.Payment;

using Stripe;
using System.Text.Json.Serialization;


namespace PastPort.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<WebhookController> _logger;
    private readonly IPayPalPaymentService _paypalService;
    private readonly PayPalSettings _paypalSettings;

    public WebhookController(
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IOptions<StripeSettings> stripeSettings,
        ILogger<WebhookController> logger)

    {
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
    }


    /// <summary>
    /// Stripe Webhook Endpoint
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret
            );

            _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceededAsync(stripeEvent);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailedAsync(stripeEvent);
                    break;

                case "payment_intent.canceled":
                    await HandlePaymentIntentCanceledAsync(stripeEvent);
                    break;

                case "charge.refunded":
                    await HandleChargeRefundedAsync(stripeEvent);
                    break;

                case "customer.subscription.created":
                    await HandleSubscriptionCreatedAsync(stripeEvent);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }


            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest(new { error = "Invalid signature" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, new { error = "Webhook processing failed" });
        }
    }

    // ==================== Event Handlers ====================

    private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(paymentIntent.Id);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found for PaymentIntent: {PaymentIntentId}", paymentIntent.Id);
            return;
        }

        // Fix: Access the latest charge using the LatestChargeId property  
        var charge = paymentIntent.LatestCharge;
        string? receiptUrl = charge?.ReceiptUrl;

        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.UtcNow;
        payment.ReceiptUrl = receiptUrl;

        await _paymentRepository.UpdateAsync(payment);

        // Log transaction  
        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Charge,
            PaymentStatus.Succeeded,
            stripeEvent.Data.RawObject.ToString()
        );

        // Update subscription if exists  
        if (payment.SubscriptionId.HasValue)
        {
            await ActivateSubscriptionAsync(payment.SubscriptionId.Value);
        }

        _logger.LogInformation("Payment succeeded: {PaymentId}", payment.Id);
    }

    private async Task HandlePaymentIntentFailedAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(paymentIntent.Id);
        if (payment == null) return;

        payment.Status = PaymentStatus.Failed;
        payment.FailedAt = DateTime.UtcNow;
        payment.FailureReason = paymentIntent.LastPaymentError?.Message ?? "Unknown error";

        await _paymentRepository.UpdateAsync(payment);

        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Charge,
            PaymentStatus.Failed,
            stripeEvent.Data.RawObject.ToString(),
            payment.FailureReason
        );
        _logger.LogWarning("Payment failed: {PaymentId} - {Reason}",
            payment.Id, payment.FailureReason);
    }

    private async Task HandlePaymentIntentCanceledAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(paymentIntent.Id);
        if (payment == null) return;

        payment.Status = PaymentStatus.Cancelled;

        await _paymentRepository.UpdateAsync(payment);

        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Cancel,
            PaymentStatus.Cancelled,
            stripeEvent.Data.RawObject.ToString()
        );

        _logger.LogInformation("Payment cancelled: {PaymentId}", payment.Id);
    }

    private async Task HandleChargeRefundedAsync(Event stripeEvent)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return;

        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(
            charge.PaymentIntentId);
        if (payment == null) return;

        var refundedAmount = charge.AmountRefunded / 100m;
        var totalAmount = charge.Amount / 100m;

        payment.Status = refundedAmount >= totalAmount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        payment.RefundedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment);

        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Refund,
            payment.Status,
            stripeEvent.Data.RawObject.ToString()
        );

        _logger.LogInformation("Payment refunded: {PaymentId} - Amount: {Amount}",
            payment.Id, refundedAmount);
    }

    private async Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Domain.Entities.Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription created in Stripe: {SubscriptionId}",
            subscription.Id);

        // Handle subscription creation if needed
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Domain.Entities.Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription updated in Stripe: {SubscriptionId}",
            subscription.Id);

        // Handle subscription updates if needed
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Domain.Entities.Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription deleted in Stripe: {SubscriptionId}",
            subscription.Id);

        // Handle subscription cancellation if needed
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Domain.Entities.Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice payment succeeded: {InvoiceId}", invoice.Id);

        // Update invoice status in your database if needed
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Domain.Entities.Invoice;
        if (invoice == null) return;

        _logger.LogWarning("Invoice payment failed: {InvoiceId}", invoice.Id);

        // Handle failed invoice payment
    }

    // ==================== Helper Methods ====================

    private async Task LogPaymentTransactionAsync(
        Guid paymentId,
        PaymentTransactionType type,
        PaymentStatus status,
        string? providerResponse,
        string? errorMessage = null)
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Type = type,
            Status = status,
            ProviderResponse = providerResponse,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database - you need to add repository method
        // await _paymentTransactionRepository.AddAsync(transaction);
    }

    private async Task ActivateSubscriptionAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null) return;

        subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Subscription activated: {SubscriptionId}", subscriptionId);
    }
    /// <summary>
    /// PayPal Webhook Endpoint
    /// </summary>
    [HttpPost("paypal")]
    [AllowAnonymous]
    public async Task<IActionResult> PayPalWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            // Extract headers for signature verification
            var headers = new Dictionary<string, string>
        {
            { "PAYPAL-AUTH-ALGO", Request.Headers["PAYPAL-AUTH-ALGO"].ToString() },
            { "PAYPAL-CERT-URL", Request.Headers["PAYPAL-CERT-URL"].ToString() },
            { "PAYPAL-TRANSMISSION-ID", Request.Headers["PAYPAL-TRANSMISSION-ID"].ToString() },
            { "PAYPAL-TRANSMISSION-SIG", Request.Headers["PAYPAL-TRANSMISSION-SIG"].ToString() },
            { "PAYPAL-TRANSMISSION-TIME", Request.Headers["PAYPAL-TRANSMISSION-TIME"].ToString() }
        };

            // Verify webhook signature
            var isValid = await _paypalService.VerifyWebhookSignatureAsync(
                _paypalSettings.WebhookId,
                headers,
                json
            );

            if (!isValid)
            {
                _logger.LogWarning("PayPal webhook signature verification failed");
                return BadRequest(new { error = "Invalid signature" });
            }

            // Parse webhook event
            var webhookEvent = System.Text.Json.JsonSerializer
                .Deserialize<PayPalWebhookEvent>(json);

            if (webhookEvent == null)
            {
                _logger.LogWarning("Failed to parse PayPal webhook event");
                return BadRequest(new { error = "Invalid webhook event" });
            }

            _logger.LogInformation("PayPal webhook received: {EventType}",
                webhookEvent.EventType);

            // Handle different event types
            switch (webhookEvent.EventType)
            {
                case "CHECKOUT.ORDER.APPROVED":
                    await HandlePayPalOrderApprovedAsync(webhookEvent);
                    break;

                case "PAYMENT.CAPTURE.COMPLETED":
                    await HandlePayPalCaptureCompletedAsync(webhookEvent);
                    break;

                case "PAYMENT.CAPTURE.DENIED":
                    await HandlePayPalCaptureDeniedAsync(webhookEvent);
                    break;

                case "PAYMENT.CAPTURE.REFUNDED":
                    await HandlePayPalCaptureRefundedAsync(webhookEvent);
                    break;

                case "CHECKOUT.ORDER.COMPLETED":
                    await HandlePayPalOrderCompletedAsync(webhookEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled PayPal event: {EventType}",
                        webhookEvent.EventType);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            return StatusCode(500, new { error = "Webhook processing failed" });
        }
    }

    // ==================== PayPal Webhook Event Handlers ====================

    private async Task HandlePayPalOrderApprovedAsync(PayPalWebhookEvent webhookEvent)
    {
        var orderId = webhookEvent.Resource?.Id;
        if (string.IsNullOrEmpty(orderId)) return;

        _logger.LogInformation("PayPal order approved: {OrderId}", orderId);

        // Order approved but not yet captured
        // No action needed - waiting for capture
    }

    private async Task HandlePayPalCaptureCompletedAsync(PayPalWebhookEvent webhookEvent)
    {
        var captureId = webhookEvent.Resource?.Id;
        if (string.IsNullOrEmpty(captureId)) return;

        // Find payment by capture ID or order ID
        var orderId = webhookEvent.Resource?.SupplementaryData?.RelatedIds?.OrderId;
        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(
            orderId ?? captureId);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for PayPal capture: {CaptureId}", captureId);
            return;
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.UtcNow;
        payment.ProviderPaymentId = captureId;

        await _paymentRepository.UpdateAsync(payment);

        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Charge,
            PaymentStatus.Succeeded,
            System.Text.Json.JsonSerializer.Serialize(webhookEvent.Resource)
        );

        // Update subscription
        if (payment.SubscriptionId.HasValue)
        {
            await ActivateSubscriptionAsync(payment.SubscriptionId.Value);
        }

        _logger.LogInformation("PayPal payment captured: {PaymentId}", payment.Id);
    }

    private async Task HandlePayPalCaptureDeniedAsync(PayPalWebhookEvent webhookEvent)
    {
        var captureId = webhookEvent.Resource?.Id;
        if (string.IsNullOrEmpty(captureId)) return;

        var orderId = webhookEvent.Resource?.SupplementaryData?.RelatedIds?.OrderId;
        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(
            orderId ?? captureId);

        if (payment == null) return;

        payment.Status = PaymentStatus.Failed;
        payment.FailedAt = DateTime.UtcNow;
        payment.FailureReason = "PayPal capture denied";

        await _paymentRepository.UpdateAsync(payment);

        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Charge,
            PaymentStatus.Failed,
            System.Text.Json.JsonSerializer.Serialize(webhookEvent.Resource),
            "Capture denied"
        );

        _logger.LogWarning("PayPal capture denied: {PaymentId}", payment.Id);
    }

    private async Task HandlePayPalCaptureRefundedAsync(PayPalWebhookEvent webhookEvent)
    {
        var refundId = webhookEvent.Resource?.Id;
        if (string.IsNullOrEmpty(refundId)) return;

        // Find original payment
        var captureId = webhookEvent.Resource?.Links?
            .FirstOrDefault(l => l.Rel == "up")?.Href?.Split('/').LastOrDefault();

        if (string.IsNullOrEmpty(captureId)) return;

        var payment = await _paymentRepository.GetPaymentByProviderIdAsync(captureId);
        if (payment == null) return;

        var refundAmount = decimal.Parse(webhookEvent.Resource?.Amount?.Value ?? "0");
        var totalAmount = payment.Amount;

        payment.Status = refundAmount >= totalAmount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        payment.RefundedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment);

        await LogPaymentTransactionAsync(
            payment.Id,
            PaymentTransactionType.Refund,
            payment.Status,
            System.Text.Json.JsonSerializer.Serialize(webhookEvent.Resource)
        );

        _logger.LogInformation("PayPal payment refunded: {PaymentId} - Amount: {Amount}",
            payment.Id, refundAmount);
    }

    private async Task HandlePayPalOrderCompletedAsync(PayPalWebhookEvent webhookEvent)
    {
        var orderId = webhookEvent.Resource?.Id;
        if (string.IsNullOrEmpty(orderId)) return;

        _logger.LogInformation("PayPal order completed: {OrderId}", orderId);
    }

    // ==================== PayPal Webhook Models ====================

    public class PayPalWebhookEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("resource")]
        public PayPalWebhookResource? Resource { get; set; }

        [JsonPropertyName("create_time")]
        public DateTime CreateTime { get; set; }
    }

    public class PayPalWebhookResource
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("supplementary_data")]
        public SupplementaryData? SupplementaryData { get; set; }

        [JsonPropertyName("links")]
        public List<Link>? Links { get; set; }
    }

    public class SupplementaryData
    {
        [JsonPropertyName("related_ids")]
        public RelatedIds? RelatedIds { get; set; }
    }

    public class RelatedIds
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;
    }
}