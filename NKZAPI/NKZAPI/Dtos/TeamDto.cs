using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class TeamDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "O Campo nome é obrigatorio")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "O nome do time deve ter entre 2 e 40 caracteres.")]
        public string Name { get; set; } = null!;

        [RegularExpression("^[A-Za-z0-9]{3,5}$", ErrorMessage = "A tag deve ter de 3 a 5 letras ou numeros.")]
        public string? Tag { get; set; }

        public Guid? OwnerId { get; set; }
        public bool IsRecruiting { get; set; } = true;
    }
}
