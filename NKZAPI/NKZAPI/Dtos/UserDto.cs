using System.ComponentModel.DataAnnotations;
using NKZAPI.Models;

namespace NKZAPI.Dtos
{
    public class UserDto
    {
        [Required(ErrorMessage = "O Campo Id é obrigatorio")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required(ErrorMessage = "O Campo Data é obrigatorio")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required(ErrorMessage = "O Campo Email é obrigatorio"), EmailAddress(ErrorMessage = "Email invalido!")]
        public string Email { get; set; }
        [Required(ErrorMessage = "O Campo Senha é obrigatorio")]
        public string PasswordHash { get; set; }
        [Required(ErrorMessage = "O Player é obrigatorio")]
        public Player Player { get; set; }
    }
}
