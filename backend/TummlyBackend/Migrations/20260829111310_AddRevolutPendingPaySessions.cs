using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRevolutPendingPaySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevolutPendingPaySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    TargetPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetCadence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RevolutSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SetupOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevolutPendingPaySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevolutPendingPaySessions_BillingAccounts_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "BillingAccounts",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevolutPendingPaySessions_RestaurantId_IdempotencyKey",
                table: "RevolutPendingPaySessions",
                columns: new[] { "RestaurantId", "IdempotencyKey" });

            migrationBuilder.CreateIndex(
                name: "IX_RevolutPendingPaySessions_RestaurantId_IsOpen",
                table: "RevolutPendingPaySessions",
                columns: new[] { "RestaurantId", "IsOpen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevolutPendingPaySessions");
        }
    }
}
