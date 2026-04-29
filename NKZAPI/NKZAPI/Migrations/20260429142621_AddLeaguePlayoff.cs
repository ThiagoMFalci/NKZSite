using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaguePlayoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeagueMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bracket = table.Column<string>(type: "text", nullable: false),
                    RoundKey = table.Column<string>(type: "text", nullable: false),
                    RoundName = table.Column<string>(type: "text", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    MatchNumber = table.Column<int>(type: "integer", nullable: false),
                    BestOf = table.Column<int>(type: "integer", nullable: false),
                    TeamAId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamBId = table.Column<Guid>(type: "uuid", nullable: true),
                    WinnerTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoserTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamAScore = table.Column<int>(type: "integer", nullable: false),
                    TeamBScore = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueMatches_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueStandings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    MapsPlayed = table.Column<int>(type: "integer", nullable: false),
                    MapDiff = table.Column<int>(type: "integer", nullable: false),
                    Penalties = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueStandings_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMatches_LeagueId",
                table: "LeagueMatches",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueStandings_LeagueId",
                table: "LeagueStandings",
                column: "LeagueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueMatches");

            migrationBuilder.DropTable(
                name: "LeagueStandings");
        }
    }
}
