using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTummlyVatInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TummlyDocumentSequences",
                columns: table => new
                {
                    DocumentPrefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TummlyDocumentSequences", x => new { x.DocumentPrefix, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "TummlyVatInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DocumentPrefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    RevolutOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RevolutSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaxPointUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LineDescription = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    NetPence = table.Column<int>(type: "int", nullable: false),
                    VatRateBps = table.Column<int>(type: "int", nullable: false),
                    VatPence = table.Column<int>(type: "int", nullable: false),
                    GrossPence = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CustomerBusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SellerLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellerRegisteredAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SellerVatRegistrationNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TummlyVatInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TummlyVatInvoices_BillingAccounts_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "BillingAccounts",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TummlyVatInvoices_DocumentNumber",
                table: "TummlyVatInvoices",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TummlyVatInvoices_RestaurantId",
                table: "TummlyVatInvoices",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_TummlyVatInvoices_RevolutOrderId",
                table: "TummlyVatInvoices",
                column: "RevolutOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TummlyDocumentSequences");

            migrationBuilder.DropTable(
                name: "TummlyVatInvoices");
        }
    }
}
