using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRevolutWebhookInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevolutWebhookInboxItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RawBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryAfterUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevolutWebhookInboxItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevolutWebhookInboxItems_PayloadHash",
                table: "RevolutWebhookInboxItems",
                column: "PayloadHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevolutWebhookInboxItems_Status_RetryAfterUtc_ClaimedAtUtc_CreatedAtUtc",
                table: "RevolutWebhookInboxItems",
                columns: new[] { "Status", "RetryAfterUtc", "ClaimedAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevolutWebhookInboxItems");
        }
    }
}
