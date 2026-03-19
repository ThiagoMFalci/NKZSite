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
        public DbSet<Game> Games { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }

    }
}
