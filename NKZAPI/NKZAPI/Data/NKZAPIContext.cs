using Microsoft.EntityFrameworkCore;
using NKZAPI.Models;

namespace NKZAPI.Data
{
    public class NKZAPIContext : DbContext
    {
        public NKZAPIContext(DbContextOptions<NKZAPI> options) : base(options)
        {

        }

        protected NKZAPIContext()
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Profiler> Profilers { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }

        public void OnModelCreatin(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
