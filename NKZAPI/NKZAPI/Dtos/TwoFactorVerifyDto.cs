using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class TwoFactorVerifyDto
    {
        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(12, MinimumLength = 4)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 16)]
        public string TwoFactorToken { get; set; } = string.Empty;
    }
}
