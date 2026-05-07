namespace NKZAPI.Dtos
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalTeams { get; set; }
        public int TotalLeagues { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal SubscriptionRevenue { get; set; }
        public decimal WalletRevenue { get; set; }
        public decimal LeagueRevenue { get; set; }
        public List<object> RecentUsers { get; set; } = new();
        public List<object> RecentSubscriptions { get; set; } = new();
        public List<object> RecentPayments { get; set; } = new();
    }

    public class AdminUpdateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? DiscordUsername { get; set; }
        public bool DiscordVerified { get; set; }
        public bool EmailVerified { get; set; }
        public decimal WalletBalance { get; set; }
    }

    public class AdminUpdateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public Guid? OwnerId { get; set; }
        public bool IsRecruiting { get; set; }
        public int Points { get; set; }
    }

    public class AdminUpdateLeagueDto
    {
        public string Name { get; set; } = string.Empty;
        public string Modality { get; set; } = "Ranking";
        public float EntryFee { get; set; }
        public float Award { get; set; }
        public int MinimumTeamPoints { get; set; }
        public int MaximumTeamPoints { get; set; }
        public int MaxTeams { get; set; }
        public string MinimumElo { get; set; } = "UNRANKED";
        public string MaximumElo { get; set; } = "CHALLENGER";
        public string? ImageUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? RankingQueueOpenTime { get; set; }
        public TimeSpan? RankingQueueCloseTime { get; set; }
    }
}
