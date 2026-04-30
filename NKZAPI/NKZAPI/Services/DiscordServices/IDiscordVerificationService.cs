namespace NKZAPI.Services.DiscordServices
{
    public interface IDiscordVerificationService
    {
        Task SendVerificationCodeAsync(string discordUserId, string email, string code);
    }
}
