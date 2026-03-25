using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekRationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "week_rations",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя"),
                    rppg_scan_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор скана RPPG"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_week_rations", x => x.id);
                    table.ForeignKey(
                        name: "fk_week_rations_user_rppg_scans_rppg_scan_id",
                        column: x => x.rppg_scan_id,
                        principalSchema: "app",
                        principalTable: "user_rppg_scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_week_rations_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_rations_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_rations_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_rations_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "week_ration_items",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    week_ration_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор рациона"),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Тип приёма пищи"),
                    day = table.Column<int>(type: "integer", nullable: false, comment: "День недели (1–7)"),
                    product_id = table.Column<long>(type: "bigint", nullable: false, comment: "Идентификатор товара из каталога X5"),
                    weigth = table.Column<int>(type: "integer", nullable: false, comment: "Вес порции, г"),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Краткая причина включения товара"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_week_ration_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_week_ration_items_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "x5",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_week_ration_items_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_ration_items_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_ration_items_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_ration_items_week_rations_week_ration_id",
                        column: x => x.week_ration_id,
                        principalSchema: "app",
                        principalTable: "week_rations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "week_ration_item_replaces",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    week_ration_item_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор позиции рациона"),
                    product_id = table.Column<long>(type: "bigint", nullable: false, comment: "Идентификатор товара-замены из каталога X5"),
                    weight = table.Column<int>(type: "integer", nullable: false, comment: "Вес порции замены, г"),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Пояснение к замене"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_week_ration_item_replaces", x => x.id);
                    table.ForeignKey(
                        name: "fk_week_ration_item_replaces_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "x5",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_week_ration_item_replaces_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_ration_item_replaces_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_ration_item_replaces_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_week_ration_item_replaces_week_ration_items_week_ration_ite~",
                        column: x => x.week_ration_item_id,
                        principalSchema: "app",
                        principalTable: "week_ration_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_item_replaces_created_by_id",
                schema: "app",
                table: "week_ration_item_replaces",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_item_replaces_deleted_by_id",
                schema: "app",
                table: "week_ration_item_replaces",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_item_replaces_product_id",
                schema: "app",
                table: "week_ration_item_replaces",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_item_replaces_updated_by_id",
                schema: "app",
                table: "week_ration_item_replaces",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_item_replaces_week_ration_item_id",
                schema: "app",
                table: "week_ration_item_replaces",
                column: "week_ration_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_items_created_by_id",
                schema: "app",
                table: "week_ration_items",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_items_deleted_by_id",
                schema: "app",
                table: "week_ration_items",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_items_product_id",
                schema: "app",
                table: "week_ration_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_items_updated_by_id",
                schema: "app",
                table: "week_ration_items",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_ration_items_week_ration_id",
                schema: "app",
                table: "week_ration_items",
                column: "week_ration_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_rations_created_by_id",
                schema: "app",
                table: "week_rations",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_rations_deleted_by_id",
                schema: "app",
                table: "week_rations",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_rations_rppg_scan_id",
                schema: "app",
                table: "week_rations",
                column: "rppg_scan_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_rations_updated_by_id",
                schema: "app",
                table: "week_rations",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_week_rations_user_id",
                schema: "app",
                table: "week_rations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "week_ration_item_replaces",
                schema: "app");

            migrationBuilder.DropTable(
                name: "week_ration_items",
                schema: "app");

            migrationBuilder.DropTable(
                name: "week_rations",
                schema: "app");
        }
    }
}
