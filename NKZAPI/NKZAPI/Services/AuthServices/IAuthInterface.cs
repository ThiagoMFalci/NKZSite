using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.AuthServices
{
    public interface IAuthInterface
    {
        Task<Response<UserDto>> UserAddAsync(UserDto User);
        Task<Response<object>> Login(UserLoginDto userLogin);
        Task<Response<string>> VerifyTwoFactorAsync(TwoFactorVerifyDto verification);
        Task<Response<string>> VerifyEmailAsync(EmailVerificationDto verification);
        Task<Response<string>> ResendEmailVerificationAsync(string email);
        Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto request);
        Task<Response<string>> ResetPasswordAsync(ResetPasswordDto request);
        Task<Response<string>> VerifyDiscordAsync(DiscordVerificationDto verification);
        Task<Response<string>> ResendDiscordVerificationAsync(string email);
    }
}
