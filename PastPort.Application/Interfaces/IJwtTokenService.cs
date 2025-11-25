using PastPort.Domain.Entities;

namespace PastPort.Application.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user);
    Task<string> GenerateRefreshTokenAsync();
    Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllUserTokensAsync(string userId);
}