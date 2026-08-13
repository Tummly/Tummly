using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnedLocationId = table.Column<int>(type: "int", nullable: false),
                    OwnedLocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportingPeriodKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReportingPeriodPresetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReportingPeriodStartDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ReportingPeriodEndDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantConversations_RestaurantLocations_OwnedLocationId",
                        column: x => x.OwnedLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantConversations_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssistantMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnedLocationId = table.Column<int>(type: "int", nullable: true),
                    OwnedLocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportingPeriodKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReportingPeriodPresetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReportingPeriodStartDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ReportingPeriodEndDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantMessages_AssistantConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AssistantConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantConversations_OwnedLocationId",
                table: "AssistantConversations",
                column: "OwnedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantConversations_OwnerUserId_LastActivityAt",
                table: "AssistantConversations",
                columns: new[] { "OwnerUserId", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantMessages_ConversationId",
                table: "AssistantMessages",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantMessages");

            migrationBuilder.DropTable(
                name: "AssistantConversations");
        }
    }
}
