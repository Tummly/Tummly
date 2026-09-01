using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="LocationGuestPermissionMigrationMapping"/> and
    /// <see cref="LocationGuestMarketingPreferenceRollup"/> — legacy marketing
    /// preference migration and rollup derivation (ticket 01).
    /// </summary>
    public class LocationGuestPermissionMigrationAndRollupTests
    {
        [Theory]
        [InlineData(
            LocationGuestMarketingPreference.Allowed,
            LocationGuestPermissionState.Granted
        )]
        [InlineData(
            LocationGuestMarketingPreference.OptedOut,
            LocationGuestPermissionState.Withdrawn
        )]
        [InlineData(
            LocationGuestMarketingPreference.NotRecorded,
            LocationGuestPermissionState.NotRecorded
        )]
        public void PermissionStatesFromLegacyMarketingPreference_MapsAllThreeKinds(
            LocationGuestMarketingPreference legacy,
            LocationGuestPermissionState expected
        )
        {
            var states =
                LocationGuestPermissionMigrationMapping.PermissionStatesFromLegacyMarketingPreference(
                    legacy
                );

            Assert.Equal(3, states.Count);
            foreach (var kind in LocationGuestPermissionKindExtensions.All)
            {
                Assert.Equal(expected, states[kind]);
            }
        }

        [Theory]
        [InlineData(LocationGuestMarketingPreference.Allowed, 3)]
        [InlineData(LocationGuestMarketingPreference.OptedOut, 3)]
        [InlineData(LocationGuestMarketingPreference.NotRecorded, 0)]
        public void LedgerEventsFromLegacyMarketingPreference_MapsEventCounts(
            LocationGuestMarketingPreference legacy,
            int expectedCount
        )
        {
            var events =
                LocationGuestPermissionMigrationMapping.LedgerEventsFromLegacyMarketingPreference(
                    legacy
                );

            Assert.Equal(expectedCount, events.Count);

            if (legacy == LocationGuestMarketingPreference.Allowed)
            {
                Assert.All(
                    events,
                    e => Assert.Equal(
                        LocationGuestPermissionLedgerEventKinds.Grant,
                        e.EventKind
                    )
                );
            }
            else if (legacy == LocationGuestMarketingPreference.OptedOut)
            {
                Assert.All(
                    events,
                    e => Assert.Equal(
                        LocationGuestPermissionLedgerEventKinds.Withdraw,
                        e.EventKind
                    )
                );
            }
        }

        [Theory]
        [InlineData(
            LocationGuestPermissionState.Granted,
            LocationGuestPermissionState.Granted,
            LocationGuestMarketingPreference.Allowed
        )]
        [InlineData(
            LocationGuestPermissionState.Granted,
            LocationGuestPermissionState.Withdrawn,
            LocationGuestMarketingPreference.Allowed
        )]
        [InlineData(
            LocationGuestPermissionState.Withdrawn,
            LocationGuestPermissionState.Withdrawn,
            LocationGuestMarketingPreference.OptedOut
        )]
        [InlineData(
            LocationGuestPermissionState.NotRecorded,
            LocationGuestPermissionState.NotRecorded,
            LocationGuestMarketingPreference.NotRecorded
        )]
        [InlineData(
            LocationGuestPermissionState.Withdrawn,
            LocationGuestPermissionState.NotRecorded,
            LocationGuestMarketingPreference.OptedOut
        )]
        public void DeriveRollup_MatchesLegacyInverseForEmailAndSms(
            LocationGuestPermissionState email,
            LocationGuestPermissionState sms,
            LocationGuestMarketingPreference expected
        )
        {
            var rollup = LocationGuestMarketingPreferenceRollup.Derive(email, sms);

            Assert.Equal(expected, rollup);
        }

        [Theory]
        [InlineData(LocationGuestMarketingPreference.Allowed)]
        [InlineData(LocationGuestMarketingPreference.OptedOut)]
        [InlineData(LocationGuestMarketingPreference.NotRecorded)]
        public void DeriveRollup_RoundTripsLegacyMarketingPreference(
            LocationGuestMarketingPreference legacy
        )
        {
            var states =
                LocationGuestPermissionMigrationMapping.PermissionStatesFromLegacyMarketingPreference(
                    legacy
                );
            var rollup = LocationGuestMarketingPreferenceRollup.Derive(states);

            Assert.Equal(legacy, rollup);
        }
    }
}
