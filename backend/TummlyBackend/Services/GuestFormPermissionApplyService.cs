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
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _ledger;

        public GuestFormPermissionApplyService(
            ApplicationDbContext context,
            ILocationGuestPermissionLedgerService ledger
        )
        {
            _context = context;
            _ledger = ledger;
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

            var ledgerStates =
                locationGuest.Id == 0
                    ? CurrentStatesFromLocal(locationGuest)
                    : await _ledger.GetCurrentStatesAsync(
                        locationGuest.Id,
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
            }

            await _ledger.SyncMarketingPreferenceRollupAsync(
                locationGuest,
                cancellationToken
            );
        }

        /// <summary>
        /// Mirrors <c>GetCurrentStatesCoreAsync</c> ordering for pending Local
        /// ledger rows on an unsaved Location Guest (Id == 0).
        /// </summary>
        private IReadOnlyDictionary<
            LocationGuestPermissionKind,
            LocationGuestPermissionState
        > CurrentStatesFromLocal(LocationGuest locationGuest)
        {
            var latestEvents = _context.LocationGuestPermissionLedgerEntries
                .Local
                .Where(e => ReferenceEquals(e.LocationGuest, locationGuest))
                .GroupBy(e => e.PermissionKind)
                .Select(g =>
                    g.OrderByDescending(e => e.OccurredAt)
                        .ThenByDescending(e => e.Id)
                        .First()
                );

            var states = LocationGuestPermissionKindExtensions.All.ToDictionary(
                kind => kind,
                _ => LocationGuestPermissionState.NotRecorded
            );

            foreach (var entry in latestEvents)
            {
                states[entry.PermissionKind] =
                    entry.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Grant
                        ? LocationGuestPermissionState.Granted
                        : LocationGuestPermissionState.Withdrawn;
            }

            return states;
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
