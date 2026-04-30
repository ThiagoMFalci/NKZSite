namespace NKZAPI.Dtos
{
    public class UserPublicDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? DiscordUserId { get; set; }
        public string? DiscordUsername { get; set; }
        public bool DiscordVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
