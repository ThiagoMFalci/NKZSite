using NKZAPI.Models;

namespace NKZAPI.Dtos
{
    public class PlayerPublicDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TeamId { get; set; }
        public bool IsCaptain { get; set; }
        public string MainRole { get; set; } = "Flex";
        public bool LookingForTeam { get; set; }
        public string Tags { get; set; } = "";
        public string? ProfileImageUrl { get; set; }
        public string? DiscordUsername { get; set; }
        public string SummonerName { get; set; } = "";
        public int SummonerLevel { get; set; }
        public string SoloQueueTier { get; set; } = "UNRANKED";
        public string SoloQueueRank { get; set; } = "";
        public int SoloQueueLP { get; set; }
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public DateTime LastStatsUpdate { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public List<PlayerChampionStat> ChampionStats { get; set; } = new();
        public List<PlayerRoleStat> RoleStats { get; set; } = new();
        public List<PlayerMatchHistory> MatchHistory { get; set; } = new();
    }

    public class TeamPublicDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Tag { get; set; }
        public Guid? OwnerId { get; set; }
        public bool IsRecruiting { get; set; }
        public int Points { get; set; }
        public string? ProfileImageUrl { get; set; }
        public ICollection<PlayerPublicDto> Players { get; set; } = new List<PlayerPublicDto>();
    }

    public class LeaguePublicDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public List<TeamPublicDto> Teams { get; set; } = new();
        public float Award { get; set; }
        public float EntryFee { get; set; }
        public string? ImageUrl { get; set; }
        public int MaxTeams { get; set; }
        public string MinimumElo { get; set; } = "UNRANKED";
        public string MaximumElo { get; set; } = "CHALLENGER";
        public int MinimumTeamPoints { get; set; }
        public int MaximumTeamPoints { get; set; }
        public TimeSpan? RankingQueueOpenTime { get; set; }
        public TimeSpan? RankingQueueCloseTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Modality { get; set; } = "Ranking";
        public List<LeagueMatchPublicDto> Matches { get; set; } = new();
        public List<LeagueStanding> Standings { get; set; } = new();
        public List<LeagueQueueEntry> QueueEntries { get; set; } = new();
    }

    public class LeagueMatchPublicDto
    {
        public Guid Id { get; set; }
        public Guid LeagueId { get; set; }
        public string Bracket { get; set; } = "Upper";
        public string RoundKey { get; set; } = "";
        public string RoundName { get; set; } = "";
        public int WeekNumber { get; set; }
        public int MatchNumber { get; set; }
        public int BestOf { get; set; }
        public Guid? TeamAId { get; set; }
        public Guid? TeamBId { get; set; }
        public Guid? WinnerTeamId { get; set; }
        public Guid? LoserTeamId { get; set; }
        public int TeamAScore { get; set; }
        public int TeamBScore { get; set; }
        public string AccessCode { get; set; } = "";
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ProposedScheduledAt { get; set; }
        public Guid? ProposedByTeamId { get; set; }
        public string ScheduleStatus { get; set; } = "Open";
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public List<LeagueMatchReportPublicDto> Reports { get; set; } = new();
    }

    public class LeagueMatchReportPublicDto
    {
        public Guid Id { get; set; }
        public Guid LeagueMatchId { get; set; }
        public Guid TeamId { get; set; }
        public Guid ReportedWinnerTeamId { get; set; }
        public string ProofImageUrl { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
