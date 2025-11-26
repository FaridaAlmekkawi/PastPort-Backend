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

    // ========== Registration ==========
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

        // إضافة Role افتراضي
        await _userManager.AddToRoleAsync(user, "Individual");

        // إرسال كود التفعيل
        await SendVerificationCodeAsync(user.Id);

        // توليد Tokens
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

    // ========== Login ==========
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

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Account locked due to multiple failed attempts. Try again later."
            };
        }

        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // تحديث آخر تسجيل دخول
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // توليد Tokens
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

    // ========== Token Refresh ==========
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

        // إلغاء الـ Token القديم
        await _jwtTokenService.RevokeRefreshTokenAsync(refreshToken);

        // توليد Tokens جديدة
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

    // ========== Logout ==========
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

        // حذف الأكواد القديمة غير المستخدمة
        var oldCodes = await _context.EmailVerificationCodes
            .Where(v => v.UserId == userId && !v.IsUsed)
            .ToListAsync();

        _context.EmailVerificationCodes.RemoveRange(oldCodes);

        // توليد كود 6 أرقام
        var code = GenerateVerificationCode(6);

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
            .Where(v => v.UserId == request.UserId && v.Code == request.Code && !v.IsUsed)
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

        if (verificationCode.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Verification code expired. Please request a new one."
            };
        }

        // تفعيل Email
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        // تحديد الكود كمستخدم
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
                Success = true,
                Message = "If the email exists, a verification code has been sent"
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

        return await SendVerificationCodeAsync(user.Id);
    }

    // ========== Forgot Password (5-digit code) ==========
    public async Task<ApiResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Security: لا تكشف عن وجود المستخدم
            return new ApiResponseDto
            {
                Success = true,
                Message = "If the email exists, a password reset code has been sent"
            };
        }

        // حذف الأكواد القديمة
        var oldTokens = await _context.PasswordResetTokens
            .Where(r => r.UserId == user.Id && !r.IsUsed)
            .ToListAsync();

        _context.PasswordResetTokens.RemoveRange(oldTokens);

        // توليد كود 5 أرقام
        var code = GenerateVerificationCode(5);
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

        // إرسال Email
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
            .Where(r => r.UserId == user.Id && r.Code == request.Code && !r.IsUsed)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (resetToken == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid code"
            };
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Code has expired. Please request a new one."
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
            .Where(r => r.UserId == user.Id && r.Code == request.Code && !r.IsUsed)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (resetToken == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Invalid code"
            };
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Code has expired"
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

        // تحديد الكود كمستخدم
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // إلغاء كل Refresh Tokens (أمان)
        await _jwtTokenService.RevokeAllUserTokensAsync(user.Id);

        // إرسال إشعار
        await _emailService.SendPasswordChangedNotificationAsync(user.Email!);

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

        // إلغاء كل Refresh Tokens (أمان)
        await _jwtTokenService.RevokeAllUserTokensAsync(userId);

        // إرسال إشعار
        await _emailService.SendPasswordChangedNotificationAsync(user.Email!);

        return new ApiResponseDto
        {
            Success = true,
            Message = "Password changed successfully"
        };
    }

   

    // ========== Helper Methods ==========
    private string GenerateVerificationCode(int length = 6)
    {
        int min = (int)Math.Pow(10, length - 1);
        int max = (int)Math.Pow(10, length) - 1;
        return RandomNumberGenerator.GetInt32(min, max).ToString();
    }
// ========== External Login Methods ==========

public Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request)
{
    return Task.FromResult(new AuthResponseDto
    {
        Success = false,
        Message = "External login not fully implemented. Use web flow."
    });
}

public async Task<AuthResponseDto> GoogleSignInAsync(string idToken)
{
    // TODO: Verify Google ID Token
    // For now, return not implemented
    return new AuthResponseDto
    {
        Success = false,
        Message = "Google Sign-In via ID Token not implemented yet. Use web flow."
    };
}

public async Task<AuthResponseDto> ExternalLoginCallbackAsync(ExternalLoginCallbackDto callback)
{
    var user = await _userManager.FindByEmailAsync(callback.Email);
    
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = callback.Email,
            Email = callback.Email,
            FirstName = callback.FirstName,
            LastName = callback.LastName,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user);
        
        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        await _userManager.AddToRoleAsync(user, "Individual");

        var loginInfo = new UserLoginInfo(
            callback.Provider,
            callback.ProviderId,
            callback.Provider
        );
        
        await _userManager.AddLoginAsync(user, loginInfo);
    }
    else
    {
        var logins = await _userManager.GetLoginsAsync(user);
        var existingLogin = logins.FirstOrDefault(l => 
            l.LoginProvider == callback.Provider && 
            l.ProviderKey == callback.ProviderId);

        if (existingLogin == null)
        {
            var loginInfo = new UserLoginInfo(
                callback.Provider,
                callback.ProviderId,
                callback.Provider
            );
            
            await _userManager.AddLoginAsync(user, loginInfo);
        }
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
}