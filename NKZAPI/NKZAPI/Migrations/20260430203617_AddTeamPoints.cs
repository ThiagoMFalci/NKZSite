using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NKZAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ""Teams"" AS t
                SET ""Points"" = COALESCE(scores.""Points"", 0)
                FROM (
                    SELECT
                        p.""TeamId"",
                        SUM(
                            CASE UPPER(COALESCE(p.""SoloQueueTier"", ''))
                                WHEN 'IRON' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 1 WHEN 'III' THEN 2 WHEN 'II' THEN 3 WHEN 'I' THEN 4 ELSE 0 END
                                WHEN 'BRONZE' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 5 WHEN 'III' THEN 6 WHEN 'II' THEN 7 WHEN 'I' THEN 8 ELSE 0 END
                                WHEN 'SILVER' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 10 WHEN 'III' THEN 11 WHEN 'II' THEN 12 WHEN 'I' THEN 13 ELSE 0 END
                                WHEN 'GOLD' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 20 WHEN 'III' THEN 21 WHEN 'II' THEN 22 WHEN 'I' THEN 23 ELSE 0 END
                                WHEN 'PLATINUM' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 30 WHEN 'III' THEN 31 WHEN 'II' THEN 32 WHEN 'I' THEN 33 ELSE 0 END
                                WHEN 'EMERALD' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 40 WHEN 'III' THEN 41 WHEN 'II' THEN 42 WHEN 'I' THEN 43 ELSE 0 END
                                WHEN 'DIAMOND' THEN CASE UPPER(COALESCE(p.""SoloQueueRank"", ''))
                                    WHEN 'IV' THEN 55 WHEN 'III' THEN 60 WHEN 'II' THEN 70 WHEN 'I' THEN 80 ELSE 0 END
                                WHEN 'MASTER' THEN CASE WHEN COALESCE(p.""SoloQueueLP"", 0) >= 300 THEN 150 ELSE 100 END
                                WHEN 'GRANDMASTER' THEN 175
                                WHEN 'CHALLENGER' THEN 200
                                ELSE 0
                            END
                        )::integer AS ""Points""
                    FROM ""Players"" AS p
                    WHERE p.""TeamId"" IS NOT NULL
                    GROUP BY p.""TeamId""
                ) AS scores
                WHERE t.""Id"" = scores.""TeamId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Teams");
        }
    }
}
