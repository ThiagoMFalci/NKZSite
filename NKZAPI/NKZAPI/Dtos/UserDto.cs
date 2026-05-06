using System.ComponentModel.DataAnnotations;
using NKZAPI.Models;

namespace NKZAPI.Dtos
{
    public class UserDto
    {
        [Required(ErrorMessage = "O Campo Email é obrigatorio"), EmailAddress(ErrorMessage = "Email invalido!")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Campo Senha é obrigatorio")]
        [StringLength(128, MinimumLength = 6)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Campo Senha é obrigatorio")]
        [Compare(nameof(PasswordHash), ErrorMessage = "As senhas nao conferem.")]
        public string PasswordSalt { get; set; } = string.Empty;

        [RegularExpression("^(User|Admin)$")]
        public string Role { get; set; } = "User";

        [StringLength(32)]
        public string DiscordUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(32, MinimumLength = 2)]
        public string DiscordUsername { get; set; } = string.Empty;


    }
}
