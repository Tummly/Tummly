using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTopupDrainSourcePaymentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectionSource",
                table: "CreditLedgerEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourcePaymentRef",
                table: "CreditLedgerEntries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_RestaurantId_SourcePaymentRef",
                table: "CreditLedgerEntries",
                columns: new[] { "RestaurantId", "SourcePaymentRef" },
                filter: "[SourcePaymentRef] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditLedgerEntries_RestaurantId_SourcePaymentRef",
                table: "CreditLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CorrectionSource",
                table: "CreditLedgerEntries");

            migrationBuilder.DropColumn(
                name: "SourcePaymentRef",
                table: "CreditLedgerEntries");
        }
    }
}
