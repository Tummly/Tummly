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
            // In-flight only — unattached Drafts are not Needs attention.
            if (!string.Equals(effectiveStatus, Active, StringComparison.Ordinal))
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

        /// <summary>
        /// List-tab / overview membership: expiring rule OR ≥1 open Void request.
        /// In-flight only — effective Active with ≥1 live attach. Pass
        /// <paramref name="hasOpenVoidRequest"/> when Void rows are queryable;
        /// otherwise false keeps expiring-only membership honest.
        /// </summary>
        public static bool IsNeedsAttention(
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            string effectiveStatus,
            DateOnly venueLocalToday,
            bool hasOpenVoidRequest = false,
            int liveAttachCount = 0
        )
        {
            if (!string.Equals(effectiveStatus, Active, StringComparison.Ordinal))
            {
                return false;
            }

            if (liveAttachCount < 1)
            {
                return false;
            }

            return hasOpenVoidRequest
                || IsNeedsAttentionRule(
                    validity,
                    customExpiryDate,
                    effectiveStatus,
                    venueLocalToday
                );
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

        /// <summary>
        /// True when a fixed ChooseExpiryDate is strictly before venue-local today.
        /// Applies to Draft and Active stored rows (Draft skips expiry in
        /// <see cref="ResolveEffectiveStatus"/>).
        /// </summary>
        public static bool IsPastFixedExpiry(
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            DateOnly venueLocalToday
        )
            => validity == CatalogOfferValidity.ChooseExpiryDate
                && customExpiryDate is { } expiry
                && expiry < venueLocalToday;

        /// <summary>
        /// In-flight / issue path: stored Active and not past fixed expiry.
        /// </summary>
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

        /// <summary>
        /// First-or-next attach may bind Draft or Active (not paused / archived /
        /// past fixed expiry). Draft becomes Active after the first live attach.
        /// </summary>
        public static bool IsAttachable(
            string storedStatus,
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            DateOnly venueLocalToday
        )
        {
            if (IsPastFixedExpiry(validity, customExpiryDate, venueLocalToday))
            {
                return false;
            }

            if (IsStoredDraft(storedStatus))
            {
                return true;
            }

            return IsAttachableActive(
                storedStatus,
                validity,
                customExpiryDate,
                venueLocalToday
            );
        }

        /// <summary>
        /// Stored draft ↔ active from raw live attach count. Leaves paused /
        /// archived / expired-effective rows unchanged.
        /// </summary>
        public static string ResolveStoredStatusFromLiveAttachCount(
            string storedStatus,
            CatalogOfferValidity validity,
            DateOnly? customExpiryDate,
            DateOnly venueLocalToday,
            int rawLiveAttachCount
        )
        {
            var effective = ResolveEffectiveStatus(
                storedStatus,
                validity,
                customExpiryDate,
                venueLocalToday
            );

            if (IsClosed(effective))
            {
                return storedStatus;
            }

            if (!IsStoredDraft(storedStatus)
                && !string.Equals(storedStatus, Active, StringComparison.Ordinal))
            {
                return storedStatus;
            }

            return rawLiveAttachCount >= 1 ? Active : Draft;
        }

        /// <summary>
        /// Resume from Paused: Active when ≥1 live attach remains, else Draft.
        /// </summary>
        public static string ResolveResumeStoredStatus(int rawLiveAttachCount)
            => rawLiveAttachCount >= 1 ? Active : Draft;

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
