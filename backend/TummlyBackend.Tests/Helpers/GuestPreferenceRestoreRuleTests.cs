using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Pure D5 restore rule: restore only after a guest-made withdraw of a kind
    /// the guest granted at that location.
    /// </summary>
    public class GuestPreferenceRestoreRuleTests
    {
        private static readonly DateTime Start = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CanRestore_True_AfterEmailUnsubscribe()
        {
            var history = History(
                Row(1, LocationGuestPermissionLedgerEventKinds.Grant, LocationGuestPermissionLedgerSources.GuestForm, 0),
                Row(2, LocationGuestPermissionLedgerEventKinds.Withdraw, LocationGuestPermissionLedgerSources.EmailUnsubscribe, 1)
            );

            Assert.True(GuestPreferenceRestoreRule.CanRestore(history));
        }

        [Fact]
        public void CanRestore_True_AfterPreferenceCentre()
        {
            var history = History(
                Row(1, LocationGuestPermissionLedgerEventKinds.Grant, LocationGuestPermissionLedgerSources.GuestForm, 0),
                Row(2, LocationGuestPermissionLedgerEventKinds.Withdraw, LocationGuestPermissionLedgerSources.PreferenceCentre, 1)
            );

            Assert.True(GuestPreferenceRestoreRule.CanRestore(history));
        }

        [Theory]
        [InlineData(GuestContactSuppressionSources.Bounce)]
        [InlineData(GuestContactSuppressionSources.Complaint)]
        [InlineData(LocationGuestPermissionLedgerSources.Operator)]
        [InlineData(LocationGuestPermissionLedgerSources.SmsStop)]
        [InlineData(LocationGuestPermissionLedgerSources.Suppression)]
        [InlineData(LocationGuestPermissionLedgerSources.LegacyMarketingPreference)]
        public void CanRestore_False_After(string source)
        {
            var history = History(
                Row(1, LocationGuestPermissionLedgerEventKinds.Grant, LocationGuestPermissionLedgerSources.GuestForm, 0),
                Row(2, LocationGuestPermissionLedgerEventKinds.Withdraw, source, 1)
            );

            Assert.False(GuestPreferenceRestoreRule.CanRestore(history));
        }

        [Fact]
        public void CanRestore_False_WhenNeverGranted()
        {
            var history = History(
                Row(1, LocationGuestPermissionLedgerEventKinds.Withdraw, LocationGuestPermissionLedgerSources.EmailUnsubscribe, 0)
            );

            Assert.False(GuestPreferenceRestoreRule.CanRestore(history));
        }

        [Fact]
        public void CanRestore_False_WhenLatestIsGrant()
        {
            var history = History(
                Row(1, LocationGuestPermissionLedgerEventKinds.Grant, LocationGuestPermissionLedgerSources.GuestForm, 0),
                Row(2, LocationGuestPermissionLedgerEventKinds.Withdraw, LocationGuestPermissionLedgerSources.EmailUnsubscribe, 1),
                Row(3, LocationGuestPermissionLedgerEventKinds.Grant, LocationGuestPermissionLedgerSources.Operator, 2)
            );

            Assert.False(GuestPreferenceRestoreRule.CanRestore(history));
        }

        [Fact]
        public void CanRestore_UsesLatestByOccurredAt_NotListOrder()
        {
            // Rows given newest first. The bounce is the latest, so no restore.
            var history = History(
                Row(3, LocationGuestPermissionLedgerEventKinds.Withdraw, GuestContactSuppressionSources.Bounce, 2),
                Row(1, LocationGuestPermissionLedgerEventKinds.Grant, LocationGuestPermissionLedgerSources.GuestForm, 0),
                Row(2, LocationGuestPermissionLedgerEventKinds.Withdraw, LocationGuestPermissionLedgerSources.EmailUnsubscribe, 1)
            );

            Assert.False(GuestPreferenceRestoreRule.CanRestore(history));
        }

        [Fact]
        public void CanRestore_False_WhenHistoryEmpty()
        {
            Assert.False(GuestPreferenceRestoreRule.CanRestore(Array.Empty<LocationGuestPermissionLedgerEntry>()));
        }

        private static IReadOnlyList<LocationGuestPermissionLedgerEntry> History(
            params LocationGuestPermissionLedgerEntry[] rows
        ) => rows;

        private static LocationGuestPermissionLedgerEntry Row(
            int id,
            string eventKind,
            string source,
            int minutesAfterStart
        ) =>
            new()
            {
                Id = id,
                EventKind = eventKind,
                Source = source,
                OccurredAt = Start.AddMinutes(minutesAfterStart),
            };
    }
}
