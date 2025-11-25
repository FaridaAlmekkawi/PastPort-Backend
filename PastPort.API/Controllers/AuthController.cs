using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Request;
using PastPort.Application.Interfaces;
using System.Security.Claims;

namespace PastPort.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.LogoutAsync(userId);

        if (!result)
            return BadRequest(new { message = "Logout failed" });

        return Ok(new { message = "Logged out successfully" });
    }

    // ========== Email Verification ==========

    [Authorize]
    [HttpPost("send-verification-code")]
    public async Task<IActionResult> SendVerificationCode()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.SendVerificationCodeAsync(userId);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.VerifyEmailAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("resend-verification-code")]
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendVerificationCodeRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ResendVerificationCodeAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ========== Password Reset ==========

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ForgotPasswordAsync(request);

        return Ok(result); // Always return success to prevent email enumeration
    }

    [HttpPost("verify-reset-code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.VerifyResetCodeAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ========== Change Password ==========

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.ChangePasswordAsync(userId, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
// ========== External Login ==========

/// <summary>
/// Google Login - Redirect to Google
/// </summary>
[HttpGet("external-login/google")]
public IActionResult GoogleLogin([FromQuery] string returnUrl = "/")
{
    var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", 
        new { provider = "Google", returnUrl });
    
    var properties = new AuthenticationProperties 
    { 
        RedirectUri = redirectUrl 
    };
    
    return Challenge(properties, "Google");
}

/// <summary>
/// Facebook Login - Redirect to Facebook
/// </summary>
[HttpGet("external-login/facebook")]
public IActionResult FacebookLogin([FromQuery] string returnUrl = "/")
{
    var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", 
        new { provider = "Facebook", returnUrl });
    
    var properties = new AuthenticationProperties 
    { 
        RedirectUri = redirectUrl 
    };
    
    return Challenge(properties, "Facebook");
}

/// <summary>
/// Apple Login - Redirect to Apple
/// </summary>
[HttpGet("external-login/apple")]
public IActionResult AppleLogin([FromQuery] string returnUrl = "/")
{
    var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", 
        new { provider = "Apple", returnUrl });
    
    var properties = new AuthenticationProperties 
    { 
        RedirectUri = redirectUrl 
    };
    
    return Challenge(properties, "Apple");
}

/// <summary>
/// External Login Callback - Called by Provider after successful login
/// </summary>
[HttpGet("external-login-callback")]
public async Task<IActionResult> ExternalLoginCallback(
    [FromQuery] string provider, 
    [FromQuery] string returnUrl = "/")
{
    var info = await HttpContext.AuthenticateAsync(provider);
    
    if (!info.Succeeded)
    {
        return BadRequest(new { message = "External authentication failed" });
    }

    // استخرج البيانات من Claims
    var email = info.Principal?.FindFirst(ClaimTypes.Email)?.Value;
    var firstName = info.Principal?.FindFirst(ClaimTypes.GivenName)?.Value ?? "";
    var lastName = info.Principal?.FindFirst(ClaimTypes.Surname)?.Value ?? "";
    var providerId = info.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerId))
    {
        return BadRequest(new { message = "Could not retrieve user information" });
    }

    var callback = new ExternalLoginCallbackDto
    {
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        ProviderId = providerId,
        Provider = provider
    };

    var result = await _authService.ExternalLoginCallbackAsync(callback);

    if (!result.Success)
        return BadRequest(result);

    // في Production: Redirect للـ Frontend مع الـ Token
    return Ok(result);
}

/// <summary>
/// Mobile/SPA External Login - For Flutter/React apps
/// </summary>
[HttpPost("external-login/mobile")]
public async Task<IActionResult> MobileExternalLogin([FromBody] ExternalLoginRequestDto request)
{
    // هنا يرسل الـ Mobile App الـ Token بعد نجاح Login
    // نتحقق من صحة الـ Token ثم نسجل المستخدم
    
    // TODO: Verify token with provider API
    // For now, return not implemented
    
    return BadRequest(new { message = "Use web login flow or implement token verification" });
}
}


public record RefreshTokenRequestDto(string RefreshToken);