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
            // Filtered unique index is SQL-only: EF cannot map two indexes on
            // the same (RestaurantId, Channel) columns alongside the general
            // lookup index already in the model.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE NONCLUSTERED INDEX [IX_CreditLedgerEntries_PilotAllocation_RestaurantId_Channel]
                ON [CreditLedgerEntries] ([RestaurantId], [Channel])
                WHERE [EntryType] = N'pilot_allocation';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX [IX_CreditLedgerEntries_PilotAllocation_RestaurantId_Channel]
                ON [CreditLedgerEntries];
                """
            );
        }
    }
}
