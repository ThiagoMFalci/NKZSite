using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.WalletServices
{
    public interface IWalletService
    {
        Task<Response<object>> GetWalletAsync(Guid userId);
        Task<Response<string>> CreateDepositAsync(Guid userId, WalletDepositDto deposit);
        Task<Response<string>> ProcessMercadoPagoNotificationAsync(string? paymentId);
    }
}
