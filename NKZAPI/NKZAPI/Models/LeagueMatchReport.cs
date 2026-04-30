namespace NKZAPI.Models
{
    public class LeagueMatchReport : BaseEntity
    {
        public Guid LeagueMatchId { get; set; }
        public Guid TeamId { get; set; }
        public Guid ReportedWinnerTeamId { get; set; }
        public Guid SubmittedByUserId { get; set; }
        public string ProofImageUrl { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
