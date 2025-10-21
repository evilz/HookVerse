using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMockEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockEndpoints",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UrlPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResponseStatus = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    ResponseContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResponseDelayMs = table.Column<int>(type: "integer", nullable: false),
                    ResponseHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequestCount = table.Column<int>(type: "integer", nullable: false),
                    LastRequestAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockEndpoints_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalSchema: "hookverse",
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockEndpointRequests",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MockEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    QueryString = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Headers = table.Column<string>(type: "jsonb", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponseStatus = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockEndpointRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockEndpointRequests_MockEndpoints_MockEndpointId",
                        column: x => x.MockEndpointId,
                        principalSchema: "hookverse",
                        principalTable: "MockEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpointRequests_MockEndpointId",
                schema: "hookverse",
                table: "MockEndpointRequests",
                column: "MockEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpointRequests_ReceivedAt",
                schema: "hookverse",
                table: "MockEndpointRequests",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_IsActive",
                schema: "hookverse",
                table: "MockEndpoints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_SubscriberId",
                schema: "hookverse",
                table: "MockEndpoints",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_UrlPath",
                schema: "hookverse",
                table: "MockEndpoints",
                column: "UrlPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockEndpointRequests",
                schema: "hookverse");

            migrationBuilder.DropTable(
                name: "MockEndpoints",
                schema: "hookverse");
        }
    }
}
