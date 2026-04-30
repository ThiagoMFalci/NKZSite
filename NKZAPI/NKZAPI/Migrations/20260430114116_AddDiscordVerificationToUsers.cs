using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordVerificationToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordUserId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscordVerificationCodeExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscordVerificationCodeHash",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DiscordVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscordVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscordVerificationCodeExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscordVerificationCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscordVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscordVerifiedAt",
                table: "Users");
        }
    }
}
