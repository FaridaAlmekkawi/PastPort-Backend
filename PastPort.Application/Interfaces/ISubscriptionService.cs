using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Domain.Enums;

namespace PastPort.Application.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(string userId, CreateSubscriptionRequestDto request);
    Task<SubscriptionResponseDto?> GetActiveSubscriptionAsync(string userId);
    Task<List<SubscriptionResponseDto>> GetUserSubscriptionsAsync(string userId);
    Task<bool> CancelSubscriptionAsync(string userId);
    Task<List<SubscriptionPlanInfoDto>> GetAvailablePlansAsync();
    Task<bool> CheckSubscriptionAccessAsync(string userId, SubscriptionPlan requiredPlan);
}