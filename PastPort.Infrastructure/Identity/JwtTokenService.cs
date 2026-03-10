using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PastPort.Application.Common;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PastPort.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public JwtTokenService(
        IOptions<JwtSettings> jwtSettings,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _context = context;
    }

    public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("uid", user.Id),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        claims.AddRange(userClaims);

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Task.FromResult(Convert.ToBase64String(randomNumber));
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = await GenerateRefreshTokenAsync(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
            return null;

        if (refreshToken.IsRevoked)
            return null;

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}