using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddExcludeProductsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exclude_products",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор продукта"),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Наименование продукта")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exclude_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_exclude_products",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя"),
                    exclude_product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Наименование продукта-исключения"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_exclude_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_exclude_products_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_exclude_products_users_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_exclude_products_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_exclude_products_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_exclude_products_created_by_id",
                schema: "app",
                table: "user_exclude_products",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_exclude_products_deleted_by_id",
                schema: "app",
                table: "user_exclude_products",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_exclude_products_updated_by_id",
                schema: "app",
                table: "user_exclude_products",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_exclude_products_user_id_exclude_product",
                schema: "app",
                table: "user_exclude_products",
                columns: new[] { "user_id", "exclude_product" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exclude_products",
                schema: "app");

            migrationBuilder.DropTable(
                name: "user_exclude_products",
                schema: "app");
        }
    }
}
