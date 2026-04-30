using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionEligibilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Tournaments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaximumElo",
                table: "Tournaments",
                type: "text",
                nullable: false,
                defaultValue: "CHALLENGER");

            migrationBuilder.AddColumn<string>(
                name: "MinimumElo",
                table: "Tournaments",
                type: "text",
                nullable: false,
                defaultValue: "UNRANKED");

            migrationBuilder.AddColumn<string>(
                name: "Modality",
                table: "Tournaments",
                type: "text",
                nullable: false,
                defaultValue: "Chaveamento");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Tournaments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Leagues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaximumElo",
                table: "Leagues",
                type: "text",
                nullable: false,
                defaultValue: "CHALLENGER");

            migrationBuilder.AddColumn<string>(
                name: "MinimumElo",
                table: "Leagues",
                type: "text",
                nullable: false,
                defaultValue: "UNRANKED");

            migrationBuilder.AddColumn<string>(
                name: "Modality",
                table: "Leagues",
                type: "text",
                nullable: false,
                defaultValue: "Ranking");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Leagues",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "MaximumElo",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "MinimumElo",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Modality",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "MaximumElo",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "MinimumElo",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Modality",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Leagues");
        }
    }
}
