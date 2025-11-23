using PastPort.Domain.Entities;
using System.Security.Claims;

namespace PastPort.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}