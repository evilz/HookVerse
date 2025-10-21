using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hookverse");

            migrationBuilder.CreateTable(
                name: "Subscribers",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTypes",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTypes_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalSchema: "hookverse",
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchemaDefinitions",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemaDefinitions_EventTypes_EventTypeId",
                        column: x => x.EventTypeId,
                        principalSchema: "hookverse",
                        principalTable: "EventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Secret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FilterExpression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomHeaders = table.Column<string>(type: "text", nullable: true),
                    AuthType = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AuthConfig = table.Column<string>(type: "text", nullable: true),
                    LastDeliveryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_EventTypes_EventTypeId",
                        column: x => x.EventTypeId,
                        principalSchema: "hookverse",
                        principalTable: "EventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalSchema: "hookverse",
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadSizeBytes = table.Column<int>(type: "integer", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_EventTypes_EventTypeId",
                        column: x => x.EventTypeId,
                        principalSchema: "hookverse",
                        principalTable: "EventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalSchema: "hookverse",
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAttempts",
                schema: "hookverse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    RequestHeaders = table.Column<string>(type: "text", nullable: true),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    ResponseStatus = table.Column<int>(type: "integer", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "text", nullable: true),
                    ResponseBody = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Signature = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryAttempts_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "hookverse",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryAttempts_WebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalSchema: "hookverse",
                        principalTable: "WebhookEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_StartedAt",
                schema: "hookverse",
                table: "DeliveryAttempts",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_Status",
                schema: "hookverse",
                table: "DeliveryAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_Status_NextRetryAt",
                schema: "hookverse",
                table: "DeliveryAttempts",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_SubscriptionId",
                schema: "hookverse",
                table: "DeliveryAttempts",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_WebhookEventId",
                schema: "hookverse",
                table: "DeliveryAttempts",
                column: "WebhookEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_WebhookEventId_AttemptNumber",
                schema: "hookverse",
                table: "DeliveryAttempts",
                columns: new[] { "WebhookEventId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_IsActive",
                schema: "hookverse",
                table: "EventTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_Name_Version_SubscriberId",
                schema: "hookverse",
                table: "EventTypes",
                columns: new[] { "Name", "Version", "SubscriberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_SubscriberId",
                schema: "hookverse",
                table: "EventTypes",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaDefinitions_ContentHash",
                schema: "hookverse",
                table: "SchemaDefinitions",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaDefinitions_EventTypeId",
                schema: "hookverse",
                table: "SchemaDefinitions",
                column: "EventTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_ApiKeyHash",
                schema: "hookverse",
                table: "Subscribers",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_Email",
                schema: "hookverse",
                table: "Subscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_EventTypeId",
                schema: "hookverse",
                table: "Subscriptions",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_EventTypeId_IsActive",
                schema: "hookverse",
                table: "Subscriptions",
                columns: new[] { "EventTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IsActive",
                schema: "hookverse",
                table: "Subscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriberId",
                schema: "hookverse",
                table: "Subscriptions",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_CreatedAt",
                schema: "hookverse",
                table: "WebhookEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventTypeId",
                schema: "hookverse",
                table: "WebhookEvents",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ScheduledFor",
                schema: "hookverse",
                table: "WebhookEvents",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_SubscriberId",
                schema: "hookverse",
                table: "WebhookEvents",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_SubscriberId_CreatedAt",
                schema: "hookverse",
                table: "WebhookEvents",
                columns: new[] { "SubscriberId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryAttempts",
                schema: "hookverse");

            migrationBuilder.DropTable(
                name: "SchemaDefinitions",
                schema: "hookverse");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "hookverse");

            migrationBuilder.DropTable(
                name: "WebhookEvents",
                schema: "hookverse");

            migrationBuilder.DropTable(
                name: "EventTypes",
                schema: "hookverse");

            migrationBuilder.DropTable(
                name: "Subscribers",
                schema: "hookverse");
        }
    }
}
