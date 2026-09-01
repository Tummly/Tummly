using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Maps legacy <see cref="LocationGuestMarketingPreference"/> values to
    /// permission ledger backfill events (ticket 01 migration).
    /// </summary>
    public static class LocationGuestPermissionMigrationMapping
    {
        public static IReadOnlyList<
            (LocationGuestPermissionKind Kind, string EventKind)
        > LedgerEventsFromLegacyMarketingPreference(
            LocationGuestMarketingPreference marketingPreference
        ) =>
            marketingPreference switch
            {
                LocationGuestMarketingPreference.Allowed =>
                    LocationGuestPermissionKindExtensions.All
                        .Select(kind => (
                            kind,
                            LocationGuestPermissionLedgerEventKinds.Grant
                        ))
                        .ToList(),
                LocationGuestMarketingPreference.OptedOut =>
                    LocationGuestPermissionKindExtensions.All
                        .Select(kind => (
                            kind,
                            LocationGuestPermissionLedgerEventKinds.Withdraw
                        ))
                        .ToList(),
                LocationGuestMarketingPreference.NotRecorded =>
                    Array.Empty<(LocationGuestPermissionKind, string)>(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(marketingPreference),
                    marketingPreference,
                    "Unknown Location Guest marketing preference."
                ),
            };

        public static IReadOnlyDictionary<
            LocationGuestPermissionKind,
            LocationGuestPermissionState
        > PermissionStatesFromLegacyMarketingPreference(
            LocationGuestMarketingPreference marketingPreference
        )
        {
            var result =
                new Dictionary<LocationGuestPermissionKind, LocationGuestPermissionState>();

            foreach (var kind in LocationGuestPermissionKindExtensions.All)
            {
                result[kind] = marketingPreference switch
                {
                    LocationGuestMarketingPreference.Allowed =>
                        LocationGuestPermissionState.Granted,
                    LocationGuestMarketingPreference.OptedOut =>
                        LocationGuestPermissionState.Withdrawn,
                    LocationGuestMarketingPreference.NotRecorded =>
                        LocationGuestPermissionState.NotRecorded,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(marketingPreference),
                        marketingPreference,
                        "Unknown Location Guest marketing preference."
                    ),
                };
            }

            return result;
        }
    }
}
