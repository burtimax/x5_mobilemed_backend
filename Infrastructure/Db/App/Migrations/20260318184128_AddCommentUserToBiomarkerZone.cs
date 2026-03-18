using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentUserToBiomarkerZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "comment_user",
                schema: "biomarker",
                table: "biomarker_zones",
                type: "text",
                nullable: true,
                comment: "Комментарий к зоне для пользователя");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment_user",
                schema: "biomarker",
                table: "biomarker_zones");
        }
    }
}
