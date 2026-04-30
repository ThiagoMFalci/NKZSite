using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRankingQueueCloseTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "RankingQueueCloseTime",
                table: "Leagues",
                type: "interval",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RankingQueueCloseTime",
                table: "Leagues");
        }
    }
}
