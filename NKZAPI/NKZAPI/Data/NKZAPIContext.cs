using Microsoft.EntityFrameworkCore;
using NKZAPI.Models;

namespace NKZAPI.Data
{
    public class NKZAPIContext : DbContext
    {
        public NKZAPIContext(DbContextOptions<NKZAPIContext> options) : base(options)
        {

        }

        protected NKZAPIContext()
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerChampionStat> PlayerChampionStats { get; set; }
        public DbSet<PlayerRoleStat> PlayerRoleStats { get; set; }
        public DbSet<PlayerMatchHistory> PlayerMatchHistory { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<LeagueMatch> LeagueMatches { get; set; }
        public DbSet<LeagueMatchReport> LeagueMatchReports { get; set; }
        public DbSet<LeagueQueueEntry> LeagueQueueEntries { get; set; }
        public DbSet<LeaguePayment> LeaguePayments { get; set; }
        public DbSet<LeagueStanding> LeagueStandings { get; set; }
        public DbSet<WalletPayment> WalletPayments { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SubscriptionPlan>()
                .Property(item => item.Price)
                .HasPrecision(12, 2);

            modelBuilder.Entity<UserSubscription>()
                .Property(item => item.Amount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<UserSubscription>()
                .HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSubscription>()
                .HasOne(item => item.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(item => item.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
