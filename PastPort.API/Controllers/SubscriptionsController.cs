using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Request;
using PastPort.Application.Interfaces;
using System.Security.Claims;
using PastPort.Infrastructure.Identity;
namespace PastPort.API.Controllers;

[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [AllowAnonymous]
    [HttpGet("plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailablePlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAvailablePlansAsync();
            return Ok(new { data = plans, message = "Subscription plans retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription plans");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get user's active subscription
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveSubscription()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            if (subscription == null)
                return NotFound(new { message = "No active subscription found" });

            return Ok(new { data = subscription, message = "Active subscription retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active subscription");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get user's subscription history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionHistory()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);
            return Ok(new { data = subscriptions, message = "Subscription history retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription history");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create new subscription
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var subscription = await _subscriptionService.CreateSubscriptionAsync(userId, request);
            _logger.LogInformation("Subscription created for user {UserId}", userId);

            return CreatedAtAction(nameof(GetActiveSubscription), null,
                new { data = subscription, message = "Subscription created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subscription");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Cancel active subscription
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSubscription()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _subscriptionService.CancelSubscriptionAsync(userId);
            _logger.LogInformation("Subscription cancelled for user {UserId}", userId);
            return Ok(new { message = "Subscription cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription");
            return HandleError(ex);
        }
    }
}
    
