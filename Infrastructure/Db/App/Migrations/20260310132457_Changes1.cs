using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class Changes1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "external_id",
                schema: "app",
                table: "users");

            migrationBuilder.DropColumn(
                name: "additional",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "birth_date",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "utm",
                schema: "stat",
                table: "stat_events");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                schema: "app",
                table: "user_profiles",
                type: "integer",
                nullable: true,
                comment: "Пол пользователя: 0 - муж, 1 - жен.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Пол пользователя");

            migrationBuilder.AddColumn<int>(
                name: "age",
                schema: "app",
                table: "user_profiles",
                type: "integer",
                nullable: true,
                comment: "Возраст (лет)");

            migrationBuilder.AddColumn<List<string>>(
                name: "goals",
                schema: "app",
                table: "user_profiles",
                type: "text[]",
                nullable: true,
                comment: "Цели пользователя");

            migrationBuilder.AddColumn<int>(
                name: "height",
                schema: "app",
                table: "user_profiles",
                type: "integer",
                nullable: true,
                comment: "Рост в см.");

            migrationBuilder.AddColumn<int>(
                name: "smoke_status",
                schema: "app",
                table: "user_profiles",
                type: "integer",
                nullable: true,
                comment: "Статус курения: 0 - не курит, 1 - курит");

            migrationBuilder.AddColumn<int>(
                name: "weight",
                schema: "app",
                table: "user_profiles",
                type: "integer",
                nullable: true,
                comment: "Вес в кг");

            migrationBuilder.AddColumn<string>(
                name: "data",
                schema: "stat",
                table: "stat_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Данные о событии");

            migrationBuilder.AddColumn<double>(
                name: "duration_seconds",
                schema: "stat",
                table: "stat_events",
                type: "double precision",
                nullable: true,
                comment: "Длительность выполняемого события");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "age",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "goals",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "height",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "smoke_status",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "weight",
                schema: "app",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "data",
                schema: "stat",
                table: "stat_events");

            migrationBuilder.DropColumn(
                name: "duration_seconds",
                schema: "stat",
                table: "stat_events");

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                schema: "app",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                schema: "app",
                table: "user_profiles",
                type: "integer",
                nullable: true,
                comment: "Пол пользователя",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Пол пользователя: 0 - муж, 1 - жен.");

            migrationBuilder.AddColumn<JsonDocument>(
                name: "additional",
                schema: "app",
                table: "user_profiles",
                type: "jsonb",
                nullable: true,
                comment: "Доп поля пользователя");

            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                schema: "app",
                table: "user_profiles",
                type: "date",
                nullable: true,
                comment: "Дата рождения");

            migrationBuilder.AddColumn<string>(
                name: "utm",
                schema: "stat",
                table: "stat_events",
                type: "text",
                nullable: true,
                comment: "UTM метки");
        }
    }
}
