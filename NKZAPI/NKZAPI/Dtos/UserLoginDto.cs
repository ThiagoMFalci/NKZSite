using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class UserLoginDto
    {
        [Required(ErrorMessage = "O Campo Email é obrigatorio!"), EmailAddress(ErrorMessage = "Email invalido!")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Campo Senha é obrigatorio!")]
        [StringLength(128, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public string? PasswordSalt { get; set; }
    }
}
