using NKZAPI.Dtos;

namespace NKZAPI.Services.DiscordServices
{
    public interface IDiscordVerificationService
    {
        Task<DiscordVerificationDeliveryDto> SendVerificationCodeAsync(string discordIdentity, string email, string code);
    }
}
