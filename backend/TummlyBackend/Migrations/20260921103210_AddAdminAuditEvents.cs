using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorAdminUserId = table.Column<int>(type: "int", nullable: true),
                    ActorIdentity = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: true),
                    DetailJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_Action_OccurredAtUtc",
                table: "AdminAuditEvents",
                columns: new[] { "Action", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_OccurredAtUtc",
                table: "AdminAuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_RestaurantId_OccurredAtUtc",
                table: "AdminAuditEvents",
                columns: new[] { "RestaurantId", "OccurredAtUtc" },
                filter: "[RestaurantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditEvents");
        }
    }
}
