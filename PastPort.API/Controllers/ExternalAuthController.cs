using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using System.Security.Claims;

namespace PastPort.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExternalAuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<ExternalAuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate Google Login
    /// </summary>
    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "ExternalAuth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme, redirectUrl);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Google Login Callback
    /// </summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return BadRequest(new { error = "Error loading external login information" });
        }

        // Try to sign in with external login provider
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, 
            info.ProviderKey, 
            isPersistent: false, 
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            // User already exists - generate token
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                var token = await _jwtTokenService.GenerateAccessTokenAsync(user);
                var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

                return Ok(new
                {
                    success = true,
                    token,
                    refreshToken = refreshToken.Token,
                    user = new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName
                    }
                });
            }
        }

        // User doesn't exist - create new user
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { error = "Email not provided by external provider" });
        }

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                error = "Failed to create user",
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        // Add external login
        await _userManager.AddLoginAsync(newUser, info);
        
        // Add default role
        await _userManager.AddToRoleAsync(newUser, "Individual");

        // Sign in
        await _signInManager.SignInAsync(newUser, isPersistent: false);

        // Generate tokens
        var newToken = await _jwtTokenService.GenerateAccessTokenAsync(newUser);
        var newRefreshToken = await _jwtTokenService.CreateRefreshTokenAsync(newUser);

        return Ok(new
        {
            success = true,
            token = newToken,
            refreshToken = newRefreshToken.Token,
            user = new
            {
                newUser.Id,
                newUser.Email,
                newUser.FirstName,
                newUser.LastName
            }
        });
    }

    /// <summary>
    /// Initiate Facebook Login
    /// </summary>
    [HttpGet("facebook")]
    public IActionResult FacebookLogin([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(FacebookCallback), "ExternalAuth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            FacebookDefaults.AuthenticationScheme, redirectUrl);
        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Facebook Login Callback
    /// </summary>
    [HttpGet("facebook-callback")]
    public async Task<IActionResult> FacebookCallback(string? returnUrl = null)
    {
        // Same logic as Google - reuse the code
        return await GoogleCallback(returnUrl);
    }
}