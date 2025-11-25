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