using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Infrastructure.Data;

namespace PastPort.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new Exception("User already exists");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new Exception("Invalid credentials");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new Exception("Invalid credentials");

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Invalid or expired refresh token");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow.ToString();
        await _context.SaveChangesAsync();

        return await GenerateAuthResponse(storedToken.User);
    }

    public async Task<bool> RevokeTokenAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow.ToString();
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(ApplicationUser user)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }
}