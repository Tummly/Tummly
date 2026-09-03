using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTummlyVatInvoicePdfSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerBillingEmail",
                table: "TummlyVatInvoices",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliverToSnapshot",
                table: "TummlyVatInvoices",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineItemsJson",
                table: "TummlyVatInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodSummary",
                table: "TummlyVatInvoices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerBillingEmail",
                table: "TummlyVatInvoices",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerBillingEmail",
                table: "TummlyVatInvoices");

            migrationBuilder.DropColumn(
                name: "DeliverToSnapshot",
                table: "TummlyVatInvoices");

            migrationBuilder.DropColumn(
                name: "LineItemsJson",
                table: "TummlyVatInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentMethodSummary",
                table: "TummlyVatInvoices");

            migrationBuilder.DropColumn(
                name: "SellerBillingEmail",
                table: "TummlyVatInvoices");
        }
    }
}
