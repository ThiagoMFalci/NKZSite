using System.ComponentModel.DataAnnotations.Schema;

namespace NKZAPI.Models
{
    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        // Navegação inversa necessária para que EF Core resolva o relacionamento many-to-many por convenção
        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
}
