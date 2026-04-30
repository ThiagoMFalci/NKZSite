using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.DiscordServices;
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

        public AuthService(NKZAPIContext context, IPasswordInterface passInterface, IDiscordVerificationService discordVerificationService)
        {
            _context = context;
            _passInterface = passInterface;
            _discordVerificationService = discordVerificationService;
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

                var discordUserId = NormalizeDiscordUserId(User.DiscordUserId);
                if (string.IsNullOrWhiteSpace(discordUserId))
                {
                    response.Data = null;
                    response.Message = "Informe seu Discord ID para criar a conta.";
                    response.Success = false;
                    return response;
                }

                var discordExists = await _context.Users.AnyAsync(u => u.DiscordUserId == discordUserId);
                if (discordExists)
                {
                    response.Data = null;
                    response.Message = "Este Discord ja esta vinculado a outra conta.";
                    response.Success = false;
                    return response;
                }

                _passInterface.CreatePassHash(User.PasswordHash, out byte[] passwordHash, out byte[] passwordSalt);
                var code = GenerateCode();
                var user = new User
                {
                    Email = User.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Player = new List<Player>(),
                    Role = User.Role,
                    DiscordUserId = discordUserId,
                    DiscordVerified = false,
                    DiscordVerificationCodeHash = HashCode(code),
                    DiscordVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15),
                };

                await _discordVerificationService.SendVerificationCodeAsync(discordUserId, User.Email, code);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                response.Data = new UserDto
                {
                    Email = user.Email,
                    PasswordHash = "",
                    PasswordSalt = "",
                    Role = user.Role,
                    DiscordUserId = discordUserId,
                };
                response.Message = "Conta criada. Enviamos um codigo no privado do Discord para confirmacao.";
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.Message = ex.Message;
                response.Success = false;
            }

            return response;
        }

        public async Task<Response<string>> Login(UserLoginDto userLogin)
        {
            var response = new Response<string>();

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

                if (!user.DiscordVerified && !string.IsNullOrWhiteSpace(user.DiscordUserId))
                {
                    response.Data = null;
                    response.Message = "Confirme seu Discord antes de entrar.";
                    response.Success = false;
                    return response;
                }

                response.Message = "Login successful";
                response.Data = _passInterface.CreateToken(user);
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

        private static string NormalizeDiscordUserId(string? value)
        {
            var normalized = Regex.Replace(value ?? "", "[^0-9]", "");
            return Regex.IsMatch(normalized, "^[0-9]{17,20}$") ? normalized : "";
        }

        private static string GenerateCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
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
