using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class Changes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source",
                schema: "app",
                table: "user_rppg_scans");

            migrationBuilder.DropColumn(
                name: "taken_at",
                schema: "app",
                table: "user_rppg_scans");

            migrationBuilder.DropColumn(
                name: "user_info_json",
                schema: "app",
                table: "user_rppg_scans");

            migrationBuilder.AddColumn<string>(
                name: "sdk_result",
                schema: "app",
                table: "user_rppg_scans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sdk_result",
                schema: "app",
                table: "user_rppg_scans");

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "app",
                table: "user_rppg_scans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "taken_at",
                schema: "app",
                table: "user_rppg_scans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "user_info_json",
                schema: "app",
                table: "user_rppg_scans",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
