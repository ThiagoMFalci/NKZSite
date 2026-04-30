namespace NKZAPI.Models
{
    public class LeagueQueueEntry : BaseEntity
    {
        public Guid LeagueId { get; set; }
        public Guid TeamId { get; set; }
        public string Status { get; set; } = "Waiting";
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? MatchedAt { get; set; }
        public Guid? MatchId { get; set; }
    }
}
