using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPaymentRefundAndTcnRelatedOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelatedRevolutOrderId",
                table: "TummlyVatInvoices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminPaymentRefundIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    SourcePaymentOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RefundOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AmountMinor = table.Column<int>(type: "int", nullable: true),
                    ActorStaffUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPaymentRefundIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminPaymentRefundIntents_BillingAccounts_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "BillingAccounts",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminPaymentRefundIntents_IdempotencyKey",
                table: "AdminPaymentRefundIntents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminPaymentRefundIntents_RefundOrderId",
                table: "AdminPaymentRefundIntents",
                column: "RefundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminPaymentRefundIntents_RestaurantId",
                table: "AdminPaymentRefundIntents",
                column: "RestaurantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminPaymentRefundIntents");

            migrationBuilder.DropColumn(
                name: "RelatedRevolutOrderId",
                table: "TummlyVatInvoices");
        }
    }
}
