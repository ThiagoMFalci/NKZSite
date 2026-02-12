using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class NKZAPI070226 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_PlayerId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_PlayerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Teams");

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

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId1",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId1",
                table: "Players",
                column: "TeamId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId1",
                table: "Players",
                column: "TeamId1",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId1",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_OwnerId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Players_TeamId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_TeamId1",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CaptainId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamId1",
                table: "Players");

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_PlayerId",
                table: "Teams",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_PlayerId",
                table: "Teams",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
