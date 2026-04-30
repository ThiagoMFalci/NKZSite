namespace NKZAPI.Models
{
    public class League : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<Team> Teams { get; set; } = new List<Team>();
        public float Award { get; set; }
        public float EntryFee { get; set; }
        public string? ImageUrl { get; set; }
        public int MaxTeams { get; set; }
        public string MinimumElo { get; set; } = "UNRANKED";
        public string MaximumElo { get; set; } = "CHALLENGER";
        public int MinimumTeamPoints { get; set; }
        public int MaximumTeamPoints { get; set; } = 999999;
        public TimeSpan? RankingQueueOpenTime { get; set; }
        public TimeSpan? RankingQueueCloseTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Modality { get; set; } = "Ranking";
        public List<LeagueMatch> Matches { get; set; } = new();
        public List<LeagueStanding> Standings { get; set; } = new();
        public List<LeagueQueueEntry> QueueEntries { get; set; } = new();


    }
}
