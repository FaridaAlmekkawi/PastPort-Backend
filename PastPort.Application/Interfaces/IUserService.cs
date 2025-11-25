using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileResponseDto?> GetUserProfileAsync(string userId);
    Task<ApiResponseDto> UpdateUserProfileAsync(string userId, UpdateProfileRequestDto request);
    Task<ApiResponseDto> DeleteUserAccountAsync(string userId);
}