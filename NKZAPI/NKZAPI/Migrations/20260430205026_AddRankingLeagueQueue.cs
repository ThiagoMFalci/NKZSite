using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRankingLeagueQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumTeamPoints",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 999999);

            migrationBuilder.AddColumn<int>(
                name: "MinimumTeamPoints",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RankingQueueOpenTime",
                table: "Leagues",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "LeagueMatches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LeagueQueueEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueQueueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueQueueEntries_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueQueueEntries_LeagueId",
                table: "LeagueQueueEntries",
                column: "LeagueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueQueueEntries");

            migrationBuilder.DropColumn(
                name: "MaximumTeamPoints",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "MinimumTeamPoints",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "RankingQueueOpenTime",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "LeagueMatches");
        }
    }
}
