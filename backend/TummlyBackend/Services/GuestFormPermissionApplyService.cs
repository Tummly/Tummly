using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Applies guest-form permission ledger events on Feedback submit (ticket 04).
    /// Caller owns SaveChanges on the shared DbContext.
    /// </summary>
    public sealed class GuestFormPermissionApplyService
        : IGuestFormPermissionApplyService
    {
        private readonly ILocationGuestPermissionLedgerService _ledger;

        public GuestFormPermissionApplyService(
            ILocationGuestPermissionLedgerService ledger
        )
        {
            _ledger = ledger;
        }

        public async Task ApplyOnSubmitAsync(
            LocationGuest locationGuest,
            Restaurant restaurant,
            int restaurantLocationId,
            bool consentGranted,
            DateTime occurredAt,
            CancellationToken cancellationToken = default
        )
        {
            var events = LocationGuestChannelPermissionGate.LedgerEventsForGuestFormSubmit(
                restaurant,
                consentGranted
            );

            foreach (var (kind, eventKind) in events)
            {
                _ledger.RecordEvent(
                    locationGuest.Id,
                    restaurantLocationId,
                    kind,
                    eventKind,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    occurredAt
                );
            }

            await _ledger.SyncMarketingPreferenceRollupAsync(
                locationGuest,
                cancellationToken
            );
        }
    }
}
