using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class NKZAPI090226 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_OwnerId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CaptainId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "Players",
                newName: "TeamId2");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId2",
                table: "Players",
                column: "TeamId2",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId2",
                table: "Players",
                column: "TeamId2",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId2",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_TeamId2",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "TeamId2",
                table: "Players",
                newName: "PlayerId");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CaptainId",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_OwnerId",
                table: "Teams",
                column: "OwnerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
