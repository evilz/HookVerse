using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGdprRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GdprRequests",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExportFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExportFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GdprRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GdprRequests_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalSchema: "hookverse",
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GdprRequests_CreatedAt",
                schema: "hookverse",
                table: "GdprRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GdprRequests_RequestType",
                schema: "hookverse",
                table: "GdprRequests",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_GdprRequests_Status",
                schema: "hookverse",
                table: "GdprRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GdprRequests_SubscriberId",
                schema: "hookverse",
                table: "GdprRequests",
                column: "SubscriberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GdprRequests",
                schema: "hookverse");
        }
    }
}
