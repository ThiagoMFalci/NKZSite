using System.Data.Entity.Core.Metadata.Edm;

namespace NKZAPI.Models
{
    public class Player : User
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public List<Profiler> Role = new List<Profiler>();

        // Riot
        public string SummonerName { get; set; } = null!;
        public string RiotPuuid { get; set; } = null!;
        public int SummonerLevel { get; set; }

        public string SoloQueueTier { get; set; } = "UNRANKED";
        public string SoloQueueRank { get; set; } = "";
        public int SoloQueueLP { get; set; }

        // Stats agregadas
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        // Controle
        public DateTime LastStatsUpdate { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}
