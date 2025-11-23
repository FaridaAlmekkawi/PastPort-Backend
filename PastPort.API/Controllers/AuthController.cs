using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Request;
using PastPort.Application.Interfaces;
using System.Security.Claims;

namespace PastPort.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return Ok(new { data = result, message = "Registration successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", request.Email);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(new { data = result, message = "Login successful" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login failed for {Email}", request.Email);
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(new { data = result, message = "Token refreshed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Logout user (revoke tokens)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _authService.RevokeTokenAsync(userId);
            _logger.LogInformation("User logged out: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new
        {
            data = new
            {
                id = userId,
                email = email,
                name = name
            }
        });
    }
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}