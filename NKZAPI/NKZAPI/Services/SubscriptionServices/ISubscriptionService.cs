using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.SubscriptionServices
{
    public interface ISubscriptionService
    {
        Task<Response<List<SubscriptionPlanDto>>> ListPlansAsync(bool includeInactive = false);
        Task<Response<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanDto request);
        Task<Response<SubscriptionPlanDto>> UpdatePlanAsync(Guid id, CreateSubscriptionPlanDto request);
        Task<Response<string>> DeletePlanAsync(Guid id);
        Task<Response<UserSubscriptionDto?>> GetCurrentSubscriptionAsync(Guid userId);
        Task<Response<string>> CreateCheckoutAsync(Guid userId, SubscriptionCheckoutDto request);
        Task<Response<string>> ProcessMercadoPagoNotificationAsync(string? paymentId);
    }
}
