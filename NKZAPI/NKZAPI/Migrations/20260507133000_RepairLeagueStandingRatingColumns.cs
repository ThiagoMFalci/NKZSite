using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class RepairLeagueStandingRatingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "LeagueStandings"
                ADD COLUMN IF NOT EXISTS "RatingPoints" integer NOT NULL DEFAULT 1500;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "LeagueStandings"
                ADD COLUMN IF NOT EXISTS "LastRatingChange" integer NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
