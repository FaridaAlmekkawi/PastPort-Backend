using Microsoft.AspNetCore.Identity;
using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Infrastructure.Data;

namespace PastPort.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<UserProfileResponseDto?> GetUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileResponseDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        };
    }

    public async Task<ApiResponseDto> UpdateUserProfileAsync(string userId, UpdateProfileRequestDto request)
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

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        return new ApiResponseDto
        {
            Success = true,
            Message = "Profile updated successfully"
        };
    }

    public async Task<ApiResponseDto> DeleteUserAccountAsync(string userId)
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

        
        // (Conversations, Subscriptions, etc. - Cascade Delete من Database)

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        return new ApiResponseDto
        {
            Success = true,
            Message = "Account deleted successfully"
        };
    }
}