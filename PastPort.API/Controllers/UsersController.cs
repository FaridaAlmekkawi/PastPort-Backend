using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Request;
using PastPort.Application.Interfaces;
using System.Security.Claims;

namespace PastPort.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public UsersController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var profile = await _userService.GetUserProfileAsync(userId);

        if (profile == null)
            return NotFound(new { message = "User not found" });

        return Ok(new { success = true, data = profile });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userService.UpdateUserProfileAsync(userId, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

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

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userService.DeleteUserAccountAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("stats")]
    public Task<IActionResult> GetUserStats()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<IActionResult>(Unauthorized());

        return Task.FromResult<IActionResult>(Ok(new 
        { 
            success = true, 
            data = new 
            {
                totalScenes = 0,
                totalConversations = 0,
                joinedDate = DateTime.UtcNow
            }
        }));
    }
}