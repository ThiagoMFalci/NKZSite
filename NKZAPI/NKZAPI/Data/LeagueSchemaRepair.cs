using Microsoft.EntityFrameworkCore;

namespace NKZAPI.Data
{
    public static class LeagueSchemaRepair
    {
        public static async Task EnsureLeagueStandingRatingColumnsAsync(NKZAPIContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "LeagueStandings"
                ADD COLUMN IF NOT EXISTS "RatingPoints" integer NOT NULL DEFAULT 1500;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "LeagueStandings"
                ADD COLUMN IF NOT EXISTS "LastRatingChange" integer NOT NULL DEFAULT 0;
                """);
        }
    }
}
