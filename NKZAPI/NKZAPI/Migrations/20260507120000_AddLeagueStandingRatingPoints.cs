using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueStandingRatingPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastRatingChange",
                table: "LeagueStandings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RatingPoints",
                table: "LeagueStandings",
                type: "integer",
                nullable: false,
                defaultValue: 1500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRatingChange",
                table: "LeagueStandings");

            migrationBuilder.DropColumn(
                name: "RatingPoints",
                table: "LeagueStandings");
        }
    }
}
