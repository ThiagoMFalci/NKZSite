using System.ComponentModel.DataAnnotations.Schema;

namespace NKZAPI.Models
{
    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

    }
}
