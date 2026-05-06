using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.DiscordServices;
using NKZAPI.Services.EmailServices;
using NKZAPI.Services.PassService;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NKZAPI.Services.AuthServices
{
    public class AuthService : IAuthInterface
    {
        private readonly NKZAPIContext _context;
        private readonly IPasswordInterface _passInterface;
        private readonly IDiscordVerificationService _discordVerificationService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(NKZAPIContext context, IPasswordInterface passInterface, IDiscordVerificationService discordVerificationService, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _passInterface = passInterface;
            _discordVerificationService = discordVerificationService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<Response<UserDto>> UserAddAsync(UserDto User)
        {
            var response = new Response<UserDto>();
            try
            {
                if (!VerifyIfUserExists(User))
                {
                    response.Data = null;
                    response.Message = "User already exists";
                    response.Success = false;
                    return response;
                }

                var requireDiscordVerification = GetFlag("Auth:RequireDiscordVerification", true);
                var requireEmailVerification = GetFlag("Auth:RequireEmailVerification", true);
                var discordIdentity = NormalizeDiscordIdentity(User.DiscordUsername, User.DiscordUserId);
                if (requireDiscordVerification && string.IsNullOrWhiteSpace(discordIdentity))
                {
                    response.Data = null;
                    response.Message = "Informe seu usuario do Discord para criar a conta.";
                    response.Success = false;
                    return response;
                }

                _passInterface.CreatePassHash(User.PasswordHash, out byte[] passwordHash, out byte[] passwordSalt);
                var discordCode = GenerateCode();
                var emailCode = GenerateCode();
                DiscordVerificationDeliveryDto? delivery = null;

                if (requireDiscordVerification)
                {
                    delivery = await _discordVerificationService.SendVerificationCodeAsync(discordIdentity, User.Email, discordCode);
                    if (string.IsNullOrWhiteSpace(delivery.DiscordUserId))
                    {
                        response.Data = null;
                        response.Message = "Nao foi possivel localizar este Discord no servidor NKZ.";
                        response.Success = false;
                        return response;
                    }

                    var discordExists = await _context.Users.AnyAsync(u => u.DiscordUserId == delivery.DiscordUserId);
                    if (discordExists)
                    {
                        response.Data = null;
                        response.Message = "Este Discord ja esta vinculado a outra conta.";
                        response.Success = false;
                        return response;
                    }
                }

                var user = new User
                {
                    Email = User.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Player = new List<Player>(),
                    Role = User.Role,
                    DiscordUserId = delivery?.DiscordUserId ?? "",
                    DiscordUsername = string.IsNullOrWhiteSpace(delivery?.DiscordUsername) ? discordIdentity : delivery.DiscordUsername,
                    DiscordVerified = !requireDiscordVerification,
                    DiscordVerifiedAt = requireDiscordVerification ? null : DateTime.UtcNow,
                    DiscordVerificationCodeHash = requireDiscordVerification ? HashCode(discordCode) : null,
                    DiscordVerificationCodeExpiresAt = requireDiscordVerification ? DateTime.UtcNow.AddMinutes(15) : null,
                    EmailVerified = !requireEmailVerification,
                    EmailVerifiedAt = requireEmailVerification ? null : DateTime.UtcNow,
                    EmailVerificationCodeHash = requireEmailVerification ? HashCode(emailCode) : null,
                    EmailVerificationCodeExpiresAt = requireEmailVerification ? DateTime.UtcNow.AddMinutes(20) : null,
                };

                if (requireEmailVerification)
                {
                    await SendEmailCodeAsync(user.Email, emailCode, "Confirme seu email NKZ", "Use este codigo para confirmar seu email na NKZ:");
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                response.Data = new UserDto
                {
                    Email = user.Email,
                    PasswordHash = "",
                    PasswordSalt = "",
                    Role = user.Role,
                    DiscordUserId = user.DiscordUserId,
                    DiscordUsername = user.DiscordUsername ?? "",
                };
                response.Message = requireDiscordVerification || requireEmailVerification
                    ? "Conta criada. Enviamos os codigos de verificacao necessarios."
                    : "Conta criada. Voce ja pode entrar.";
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.Message = ex.Message;
                response.Success = false;
            }

            return response;
        }

        public async Task<Response<object>> Login(UserLoginDto userLogin)
        {
            var response = new Response<object>();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userLogin.Email);
                if (user == null)
                {
                    response.Data = null;
                    response.Message = "User not found";
                    response.Success = false;
                    return response;
                }

                if (!_passInterface.VerifyPassHash(userLogin.Password, user.PasswordHash, user.PasswordSalt))
                {
                    response.Data = null;
                    response.Message = "Incorrect password";
                    response.Success = false;
                    return response;
                }

                if (GetFlag("Auth:RequireEmailVerification", true) && !user.EmailVerified)
                {
                    response.Data = null;
                    response.Message = "Confirme seu email antes de entrar.";
                    response.Success = false;
                    return response;
                }

                if (GetFlag("Auth:RequireDiscordVerification", true) && !user.DiscordVerified && !string.IsNullOrWhiteSpace(user.DiscordUserId))
                {
                    response.Data = null;
                    response.Message = "Confirme seu Discord antes de entrar.";
                    response.Success = false;
                    return response;
                }

                if (!GetFlag("Auth:RequireTwoFactor", true))
                {
                    response.Message = "Login successful";
                    response.Data = _passInterface.CreateToken(user);
                    response.Success = true;
                    return response;
                }

                var twoFactorCode = GenerateCode();
                var twoFactorToken = GenerateSecureToken();
                user.TwoFactorCodeHash = HashCode(twoFactorCode);
                user.TwoFactorSessionHash = HashCode(twoFactorToken);
                user.TwoFactorCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
                await _context.SaveChangesAsync();
                await SendEmailCodeAsync(user.Email, twoFactorCode, "Codigo de acesso NKZ", "Use este codigo para concluir seu login na NKZ:");

                response.Message = "Codigo de verificacao enviado para o email.";
                response.Data = new TwoFactorChallengeDto
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = twoFactorToken,
                    Email = user.Email,
                };
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.Message = ex.Message;
                response.Success = false;
            }

            return response;
        }

        public async Task<Response<string>> VerifyTwoFactorAsync(TwoFactorVerifyDto verification)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verification.Email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            if (user.TwoFactorCodeExpiresAt == null || user.TwoFactorCodeExpiresAt < DateTime.UtcNow)
            {
                response.Success = false;
                response.Message = "Codigo expirado. Entre novamente.";
                return response;
            }

            if (!FixedTimeEquals(user.TwoFactorCodeHash, HashCode(verification.Code)) ||
                !FixedTimeEquals(user.TwoFactorSessionHash, HashCode(verification.TwoFactorToken)))
            {
                response.Success = false;
                response.Message = "Codigo invalido.";
                return response;
            }

            user.TwoFactorCodeHash = null;
            user.TwoFactorSessionHash = null;
            user.TwoFactorCodeExpiresAt = null;
            await _context.SaveChangesAsync();

            response.Message = "Login successful";
            response.Data = _passInterface.CreateToken(user);
            return response;
        }

        public async Task<Response<string>> VerifyEmailAsync(EmailVerificationDto verification)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verification.Email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            if (user.EmailVerified)
            {
                response.Message = "Email already verified.";
                response.Data = user.Id.ToString();
                return response;
            }

            if (user.EmailVerificationCodeExpiresAt == null || user.EmailVerificationCodeExpiresAt < DateTime.UtcNow)
            {
                response.Success = false;
                response.Message = "Codigo expirado. Solicite um novo codigo.";
                return response;
            }

            if (!FixedTimeEquals(user.EmailVerificationCodeHash, HashCode(verification.Code)))
            {
                response.Success = false;
                response.Message = "Codigo invalido.";
                return response;
            }

            user.EmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            user.EmailVerificationCodeHash = null;
            user.EmailVerificationCodeExpiresAt = null;
            await _context.SaveChangesAsync();

            response.Message = "Email verificado.";
            response.Data = user.Id.ToString();
            return response;
        }

        public async Task<Response<string>> ResendEmailVerificationAsync(string email)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            if (user.EmailVerified)
            {
                response.Message = "Email already verified.";
                response.Data = user.Id.ToString();
                return response;
            }

            var code = GenerateCode();
            user.EmailVerificationCodeHash = HashCode(code);
            user.EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(20);
            await _context.SaveChangesAsync();
            await SendEmailCodeAsync(user.Email, code, "Confirme seu email NKZ", "Use este codigo para confirmar seu email na NKZ:");

            response.Message = "Novo codigo enviado para seu email.";
            response.Data = user.Id.ToString();
            return response;
        }

        public async Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto request)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                response.Message = "Se o email existir, enviaremos um codigo de recuperacao.";
                return response;
            }

            var code = GenerateCode();
            user.PasswordResetCodeHash = HashCode(code);
            user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(20);
            await _context.SaveChangesAsync();
            await SendEmailCodeAsync(user.Email, code, "Recuperacao de senha NKZ", "Use este codigo para redefinir sua senha:");

            response.Message = "Se o email existir, enviaremos um codigo de recuperacao.";
            return response;
        }

        public async Task<Response<string>> ResetPasswordAsync(ResetPasswordDto request)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            if (user.PasswordResetCodeExpiresAt == null || user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
            {
                response.Success = false;
                response.Message = "Codigo expirado. Solicite um novo codigo.";
                return response;
            }

            if (!FixedTimeEquals(user.PasswordResetCodeHash, HashCode(request.Code)))
            {
                response.Success = false;
                response.Message = "Codigo invalido.";
                return response;
            }

            _passInterface.CreatePassHash(request.NewPassword, out var passwordHash, out var passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.PasswordResetCodeHash = null;
            user.PasswordResetCodeExpiresAt = null;
            user.TwoFactorCodeHash = null;
            user.TwoFactorSessionHash = null;
            user.TwoFactorCodeExpiresAt = null;
            await _context.SaveChangesAsync();

            response.Message = "Senha atualizada.";
            return response;
        }

        public async Task<Response<string>> VerifyDiscordAsync(DiscordVerificationDto verification)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verification.Email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            if (user.DiscordVerified)
            {
                response.Message = "Discord already verified.";
                response.Data = user.Id.ToString();
                return response;
            }

            if (user.DiscordVerificationCodeExpiresAt == null || user.DiscordVerificationCodeExpiresAt < DateTime.UtcNow)
            {
                response.Success = false;
                response.Message = "Codigo expirado. Solicite um novo codigo.";
                return response;
            }

            if (!FixedTimeEquals(user.DiscordVerificationCodeHash, HashCode(verification.Code)))
            {
                response.Success = false;
                response.Message = "Codigo invalido.";
                return response;
            }

            user.DiscordVerified = true;
            user.DiscordVerifiedAt = DateTime.UtcNow;
            user.DiscordVerificationCodeHash = null;
            user.DiscordVerificationCodeExpiresAt = null;
            await _context.SaveChangesAsync();

            response.Message = "Discord verificado.";
            response.Data = user.Id.ToString();
            return response;
        }

        public async Task<Response<string>> ResendDiscordVerificationAsync(string email)
        {
            var response = new Response<string>();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            if (string.IsNullOrWhiteSpace(user.DiscordUserId))
            {
                response.Success = false;
                response.Message = "Esta conta nao possui Discord vinculado.";
                return response;
            }

            if (user.DiscordVerified)
            {
                response.Message = "Discord already verified.";
                response.Data = user.Id.ToString();
                return response;
            }

            var code = GenerateCode();
            user.DiscordVerificationCodeHash = HashCode(code);
            user.DiscordVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
            await _discordVerificationService.SendVerificationCodeAsync(user.DiscordUserId, user.Email, code);
            await _context.SaveChangesAsync();

            response.Message = "Novo codigo enviado no Discord.";
            response.Data = user.Id.ToString();
            return response;
        }

        public bool VerifyIfUserExists(UserDto User)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == User.Email);
            return user == null;
        }

        private static string NormalizeDiscordIdentity(string? username, string? userId)
        {
            var raw = string.IsNullOrWhiteSpace(username) ? userId : username;
            raw = (raw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) return "";

            var numeric = Regex.Replace(raw, "[^0-9]", "");
            if (Regex.IsMatch(numeric, "^[0-9]{17,20}$")) return numeric;

            raw = raw.TrimStart('@');
            return raw.Length is >= 2 and <= 32 ? raw : "";
        }

        private static string GenerateCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private static string GenerateSecureToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        private bool GetFlag(string key, bool defaultValue)
        {
            return bool.TryParse(_configuration[key], out var configuredValue) ? configuredValue : defaultValue;
        }

        private async Task SendEmailCodeAsync(string email, string code, string subject, string intro)
        {
            var body = $"""
            {intro}

            {code}

            O codigo expira em poucos minutos. Se voce nao pediu isso, ignore esta mensagem.
            """;

            await _emailService.SendAsync(email, subject, body);
        }

        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));
            return Convert.ToHexString(bytes);
        }

        private static bool FixedTimeEquals(string? left, string right)
        {
            if (string.IsNullOrWhiteSpace(left)) return false;
            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);
            return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
