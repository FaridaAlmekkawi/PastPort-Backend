using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;
using System.Security.Claims;

namespace PastPort.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ISubscriptionService subscriptionService,
        IPaymentRepository paymentRepository,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    /// <summary>
    /// بدء عملية دفع جديدة
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] PayPalPaymentRequestDto request,
        [FromQuery] SubscriptionPlan plan,
        [FromQuery] int durationInMonths = 1)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var subscriptionRequest = new CreateSubscriptionRequestDto
            {
                Plan = plan,
                DurationInMonths = durationInMonths
            };

            // استدعاء الـ Service
            var result = await ((ISubscriptionService)_subscriptionService)
                .InitiatePaymentAsync(userId, subscriptionRequest, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// إكمال الدفع بعد الموافقة
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> CompletePayment(
        [FromBody] PayPalApprovalDto request,
        [FromQuery] SubscriptionPlan plan,
        [FromQuery] int durationInMonths = 1)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var subscriptionRequest = new CreateSubscriptionRequestDto
            {
                Plan = plan,
                DurationInMonths = durationInMonths
            };

            var result = await ((ISubscriptionService)_subscriptionService)
                .CompletePaymentAsync(userId, request.OrderId, subscriptionRequest);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على سجل الدفع
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var payments = await _paymentRepository.GetUserPaymentsAsync(userId);

            return Ok(new { data = payments, message = "Payment history retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history");
            return BadRequest(new { error = ex.Message });
        }
    }
}