using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaDefinitionEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "hookverse",
                table: "Subscriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                schema: "hookverse",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                schema: "hookverse",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                schema: "hookverse",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "hookverse",
                table: "SchemaDefinitions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "hookverse",
                table: "SchemaDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastValidatedAt",
                schema: "hookverse",
                table: "SchemaDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "hookverse",
                table: "SchemaDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_SchemaDefinitions_IsActive",
                schema: "hookverse",
                table: "SchemaDefinitions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SchemaDefinitions_IsActive",
                schema: "hookverse",
                table: "SchemaDefinitions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "hookverse",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                schema: "hookverse",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                schema: "hookverse",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                schema: "hookverse",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "hookverse",
                table: "SchemaDefinitions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "hookverse",
                table: "SchemaDefinitions");

            migrationBuilder.DropColumn(
                name: "LastValidatedAt",
                schema: "hookverse",
                table: "SchemaDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "hookverse",
                table: "SchemaDefinitions");
        }
    }
}
