using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Infrastructure.Data;
using System.Security.Cryptography;

namespace PastPort.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext context,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "User with this email already exists"
            };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        await _userManager.AddToRoleAsync(user, "Individual");

        // إرسال كود التفعيل
        await SendVerificationCodeAsync(user.Id);

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful. Please verify your email.",
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var validatedToken = await _jwtTokenService.ValidateRefreshTokenAsync(refreshToken);
        
        if (validatedToken == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            };
        }

        var user = validatedToken.User;

        await _jwtTokenService.RevokeRefreshTokenAsync(refreshToken);

        var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var newRefreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed successfully",
            Token = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }

    public async Task<bool> LogoutAsync(string userId)
    {
        await _jwtTokenService.RevokeAllUserTokensAsync(userId);
        await _signInManager.SignOutAsync();
        return true;
    }

    // ========== Email Verification ==========

    public async Task<ApiResponseDto> SendVerificationCodeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (user.IsEmailVerified)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Email already verified"
            };
        }

        // Generate 6-digit code
        var code = GenerateVerificationCode();

        var verificationCode = new EmailVerificationCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerificationCodes.Add(verificationCode);
        await _context.SaveChangesAsync();

        // إرسال Email
        await _emailService.SendVerificationEmailAsync(user.Email!, code);

        return new ApiResponseDto
        {
            Success = true,
            Message = "Verification code sent to your email"
        };
    }

    public async Task<ApiResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "User not found"
            };
        }

        var verificationCode = await _context.EmailVerificationCodes
            .Where(v => v.UserId == request.UserId && v.Code == request.Code)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (verificationCode == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid verification code"
            };
        }

        if (verificationCode.IsUsed)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Verification code already used"
            };
        }

        if (verificationCode.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Verification code expired"
            };
        }

        // تفعيل الـ Email
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        // Mark code as used
        verificationCode.IsUsed = true;
        verificationCode.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponseDto
        {
            Success = true,
            Message = "Email verified successfully"
        };
    }

    public async Task<ApiResponseDto> ResendVerificationCodeAsync(ResendVerificationCodeRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "User not found"
            };
        }

        return await SendVerificationCodeAsync(user.Id);
    }

    // ========== Password Reset ==========

    public async Task<ApiResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // لا تكشف عن وجود المستخدم من عدمه
            return new ApiResponseDto
            {
                Success = true,
                Message = "If the email exists, a password reset code has been sent"
            };
        }

        var code = GenerateVerificationCode();
        var token = Guid.NewGuid().ToString();

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email!, code);

        return new ApiResponseDto
        {
            Success = true,
            Message = "Password reset code sent to your email"
        };
    }

    public async Task<ApiResponseDto> VerifyResetCodeAsync(VerifyResetCodeRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid code"
            };
        }

        var resetToken = await _context.PasswordResetTokens
            .Where(r => r.UserId == user.Id && r.Code == request.Code)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid or expired code"
            };
        }

        return new ApiResponseDto
        {
            Success = true,
            Message = "Code verified successfully",
            Data = new { token = resetToken.Token }
        };
    }

    public async Task<ApiResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid request"
            };
        }

        var resetToken = await _context.PasswordResetTokens
            .Where(r => r.UserId == user.Id && r.Code == request.Code)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid or expired code"
            };
        }

        // Reset Password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // Mark as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Revoke all refresh tokens
        await _jwtTokenService.RevokeAllUserTokensAsync(user.Id);

        return new ApiResponseDto
        {
            Success = true,
            Message = "Password reset successfully"
        };
    }

    // ========== Change Password ==========

    public async Task<ApiResponseDto> ChangePasswordAsync(string userId, ChangePasswordRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "User not found"
            };
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword
        );

        if (!result.Succeeded)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // Revoke all refresh tokens for security
        await _jwtTokenService.RevokeAllUserTokensAsync(userId);

        return new ApiResponseDto
        {
            Success = true,
            Message = "Password changed successfully"
        };
    }

    // ========== Helper Methods ==========

    private string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

// ========== External Login ==========

public async Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request)
{
    // سيتم استدعاء هذا من Frontend بعد نجاح Login من Provider
    
    // هنا نتحقق من صحة الـ Token من Provider
    // ثم نسجل المستخدم أو نسجله دخول
    
    return new AuthResponseDto
    {
        Success = false,
        Message = "Not implemented yet - Use callback endpoint"
    };
}

public async Task<AuthResponseDto> ExternalLoginCallbackAsync(ExternalLoginCallbackDto callback)
{
    // البحث عن المستخدم بالـ Email
    var user = await _userManager.FindByEmailAsync(callback.Email);
    
    if (user == null)
    {
        // مستخدم جديد - سجله
        user = new ApplicationUser
        {
            UserName = callback.Email,
            Email = callback.Email,
            FirstName = callback.FirstName,
            LastName = callback.LastName,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = true, // External providers verify email
            EmailVerifiedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        // إنشاء بدون password (External Login)
        var result = await _userManager.CreateAsync(user);
        
        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // أعطه Role
        await _userManager.AddToRoleAsync(user, "Individual");

        // أضف Login Info
        var loginInfo = new UserLoginInfo(
            callback.Provider,
            callback.ProviderId,
            callback.Provider
        );
        
        await _userManager.AddLoginAsync(user, loginInfo);
    }
    else
    {
        // مستخدم موجود - تحقق من ربط الحساب
        var logins = await _userManager.GetLoginsAsync(user);
        var existingLogin = logins.FirstOrDefault(l => 
            l.LoginProvider == callback.Provider && 
            l.ProviderKey == callback.ProviderId);

        if (existingLogin == null)
        {
            // ربط الحساب بهذا Provider
            var loginInfo = new UserLoginInfo(
                callback.Provider,
                callback.ProviderId,
                callback.Provider
            );
            
            await _userManager.AddLoginAsync(user, loginInfo);
        }
    }

    // حدّث آخر تسجيل دخول
    user.LastLoginAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    // ولّد Tokens
    var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
    var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

    return new AuthResponseDto
    {
        Success = true,
        Message = "Login successful",
        Token = accessToken,
        RefreshToken = refreshToken.Token,
        TokenExpiration = DateTime.UtcNow.AddMinutes(60),
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