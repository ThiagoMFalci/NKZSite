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
}
