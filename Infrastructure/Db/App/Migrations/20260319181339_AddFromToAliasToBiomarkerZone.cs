using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddFromToAliasToBiomarkerZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "from_to_alias",
                schema: "biomarker",
                table: "biomarker_zones",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Человекочитаемый алиас диапазона (например, 60 - 100, < 50, 110 +)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "from_to_alias",
                schema: "biomarker",
                table: "biomarker_zones");
        }
    }
}
