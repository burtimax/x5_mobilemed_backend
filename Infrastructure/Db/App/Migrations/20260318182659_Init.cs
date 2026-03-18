using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "biomarker");

            migrationBuilder.EnsureSchema(
                name: "x5");

            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.EnsureSchema(
                name: "stat");

            migrationBuilder.CreateTable(
                name: "biomarkers",
                schema: "biomarker",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор биомаркера")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Уникальный ключ биомаркера"),
                    description = table.Column<string>(type: "text", nullable: false, comment: "Техническое описание параметра"),
                    description_user = table.Column<string>(type: "text", nullable: false, comment: "Описание для пользователя")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_biomarkers", x => x.id);
                });

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
                name: "users",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "biomarker_scales",
                schema: "biomarker",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор шкалы")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    biomarker_id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор биомаркера"),
                    gender_from = table.Column<int>(type: "integer", nullable: false, comment: "Пол от (0=женщина, 1=мужчина)"),
                    gender_to = table.Column<int>(type: "integer", nullable: false, comment: "Пол до (0=женщина, 1=мужчина)"),
                    weight_from = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, comment: "Вес от (кг)"),
                    weight_to = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, comment: "Вес до (кг)"),
                    age_from = table.Column<int>(type: "integer", nullable: false, comment: "Возраст от (лет)"),
                    age_to = table.Column<int>(type: "integer", nullable: false, comment: "Возраст до (лет)"),
                    value_from = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false, comment: "Значение параметра от"),
                    value_to = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false, comment: "Значение параметра до"),
                    relative_to_age = table.Column<bool>(type: "boolean", nullable: false, comment: "Интерпретация относительно возраста пользователя")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_biomarker_scales", x => x.id);
                    table.ForeignKey(
                        name: "fk_biomarker_scales_biomarkers_biomarker_id",
                        column: x => x.biomarker_id,
                        principalSchema: "biomarker",
                        principalTable: "biomarkers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "stat_events",
                schema: "stat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор пользователя"),
                    session_id = table.Column<long>(type: "bigint", nullable: true, comment: "Идентификатор сессии"),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Тип события"),
                    data = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Данные о событии"),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: true, comment: "Длительность выполняемого события"),
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

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя"),
                    age = table.Column<int>(type: "integer", nullable: true, comment: "Возраст (лет)"),
                    height = table.Column<int>(type: "integer", nullable: true, comment: "Рост в см."),
                    weight = table.Column<int>(type: "integer", nullable: true, comment: "Вес в кг"),
                    gender = table.Column<int>(type: "integer", nullable: true, comment: "Пол пользователя: 0 - муж, 1 - жен."),
                    smoke_status = table.Column<int>(type: "integer", nullable: true, comment: "Статус курения: 0 - не курит, 1 - курит"),
                    goals = table.Column<List<string>>(type: "text[]", nullable: true, comment: "Цели пользователя"),
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

            migrationBuilder.CreateTable(
                name: "user_rppg_scans",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД сущности."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sdk_result = table.Column<string>(type: "text", nullable: true),
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
                name: "biomarker_zones",
                schema: "biomarker",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор зоны")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    biomarker_scale_id = table.Column<int>(type: "integer", nullable: false, comment: "Идентификатор шкалы"),
                    zone_key = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Ключ зоны (red/yellow/green)"),
                    value_from = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true, comment: "Начало диапазона значения"),
                    value_to = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true, comment: "Конец диапазона значения"),
                    rule = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Правило интерпретации (для relativeToAge)"),
                    comment = table.Column<string>(type: "text", nullable: false, comment: "Комментарий к зоне")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_biomarker_zones", x => x.id);
                    table.ForeignKey(
                        name: "fk_biomarker_zones_biomarker_scales_biomarker_scale_id",
                        column: x => x.biomarker_scale_id,
                        principalSchema: "biomarker",
                        principalTable: "biomarker_scales",
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
                name: "ix_biomarker_scales_biomarker_id",
                schema: "biomarker",
                table: "biomarker_scales",
                column: "biomarker_id");

            migrationBuilder.CreateIndex(
                name: "ix_biomarker_zones_biomarker_scale_id",
                schema: "biomarker",
                table: "biomarker_zones",
                column: "biomarker_scale_id");

            migrationBuilder.CreateIndex(
                name: "IX_biomarkers_key",
                schema: "biomarker",
                table: "biomarkers",
                column: "key",
                unique: true);

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
                name: "IX_user_exclude_products_user_id_exclude_product",
                schema: "app",
                table: "user_exclude_products",
                columns: new[] { "user_id", "exclude_product" },
                unique: true);

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
                name: "biomarker_zones",
                schema: "biomarker");

            migrationBuilder.DropTable(
                name: "exclude_products",
                schema: "app");

            migrationBuilder.DropTable(
                name: "products",
                schema: "x5");

            migrationBuilder.DropTable(
                name: "stat_events",
                schema: "stat");

            migrationBuilder.DropTable(
                name: "user_exclude_products",
                schema: "app");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "user_rppg_scan_result_items",
                schema: "app");

            migrationBuilder.DropTable(
                name: "biomarker_scales",
                schema: "biomarker");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "x5");

            migrationBuilder.DropTable(
                name: "user_rppg_scans",
                schema: "app");

            migrationBuilder.DropTable(
                name: "biomarkers",
                schema: "biomarker");

            migrationBuilder.DropTable(
                name: "users",
                schema: "app");
        }
    }
}
