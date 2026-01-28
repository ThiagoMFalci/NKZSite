using System.Data.Entity.Core.Metadata.Edm;

namespace NKZAPI.Models
{
    public class Team
    {
        public string Name { get; set; } = null!;

        public Guid CaptainId { get; set; }
        public Player Captain { get; set; } = null!;

        public ICollection<Player> Members { get; set; } = new List<Player>();
    }
}
