using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddNameAndUnitToBiomarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "biomarker",
                table: "biomarkers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                comment: "Название показателя");

            migrationBuilder.AddColumn<string>(
                name: "unit",
                schema: "biomarker",
                table: "biomarkers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Единица измерения");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                schema: "biomarker",
                table: "biomarkers");

            migrationBuilder.DropColumn(
                name: "unit",
                schema: "biomarker",
                table: "biomarkers");
        }
    }
}
