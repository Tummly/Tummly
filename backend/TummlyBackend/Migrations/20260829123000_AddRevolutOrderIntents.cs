using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TummlyBackend.Data;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260829123000_AddRevolutOrderIntents")]
    public partial class AddRevolutOrderIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevolutOrderIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetCadence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RevolutSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    NetAmountMinor = table.Column<int>(type: "int", nullable: false),
                    VatAmountMinor = table.Column<int>(type: "int", nullable: false),
                    GrossAmountMinor = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevolutOrderIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevolutOrderIntents_BillingAccounts_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "BillingAccounts",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevolutOrderIntents_OrderId",
                table: "RevolutOrderIntents",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevolutOrderIntents_RestaurantId_IdempotencyKey",
                table: "RevolutOrderIntents",
                columns: new[] { "RestaurantId", "IdempotencyKey" });

            migrationBuilder.CreateIndex(
                name: "IX_RevolutOrderIntents_RestaurantId_IsOpen",
                table: "RevolutOrderIntents",
                columns: new[] { "RestaurantId", "IsOpen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevolutOrderIntents");
        }
    }
}
