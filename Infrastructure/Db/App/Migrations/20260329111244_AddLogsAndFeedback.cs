using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddLogsAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "feedback",
                schema: "app",
                table: "user_feedbacks",
                type: "jsonb",
                nullable: true,
                comment: "Объект фидбека в формате JSON",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldComment: "Объект фидбека в формате JSON");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "feedback",
                schema: "app",
                table: "user_feedbacks",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                comment: "Объект фидбека в формате JSON",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Объект фидбека в формате JSON");
        }
    }
}
