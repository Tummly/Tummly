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
            bool consentGranted
        )
        {
            if (consentGranted)
            {
                return EnabledRestaurantPermissions(restaurant)
                    .Select(kind => (
                        kind,
                        LocationGuestPermissionLedgerEventKinds.Grant
                    ))
                    .ToList();
            }

            return LocationGuestPermissionKindExtensions.All
                .Where(kind =>
                    kind == LocationGuestPermissionKind.EmailMarketing
                    || kind == LocationGuestPermissionKind.SmsMarketing
                )
                .Select(kind => (
                    kind,
                    LocationGuestPermissionLedgerEventKinds.Withdraw
                ))
                .ToList();
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
