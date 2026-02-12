using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class NKZAPI100226 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId2",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_TeamId2",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamId2",
                table: "Players");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Teams",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Teams");

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId2",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId2",
                table: "Players",
                column: "TeamId2",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId2",
                table: "Players",
                column: "TeamId2",
                principalTable: "Teams",
                principalColumn: "Id");
        }
    }
}
