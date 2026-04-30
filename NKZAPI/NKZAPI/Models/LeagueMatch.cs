namespace NKZAPI.Models
{
    public class LeagueMatch : BaseEntity
    {
        public Guid LeagueId { get; set; }
        public string Bracket { get; set; } = "Upper";
        public string RoundKey { get; set; } = "";
        public string RoundName { get; set; } = "";
        public int WeekNumber { get; set; }
        public int MatchNumber { get; set; }
        public int BestOf { get; set; } = 1;
        public Guid? TeamAId { get; set; }
        public Guid? TeamBId { get; set; }
        public Guid? WinnerTeamId { get; set; }
        public Guid? LoserTeamId { get; set; }
        public int TeamAScore { get; set; }
        public int TeamBScore { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ProposedScheduledAt { get; set; }
        public Guid? ProposedByTeamId { get; set; }
        public string ScheduleStatus { get; set; } = "Open";
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public List<LeagueMatchReport> Reports { get; set; } = new();
    }
}
