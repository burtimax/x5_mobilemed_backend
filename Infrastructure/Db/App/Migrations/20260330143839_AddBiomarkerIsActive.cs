using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddBiomarkerIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "biomarker",
                table: "biomarkers",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Активен для отображения в сканах");

            migrationBuilder.Sql(
                """
                UPDATE biomarker.biomarkers
                SET is_active = false
                WHERE key IN (
                    'normalizedStressIndex',
                    'wellnessLevel',
                    'wellnessIndex',
                    'ascvdRiskLevel'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "biomarker",
                table: "biomarkers");
        }
    }
}
