using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;


namespace PastPort.Application.Interfaces;

public interface IAuthService
{
    // Existing
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string userId);

    // New - Email Verification
    Task<ApiResponseDto> SendVerificationCodeAsync(string userId);
    Task<ApiResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request);
    Task<ApiResponseDto> ResendVerificationCodeAsync(ResendVerificationCodeRequestDto request);

    // New - Password Reset
    Task<ApiResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ApiResponseDto> VerifyResetCodeAsync(VerifyResetCodeRequestDto request);
    Task<ApiResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);

    // New - Change Password
    Task<ApiResponseDto> ChangePasswordAsync(string userId, ChangePasswordRequestDto request);
// External Login
Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request);
Task<AuthResponseDto> ExternalLoginCallbackAsync(ExternalLoginCallbackDto callback);
Task<AuthResponseDto> GoogleSignInAsync(string idToken);
}