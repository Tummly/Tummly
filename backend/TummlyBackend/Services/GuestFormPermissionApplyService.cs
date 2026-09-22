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

            var currentMarketingState = LocationGuestPermissionState.NotRecorded;
            if (marketingKind != null)
            {
                var ledgerStates =
                    locationGuest.Id == 0
                        ? LocationGuestPermissionKindExtensions.All.ToDictionary(
                            kind => kind,
                            _ => LocationGuestPermissionState.NotRecorded
                        )
                        : await _ledger.GetCurrentStatesAsync(
                            locationGuest.Id,
                            cancellationToken
                        );
                var effectiveStates =
                    LocationGuestChannelPermissionGate.ResolveEffectiveStates(
                        locationGuest.MarketingPreference,
                        ledgerStates
                    );
                currentMarketingState = effectiveStates[marketingKind.Value];
            }

            var events = LocationGuestChannelPermissionGate.LedgerEventsForGuestFormSubmit(
                restaurant,
                marketingConsentGranted,
                contactType,
                currentMarketingState
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
