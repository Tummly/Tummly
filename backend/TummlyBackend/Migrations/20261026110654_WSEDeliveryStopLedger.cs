using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class WSEDeliveryStopLedger : Migration
    {
        /// <summary>
        /// Stops the migration when a TopupAllocation group repeats for one restaurant
        /// and payment ref. Runs before the unique ledger index (WS-E R8.1).
        /// </summary>
        public const string DuplicateTopupPreCheckSql = @"
IF EXISTS (
    SELECT 1
    FROM CreditLedgerEntries
    WHERE [SourcePaymentRef] IS NOT NULL
      AND [EntryType] = N'topup_allocation'
    GROUP BY [RestaurantId], [SourcePaymentRef], [EntryType]
    HAVING COUNT(*) > 1
)
    THROW 51000, 'WSE: duplicate TopupAllocation rows exist; resolve before migrating.', 1;";

        /// <summary>
        /// Backfills reply delivery state for rows that existed before this migration
        /// (WS-E R1.7). SMS rows and email rows the worker never sent become Unavailable.
        /// Email rows still queued (Pending) stay Pending; Accepted stays Accepted.
        /// </summary>
        public const string BackfillSql = @"
UPDATE FeedbackGuestResponses
SET DeliveryState = CASE
        WHEN Channel = 1 THEN 4
        WHEN EmailDeliveryStatus = 2 THEN 1
        WHEN EmailDeliveryStatus = 1 THEN 0
        ELSE 4
    END,
    DeliveryStateAtUtc = COALESCE(EmailDeliveredAt, CreatedAt);";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SmsStoppedAtUtc",
                table: "MasterGuests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryState",
                table: "FeedbackGuestResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryStateAtUtc",
                table: "FeedbackGuestResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "FeedbackGuestResponses",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(BackfillSql);

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackGuestResponses_ProviderMessageId",
                table: "FeedbackGuestResponses",
                column: "ProviderMessageId",
                filter: "[ProviderMessageId] IS NOT NULL");

            migrationBuilder.Sql(DuplicateTopupPreCheckSql);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_RestaurantId_SourcePaymentRef_EntryType",
                table: "CreditLedgerEntries",
                columns: new[] { "RestaurantId", "SourcePaymentRef", "EntryType" },
                unique: true,
                filter: "[SourcePaymentRef] IS NOT NULL AND [EntryType] = N'topup_allocation'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeedbackGuestResponses_ProviderMessageId",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropIndex(
                name: "IX_CreditLedgerEntries_RestaurantId_SourcePaymentRef_EntryType",
                table: "CreditLedgerEntries");

            migrationBuilder.DropColumn(
                name: "SmsStoppedAtUtc",
                table: "MasterGuests");

            migrationBuilder.DropColumn(
                name: "DeliveryState",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "DeliveryStateAtUtc",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "FeedbackGuestResponses");
        }
    }
}
