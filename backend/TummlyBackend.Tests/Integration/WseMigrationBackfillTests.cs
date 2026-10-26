using TummlyBackend.Migrations;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public sealed class WseMigrationBackfillTests
    {
        [Fact]
        public void BackfillSql_UsesEnumValueLiterals()
        {
            var sql = WSEDeliveryStopLedger.BackfillSql;

            Assert.Contains(
                $"WHEN Channel = {(int)FeedbackGuestResponseChannel.Sms} THEN {(int)GuestReplyDeliveryState.Unavailable}",
                sql
            );
            Assert.Contains(
                $"WHEN EmailDeliveryStatus = {(int)GuestResponseEmailDeliveryStatus.Accepted} THEN {(int)GuestReplyDeliveryState.Accepted}",
                sql
            );
            Assert.Contains(
                $"WHEN EmailDeliveryStatus = {(int)GuestResponseEmailDeliveryStatus.Pending} THEN {(int)GuestReplyDeliveryState.Pending}",
                sql
            );
            Assert.Contains($"ELSE {(int)GuestReplyDeliveryState.Unavailable}", sql);
        }

        [Fact]
        public void DuplicateTopupPreCheck_ThrowsWseErrorForTopupAllocationGroups()
        {
            var sql = WSEDeliveryStopLedger.DuplicateTopupPreCheckSql;

            Assert.Contains(
                "THROW 51000, 'WSE: duplicate TopupAllocation rows exist; resolve before migrating.', 1",
                sql
            );
            Assert.Contains(
                $"[EntryType] = N'{CreditLedgerEntryTypes.TopupAllocation}'",
                sql
            );
        }
    }
}
