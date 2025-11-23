using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string userId);
}