using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFeedbackAndApplicationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_logs",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор пользователя (если известен)"),
                    log_source = table.Column<int>(type: "integer", nullable: true, comment: "Источник лога: бэкенд или фронтенд"),
                    log_type = table.Column<string>(type: "text", nullable: true, comment: "Тип/категория лога"),
                    log = table.Column<string>(type: "jsonb", nullable: true, comment: "Структурированные данные лога в формате JSON"),
                    log_message = table.Column<string>(type: "text", nullable: true, comment: "Текстовое сообщение лога"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_application_logs_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_logs_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_logs_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_logs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_feedbacks",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя"),
                    feedback = table.Column<string>(type: "jsonb", nullable: false, comment: "Объект фидбека в формате JSON"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_feedbacks_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_feedbacks_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_feedbacks_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_feedbacks_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_logs_created_by_id",
                schema: "app",
                table: "application_logs",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_logs_deleted_by_id",
                schema: "app",
                table: "application_logs",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_logs_updated_by_id",
                schema: "app",
                table: "application_logs",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_logs_user_id",
                schema: "app",
                table: "application_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_feedbacks_created_by_id",
                schema: "app",
                table: "user_feedbacks",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_feedbacks_deleted_by_id",
                schema: "app",
                table: "user_feedbacks",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_feedbacks_updated_by_id",
                schema: "app",
                table: "user_feedbacks",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_feedbacks_user_id",
                schema: "app",
                table: "user_feedbacks",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_logs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "user_feedbacks",
                schema: "app");
        }
    }
}
