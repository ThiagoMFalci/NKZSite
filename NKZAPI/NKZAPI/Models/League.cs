using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Models
{
    public class League : BaseEntity
    {
        [Required]
        [StringLength(60, MinimumLength = 4)]
        public string Name { get; set; } = string.Empty;

        public List<Team> Teams { get; set; } = new();

        [Range(0, 100000)]
        public float Award { get; set; }

        [Range(0, 10000)]
        public float EntryFee { get; set; }

        [StringLength(512)]
        public string? ImageUrl { get; set; }

        [Range(2, 64)]
        public int MaxTeams { get; set; }

        [StringLength(24)]
        public string MinimumElo { get; set; } = "UNRANKED";

        [StringLength(24)]
        public string MaximumElo { get; set; } = "CHALLENGER";

        [Range(0, 999999)]
        public int MinimumTeamPoints { get; set; }

        [Range(0, 999999)]
        public int MaximumTeamPoints { get; set; } = 999999;

        public TimeSpan? RankingQueueOpenTime { get; set; }
        public TimeSpan? RankingQueueCloseTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [RegularExpression("^(Ranking|Chaveamento)$")]
        public string Modality { get; set; } = "Ranking";

        public List<LeagueMatch> Matches { get; set; } = new();
        public List<LeagueStanding> Standings { get; set; } = new();
        public List<LeagueQueueEntry> QueueEntries { get; set; } = new();
    }
}
