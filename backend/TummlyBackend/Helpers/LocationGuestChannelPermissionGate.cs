using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Restaurant channel toggles + guest permission state gates (ticket 04).
    /// </summary>
    public static class LocationGuestChannelPermissionGate
    {
        public static LocationGuestPermissionKind? ChannelToPermissionKind(
            string channel
        )
        {
            return (channel ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "email" => LocationGuestPermissionKind.EmailMarketing,
                "sms" => LocationGuestPermissionKind.SmsMarketing,
                _ => null,
            };
        }

        public static bool IsRestaurantPermissionEnabled(
            Restaurant restaurant,
            LocationGuestPermissionKind kind
        ) =>
            kind switch
            {
                LocationGuestPermissionKind.EmailMarketing =>
                    restaurant.EmailMarketingPermissionEnabled,
                LocationGuestPermissionKind.SmsMarketing =>
                    restaurant.SmsMarketingPermissionEnabled,
                LocationGuestPermissionKind.FeedbackFollowUp =>
                    restaurant.FeedbackFollowUpPermissionEnabled,
                _ => false,
            };

        public static bool IsGuestPermissionGranted(
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > states,
            LocationGuestPermissionKind kind
        ) =>
            states.TryGetValue(kind, out var state)
            && state == LocationGuestPermissionState.Granted;

        public static bool CanSendOnChannel(
            Restaurant restaurant,
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > states,
            string channel
        )
        {
            var kind = ChannelToPermissionKind(channel);
            if (kind == null)
            {
                return false;
            }

            return IsRestaurantPermissionEnabled(restaurant, kind.Value)
                && IsGuestPermissionGranted(states, kind.Value);
        }

        /// <summary>
        /// Thank-you and recovery Offer issue paths — at least one Enabled
        /// marketing channel with guest grant (legacy Offers opt-in).
        /// </summary>
        public static bool IsMarketingOfferAllowed(
            Restaurant restaurant,
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > states
        )
        {
            foreach (
                var kind in new[]
                {
                    LocationGuestPermissionKind.EmailMarketing,
                    LocationGuestPermissionKind.SmsMarketing,
                }
            )
            {
                if (
                    IsRestaurantPermissionEnabled(restaurant, kind)
                    && IsGuestPermissionGranted(states, kind)
                )
                {
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<LocationGuestPermissionKind> EnabledRestaurantPermissions(
            Restaurant restaurant
        ) =>
            LocationGuestPermissionKindExtensions.All
                .Where(kind => IsRestaurantPermissionEnabled(restaurant, kind))
                .ToList();

        public static IReadOnlyList<(
            LocationGuestPermissionKind Kind,
            string EventKind
        )> LedgerEventsForGuestFormSubmit(
            Restaurant restaurant,
            bool marketingConsentGranted,
            ContactType contactType,
            LocationGuestPermissionState currentMarketingState =
                LocationGuestPermissionState.NotRecorded
        )
        {
            var events = new List<(LocationGuestPermissionKind Kind, string EventKind)>();

            // Feedback follow-up has no guest checkbox — providing contact grants
            // it when the restaurant permission is Enabled (GF handoff).
            if (
                IsRestaurantPermissionEnabled(
                    restaurant,
                    LocationGuestPermissionKind.FeedbackFollowUp
                )
            )
            {
                events.Add(
                    (
                        LocationGuestPermissionKind.FeedbackFollowUp,
                        LocationGuestPermissionLedgerEventKinds.Grant
                    )
                );
            }

            var marketingKind = contactType switch
            {
                ContactType.Email => LocationGuestPermissionKind.EmailMarketing,
                ContactType.Phone => LocationGuestPermissionKind.SmsMarketing,
                _ => (LocationGuestPermissionKind?)null,
            };

            if (marketingKind == null)
            {
                return events;
            }

            if (!IsRestaurantPermissionEnabled(restaurant, marketingKind.Value))
            {
                // Matching channel is off — no marketing checkbox was shown; do
                // not invent grant/withdraw for marketing.
                return events;
            }

            if (marketingConsentGranted)
            {
                events.Add(
                    (
                        marketingKind.Value,
                        LocationGuestPermissionLedgerEventKinds.Grant
                    )
                );
            }
            else if (
                currentMarketingState == LocationGuestPermissionState.Granted
            )
            {
                // Untick only withdraws a prior grant. First-time untick stays
                // Not recorded → Guest profile "Not granted" (not Withdrawn).
                events.Add(
                    (
                        marketingKind.Value,
                        LocationGuestPermissionLedgerEventKinds.Withdraw
                    )
                );
            }

            return events;
        }

        public static bool CanSendFeedbackFollowUp(
            Restaurant restaurant,
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > states
        ) =>
            IsRestaurantPermissionEnabled(
                restaurant,
                LocationGuestPermissionKind.FeedbackFollowUp
            )
            && IsGuestPermissionGranted(
                states,
                LocationGuestPermissionKind.FeedbackFollowUp
            );

        public static IReadOnlyDictionary<
            LocationGuestPermissionKind,
            LocationGuestPermissionState
        > ResolveEffectiveStates(
            LocationGuestMarketingPreference marketingPreference,
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > ledgerStates
        )
        {
            if (
                ledgerStates.Values.All(
                    state => state == LocationGuestPermissionState.NotRecorded
                )
                && marketingPreference
                    != LocationGuestMarketingPreference.NotRecorded
            )
            {
                return LocationGuestPermissionMigrationMapping
                    .PermissionStatesFromLegacyMarketingPreference(
                        marketingPreference
                    );
            }

            return ledgerStates;
        }

        public static IReadOnlyList<(
            LocationGuestPermissionKind Kind,
            string EventKind
        )> LedgerEventsForOperatorMarketingPreference(
            LocationGuestMarketingPreference preference
        ) =>
            LocationGuestPermissionMigrationMapping
                .LedgerEventsFromLegacyMarketingPreference(preference)
                .ToList();
    }
}
