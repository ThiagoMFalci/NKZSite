using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class DiscordVerificationDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
