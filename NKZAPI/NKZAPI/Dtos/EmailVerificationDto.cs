using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class EmailVerificationDto
    {
        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(12, MinimumLength = 4)]
        public string Code { get; set; } = string.Empty;
    }
}
