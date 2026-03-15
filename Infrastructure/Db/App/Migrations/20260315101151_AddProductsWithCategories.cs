using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddProductsWithCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "x5");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "x5",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор категории")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: true, comment: "Идентификатор родительской категории"),
                    title = table.Column<string>(type: "text", nullable: false, comment: "Название категории"),
                    image_url = table.Column<string>(type: "text", nullable: true, comment: "URL изображения категории")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "x5",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "x5",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Идентификатор товара")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор категории"),
                    plu = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "PLU код товара"),
                    title = table.Column<string>(type: "text", nullable: false, comment: "Название товара"),
                    images = table.Column<string>(type: "jsonb", nullable: false, comment: "URL изображений товара"),
                    labels = table.Column<string>(type: "text", nullable: true, comment: "Метки товара"),
                    rating = table.Column<int>(type: "integer", nullable: true, comment: "Рейтинг товара"),
                    kcal_per100_g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, comment: "Ккал на 100 г"),
                    proteins_gper100_g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, comment: "Белки на 100 г"),
                    fats_gper100_g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, comment: "Жиры на 100 г"),
                    carbs_gper100_g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, comment: "Углеводы на 100 г"),
                    allergens = table.Column<string>(type: "text", nullable: true, comment: "Аллергены"),
                    main_ingrediants = table.Column<string>(type: "text", nullable: true, comment: "Основные ингредиенты"),
                    full_ingrediants = table.Column<string>(type: "text", nullable: true, comment: "Полный состав"),
                    Features = table.Column<string>(type: "jsonb", nullable: false, comment: "Характеристики товара"),
                    price = table.Column<int>(type: "integer", nullable: true, comment: "Цена в копейках"),
                    product_type = table.Column<string>(type: "text", nullable: true, comment: "Тип продукта"),
                    manufacturer = table.Column<string>(type: "text", nullable: true, comment: "Производитель"),
                    brand = table.Column<string>(type: "text", nullable: true, comment: "Бренд"),
                    country = table.Column<string>(type: "text", nullable: true, comment: "Страна производства"),
                    shelf_life_days = table.Column<int>(type: "integer", nullable: true, comment: "Срок годности в днях"),
                    weight_g = table.Column<int>(type: "integer", nullable: true, comment: "Вес в граммах"),
                    unit_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Наименование единицы измерения"),
                    volume_ml = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true, comment: "Объём в мл"),
                    is_alcohol = table.Column<bool>(type: "boolean", nullable: true, comment: "Содержит алкоголь"),
                    is_tobacco = table.Column<bool>(type: "boolean", nullable: true, comment: "Табачное изделие"),
                    is_adult_content = table.Column<bool>(type: "boolean", nullable: true, comment: "Контент 18+"),
                    priority = table.Column<int>(type: "integer", nullable: false, comment: "Приоритет сортировки"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, comment: "Активен ли товар")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "x5",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                schema: "x5",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                schema: "x5",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_plu",
                schema: "x5",
                table: "products",
                column: "plu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products",
                schema: "x5");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "x5");
        }
    }
}
