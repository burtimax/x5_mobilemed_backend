using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddBiomarkerOrderAndZoneValueAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order",
                schema: "biomarker",
                table: "biomarkers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Порядок отображения");

            migrationBuilder.AddColumn<string>(
                name: "value_alias",
                schema: "biomarker",
                table: "biomarker_zones",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Алиас значения для отображения");

            migrationBuilder.Sql(
                """
                UPDATE biomarker.biomarkers b
                SET "order" = s.rn
                FROM (
                    SELECT id, (ROW_NUMBER() OVER (ORDER BY id) - 1) AS rn
                    FROM biomarker.biomarkers
                ) s
                WHERE b.id = s.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order",
                schema: "biomarker",
                table: "biomarkers");

            migrationBuilder.DropColumn(
                name: "value_alias",
                schema: "biomarker",
                table: "biomarker_zones");
        }
    }
}
