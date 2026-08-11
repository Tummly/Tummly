using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Stored vs effective catalog offer status + tab predicates (ticket 22).
    /// </summary>
    public static class CatalogOfferStatus
    {
        public const string Draft = "draft";
        public const string Active = "active";
        public const string Paused = "paused";
        public const string Archived = "archived";

        /// <summary>Wire / badge only — never stored.</summary>
        public const string Expired = "expired";

        public const string AttachKindCampaign = "campaign";
        public const string AttachSourceCampaign = "campaign";
        public const string AttachSourceRecovery = "recovery";
        public const string AttachSourceGuestFormThankYou = "guest-form-thank-you";
        public const string AttachSourceManual = "manual";

        public static DateOnly VenueLocalToday(
            DateTime utcNow,
            int utcOffsetMinutes
        ) => DateOnly.FromDateTime(utcNow.AddMinutes(utcOffsetMinutes));

        public static string ResolveEffectiveStatus(
            string storedStatus,
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            DateOnly venueLocalToday
        )
        {
            if (string.Equals(storedStatus, Archived, StringComparison.Ordinal))
            {
                return Archived;
            }

            if (string.Equals(storedStatus, Paused, StringComparison.Ordinal))
            {
                return Paused;
            }

            if (string.Equals(storedStatus, Draft, StringComparison.Ordinal))
            {
                return Draft;
            }

            if (
                validity == CatalogOfferValidity.ChooseExpiryDate
                && customExpiryDate is { } expiry
                && expiry < venueLocalToday
            )
            {
                return Expired;
            }

            return Active;
        }

        public static bool IsClosed(string effectiveStatus)
            => string.Equals(effectiveStatus, Paused, StringComparison.Ordinal)
                || string.Equals(effectiveStatus, Archived, StringComparison.Ordinal)
                || string.Equals(effectiveStatus, Expired, StringComparison.Ordinal);

        public static bool IsNeedsAttentionRule(
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            string effectiveStatus,
            DateOnly venueLocalToday
        )
        {
            if (IsClosed(effectiveStatus))
            {
                return false;
            }

            if (validity != CatalogOfferValidity.ChooseExpiryDate
                || customExpiryDate is not { } expiry)
            {
                return false;
            }

            var windowEnd = venueLocalToday.AddDays(7);
            return expiry >= venueLocalToday && expiry <= windowEnd;
        }

        public static bool IsStoredDraft(string storedStatus)
            => string.Equals(storedStatus, Draft, StringComparison.Ordinal);

        public static bool MatchesView(
            string view,
            string storedStatus,
            string effectiveStatus,
            int liveAttachCount
        )
        {
            return view switch
            {
                "drafts" => IsStoredDraft(storedStatus)
                    || (!IsClosed(effectiveStatus) && liveAttachCount == 0),
                "in-flight" => !IsStoredDraft(storedStatus)
                    && !IsClosed(effectiveStatus)
                    && liveAttachCount >= 1,
                "sent" => IsClosed(effectiveStatus),
                "needs-attention" => false, // caller uses IsNeedsAttentionRule
                _ => true,
            };
        }

        public static bool IsAttachableActive(
            string storedStatus,
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            DateOnly venueLocalToday
        )
        {
            if (!string.Equals(storedStatus, Active, StringComparison.Ordinal))
            {
                return false;
            }

            var effective = ResolveEffectiveStatus(
                storedStatus,
                validity,
                customExpiryDate,
                venueLocalToday
            );
            return string.Equals(effective, Active, StringComparison.Ordinal);
        }

        public static string BuildDuplicateTitle(
            string originalTitle,
            int maxTitleLength
        )
        {
            const string suffix = " (copy)";
            var title = (originalTitle ?? string.Empty) + suffix;
            if (title.Length <= maxTitleLength)
            {
                return title;
            }

            return title[..maxTitleLength];
        }
    }
}
