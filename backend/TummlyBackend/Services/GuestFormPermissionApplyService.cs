using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
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
        private readonly ApplicationDbContext _context;

        public GuestFormPermissionApplyService(
            ILocationGuestPermissionLedgerService ledger,
            ApplicationDbContext context
        )
        {
            _ledger = ledger;
            _context = context;
        }

        public async Task ApplyOnSubmitAsync(
            LocationGuest locationGuest,
            Restaurant restaurant,
            int restaurantLocationId,
            string locationName,
            bool marketingConsentGranted,
            ContactType contactType,
            DateTime occurredAt,
            CancellationToken cancellationToken = default
        )
        {
            var marketingKind = contactType switch
            {
                ContactType.Email => LocationGuestPermissionKind.EmailMarketing,
                ContactType.Phone => LocationGuestPermissionKind.SmsMarketing,
                _ => (LocationGuestPermissionKind?)null,
            };

            var ledgerStates = await _ledger.GetCurrentStatesAsync(
                locationGuest,
                cancellationToken
            );

            var effectiveStates =
                LocationGuestChannelPermissionGate.ResolveEffectiveStates(
                    locationGuest.MarketingPreference,
                    ledgerStates
                );

            var currentMarketingState =
                marketingKind != null
                    ? effectiveStates[marketingKind.Value]
                    : LocationGuestPermissionState.NotRecorded;

            var currentFeedbackFollowUpState = effectiveStates[
                LocationGuestPermissionKind.FeedbackFollowUp
            ];

            var events = LocationGuestChannelPermissionGate.LedgerEventsForGuestFormSubmit(
                restaurant,
                marketingConsentGranted,
                contactType,
                currentMarketingState,
                currentFeedbackFollowUpState
            );

            foreach (var (kind, eventKind) in events)
            {
                // Navigation overload: new guests still have Id == 0 until
                // the caller's SaveChanges (SQL Server FK-safe).
                var evidence = BuildEvidence(
                    kind,
                    eventKind,
                    contactType,
                    restaurant,
                    locationName
                );
                _ledger.RecordEvent(
                    locationGuest,
                    restaurantLocationId,
                    kind,
                    eventKind,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    occurredAt,
                    evidence
                );

                if (
                    kind == LocationGuestPermissionKind.SmsMarketing
                    && eventKind == LocationGuestPermissionLedgerEventKinds.Grant
                )
                {
                    await ClearSmsStopAsync(locationGuest, cancellationToken);
                }
            }

            await _ledger.SyncMarketingPreferenceRollupAsync(
                locationGuest,
                cancellationToken
            );
        }

        /// <summary>
        /// A new SMS marketing grant on the Guest Form lifts the STOP block for this
        /// restaurant (R4.6). Email permissions are not touched.
        /// </summary>
        private async Task ClearSmsStopAsync(
            LocationGuest locationGuest,
            CancellationToken cancellationToken
        )
        {
            var master =
                locationGuest.MasterGuest
                ?? await _context.MasterGuests.FirstOrDefaultAsync(
                    row => row.Id == locationGuest.MasterGuestId,
                    cancellationToken
                );
            if (master != null)
            {
                master.SmsStoppedAtUtc = null;
            }
        }

        private static PermissionLedgerEvidence BuildEvidence(
            LocationGuestPermissionKind kind,
            string eventKind,
            ContactType contactType,
            Restaurant restaurant,
            string locationName
        )
        {
            if (kind == LocationGuestPermissionKind.FeedbackFollowUp)
            {
                return GuestFormPermissionEvidence.ForFeedbackFollowUpGrant(
                    restaurant.Name,
                    locationName
                );
            }

            var customWording = contactType switch
            {
                ContactType.Email => restaurant.EmailConsentWording,
                ContactType.Phone => restaurant.SmsConsentWording,
                _ => null,
            };

            if (eventKind == LocationGuestPermissionLedgerEventKinds.Withdraw)
            {
                return GuestFormPermissionEvidence.ForMarketingWithdraw(
                    contactType,
                    restaurant.Name,
                    customWording
                );
            }

            return GuestFormPermissionEvidence.ForMarketingGrant(
                contactType,
                restaurant.Name,
                customWording
            );
        }
    }
}
