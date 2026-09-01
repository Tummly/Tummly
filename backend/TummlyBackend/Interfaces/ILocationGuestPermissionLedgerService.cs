using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Location Guest permission ledger — append-only grant/withdraw events and
    /// derived current permission state. Caller owns SaveChanges (same unit of
    /// work as the domain write).
    /// </summary>
    public interface ILocationGuestPermissionLedgerService
    {
        void RecordEvent(
            int locationGuestId,
            int restaurantLocationId,
            LocationGuestPermissionKind permissionKind,
            string eventKind,
            string source,
            DateTime occurredAt,
            int? actorUserId = null
        );

        Task<
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            >
        > GetCurrentStatesAsync(
            int locationGuestId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Recomputes and writes the legacy
        /// <see cref="LocationGuest.MarketingPreference"/> rollup from the
        /// permission ledger current state (persisted rows plus pending
        /// Added/Modified entries on this context). Caller must invoke this
        /// in the same unit of work as <see cref="RecordEvent"/>.
        /// </summary>
        Task<LocationGuestMarketingPreference> SyncMarketingPreferenceRollupAsync(
            LocationGuest locationGuest,
            CancellationToken cancellationToken = default
        );

        /// <inheritdoc cref="SyncMarketingPreferenceRollupAsync(LocationGuest, CancellationToken)"/>
        Task<LocationGuestMarketingPreference> SyncMarketingPreferenceRollupAsync(
            int locationGuestId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Current permission state per guest id (latest ledger event per kind).
        /// Guests with no ledger rows get all kinds
        /// <see cref="LocationGuestPermissionState.NotRecorded"/>.
        /// </summary>
        Task<
            IReadOnlyDictionary<
                int,
                IReadOnlyDictionary<
                    LocationGuestPermissionKind,
                    LocationGuestPermissionState
                >
            >
        > GetCurrentStatesBatchAsync(
            IReadOnlyList<int> locationGuestIds,
            CancellationToken cancellationToken = default
        );
    }
}
