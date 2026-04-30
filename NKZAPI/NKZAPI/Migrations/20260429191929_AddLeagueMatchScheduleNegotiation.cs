using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueMatchScheduleNegotiation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProposedByTeamId",
                table: "LeagueMatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedScheduledAt",
                table: "LeagueMatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleStatus",
                table: "LeagueMatches",
                type: "text",
                nullable: false,
                defaultValue: "Open");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedByTeamId",
                table: "LeagueMatches");

            migrationBuilder.DropColumn(
                name: "ProposedScheduledAt",
                table: "LeagueMatches");

            migrationBuilder.DropColumn(
                name: "ScheduleStatus",
                table: "LeagueMatches");
        }
    }
}
