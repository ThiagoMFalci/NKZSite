namespace NKZAPI.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public List<Player> Player { get; set; } = null!;
        public string Role { get; set; } = "User";
        public string? DiscordUserId { get; set; }
        public string? DiscordUsername { get; set; }
        public bool DiscordVerified { get; set; } = true;
        public string? DiscordVerificationCodeHash { get; set; }
        public DateTime? DiscordVerificationCodeExpiresAt { get; set; }
        public DateTime? DiscordVerifiedAt { get; set; }
        public bool EmailVerified { get; set; }
        public string? EmailVerificationCodeHash { get; set; }
        public DateTime? EmailVerificationCodeExpiresAt { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public string? TwoFactorCodeHash { get; set; }
        public string? TwoFactorSessionHash { get; set; }
        public DateTime? TwoFactorCodeExpiresAt { get; set; }
        public string? PasswordResetCodeHash { get; set; }
        public DateTime? PasswordResetCodeExpiresAt { get; set; }
        public decimal WalletBalance { get; set; }
    }
}
