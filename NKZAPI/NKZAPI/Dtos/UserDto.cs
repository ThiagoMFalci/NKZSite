using System.ComponentModel.DataAnnotations;
using NKZAPI.Models;

namespace NKZAPI.Dtos
{
    public class UserDto
    {
        [Required(ErrorMessage = "O Campo Email é obrigatorio"), EmailAddress(ErrorMessage = "Email invalido!")]
        public string Email { get; set; }
        [Required(ErrorMessage = "O Campo Senha é obrigatorio")]
        public string PasswordHash { get; set; }
        [Required(ErrorMessage = "O Campo Senha é obrigatorio")]
        public string PasswordSalt { get; set; }
        [Required(ErrorMessage = "O Player é obrigatorio")]

        public string Role { get; set; } = "User";


    }
}
