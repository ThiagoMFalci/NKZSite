using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class UserLoginDto
    {
        [Required(ErrorMessage = "O Campo Email é obrigatorio!"), EmailAddress(ErrorMessage = "Email invalido!")]
        public string Email { get; set; }
        [Required(ErrorMessage = "O Campo Senha é obrigatorio!")]
        public string Password { get; set; }
        [Compare("Password", ErrorMessage ="Senhas não são iguais!")]
        public string PasswordSalt { get; set; }
    }
}
