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
        /// permission ledger current state.
        /// </summary>
        Task<LocationGuestMarketingPreference> SyncMarketingPreferenceRollupAsync(
            int locationGuestId,
            CancellationToken cancellationToken = default
        );
    }
}
