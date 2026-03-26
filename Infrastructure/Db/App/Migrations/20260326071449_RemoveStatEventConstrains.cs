using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStatEventConstrains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "stat",
                table: "stat_events",
                type: "text",
                nullable: true,
                comment: "Тип события",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true,
                oldComment: "Тип события");

            migrationBuilder.AlterColumn<string>(
                name: "data",
                schema: "stat",
                table: "stat_events",
                type: "text",
                nullable: true,
                comment: "Данные о событии",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Данные о событии");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "stat",
                table: "stat_events",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                comment: "Тип события",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Тип события");

            migrationBuilder.AlterColumn<string>(
                name: "data",
                schema: "stat",
                table: "stat_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Данные о событии",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Данные о событии");
        }
    }
}
