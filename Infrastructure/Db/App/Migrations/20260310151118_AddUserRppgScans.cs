using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRppgScans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_rppg_scans",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    taken_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_info_json = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_rppg_scans", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_rppg_scans_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_rppg_scans_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_rppg_scans_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_rppg_scans_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_rppg_scan_result_items",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    confidence_level = table.Column<int>(type: "integer", nullable: true),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_rppg_scan_result_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_rppg_scan_result_items_user_rppg_scans_scan_id",
                        column: x => x.scan_id,
                        principalSchema: "app",
                        principalTable: "user_rppg_scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_rppg_scan_result_items_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_rppg_scan_result_items_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_rppg_scan_result_items_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scan_result_items_created_by_id",
                schema: "app",
                table: "user_rppg_scan_result_items",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scan_result_items_deleted_by_id",
                schema: "app",
                table: "user_rppg_scan_result_items",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scan_result_items_scan_id",
                schema: "app",
                table: "user_rppg_scan_result_items",
                column: "scan_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scan_result_items_updated_by_id",
                schema: "app",
                table: "user_rppg_scan_result_items",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scans_created_by_id",
                schema: "app",
                table: "user_rppg_scans",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scans_deleted_by_id",
                schema: "app",
                table: "user_rppg_scans",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scans_updated_by_id",
                schema: "app",
                table: "user_rppg_scans",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_rppg_scans_user_id",
                schema: "app",
                table: "user_rppg_scans",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_rppg_scan_result_items",
                schema: "app");

            migrationBuilder.DropTable(
                name: "user_rppg_scans",
                schema: "app");
        }
    }
}
