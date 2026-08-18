namespace TummlyBackend.Models
{
    /// <summary>
    /// Durable Location Guest marketing preference for one Owned location.
    /// Wire values: allowed | opted_out | not_recorded.
    /// </summary>
    public enum LocationGuestMarketingPreference
    {
        Allowed,
        OptedOut,
        NotRecorded,
    }

    public static class LocationGuestMarketingPreferenceExtensions
    {
        public const string AllowedWire = "allowed";
        public const string OptedOutWire = "opted_out";
        public const string NotRecordedWire = "not_recorded";

        public static string ToWireString(
            this LocationGuestMarketingPreference preference
        ) =>
            preference switch
            {
                LocationGuestMarketingPreference.Allowed => AllowedWire,
                LocationGuestMarketingPreference.OptedOut => OptedOutWire,
                LocationGuestMarketingPreference.NotRecorded => NotRecordedWire,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(preference),
                    preference,
                    "Unknown Location Guest marketing preference."
                ),
            };

        public static LocationGuestMarketingPreference FromWireString(
            string stored
        )
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                throw new ArgumentException(
                    "Location Guest marketing preference is required.",
                    nameof(stored)
                );
            }

            return stored.Trim() switch
            {
                AllowedWire => LocationGuestMarketingPreference.Allowed,
                OptedOutWire => LocationGuestMarketingPreference.OptedOut,
                NotRecordedWire => LocationGuestMarketingPreference.NotRecorded,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(stored),
                    stored,
                    "Unknown Location Guest marketing preference."
                ),
            };
        }

        /// <summary>
        /// Maps per-Feedback Offers opt-out to the durable preference.
        /// Never returns <see cref="LocationGuestMarketingPreference.NotRecorded"/>.
        /// </summary>
        public static LocationGuestMarketingPreference FromFeedbackOffersOptOut(
            bool offersOptOut
        ) =>
            offersOptOut
                ? LocationGuestMarketingPreference.OptedOut
                : LocationGuestMarketingPreference.Allowed;

        public static bool IsAllowed(
            this LocationGuestMarketingPreference preference
        ) => preference == LocationGuestMarketingPreference.Allowed;
    }
}
