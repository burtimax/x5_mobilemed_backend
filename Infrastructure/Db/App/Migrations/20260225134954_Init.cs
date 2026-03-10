using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stat");

            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stat_events",
                schema: "stat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор пользователя"),
                    session_id = table.Column<long>(type: "bigint", nullable: true, comment: "Идентификатор сессии"),
                    utm = table.Column<string>(type: "text", nullable: true, comment: "UTM метки"),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Тип события"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stat_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_stat_events_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_stat_events_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_stat_events_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_stat_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя"),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "Дата рождения"),
                    gender = table.Column<int>(type: "integer", nullable: true, comment: "Пол пользователя"),
                    additional = table.Column<JsonDocument>(type: "jsonb", nullable: true, comment: "Доп поля пользователя"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_profiles_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_profiles_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stat_events_created_by_id",
                schema: "stat",
                table: "stat_events",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stat_events_deleted_by_id",
                schema: "stat",
                table: "stat_events",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stat_events_updated_by_id",
                schema: "stat",
                table: "stat_events",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stat_events_user_id",
                schema: "stat",
                table: "stat_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_created_by_id",
                schema: "app",
                table: "user_profiles",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_deleted_by_id",
                schema: "app",
                table: "user_profiles",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_updated_by_id",
                schema: "app",
                table: "user_profiles",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_user_id",
                schema: "app",
                table: "user_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stat_events",
                schema: "stat");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "users",
                schema: "app");
        }
    }
}
