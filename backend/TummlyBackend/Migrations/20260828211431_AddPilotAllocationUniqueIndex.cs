using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotAllocationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_PilotAllocation_RestaurantId_Channel",
                table: "CreditLedgerEntries",
                columns: new[] { "RestaurantId", "Channel" },
                unique: true,
                filter: "[EntryType] = N'pilot_allocation'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditLedgerEntries_PilotAllocation_RestaurantId_Channel",
                table: "CreditLedgerEntries");
        }
    }
}
