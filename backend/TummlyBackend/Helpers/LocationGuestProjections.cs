using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public sealed record LocationGuestFeedbackFact(
        DateTime CreatedAt,
        ClassificationStatus ClassificationStatus,
        FeedbackSentiment? Sentiment
    );

    public sealed record LocationGuestScopedFeedbackFact(
        int LocationGuestId,
        DateTime CreatedAt,
        ClassificationStatus ClassificationStatus,
        FeedbackSentiment? Sentiment
    )
    {
        public LocationGuestFeedbackFact ToFeedbackFact() =>
            new(CreatedAt, ClassificationStatus, Sentiment);
    }

    public sealed record LocationGuestFeedbackStats(
        int FeedbackSubmissionCount,
        string LatestFeedbackSentiment,
        DateTime? LastInteractionAt
    );

    public sealed record LocationGuestContactEligibilityRow(
        string Channel,
        string Status,
        string? DetailKind,
        DateTime? DetailAt
    );

    public sealed record LocationGuestOffersOptOutFact(
        DateTime CreatedAt,
        bool OffersOptOut
    );

    public static class LocationGuestProjections
    {
        /// <summary>
        /// Marketing-eligible when not opted out and a reachable email or mobile
        /// is present. Keep in sync with
        /// <see cref="GuestsListQueryComposer.WhereMarketingEligible"/>.
        /// </summary>
        public static bool IsMarketingEligible(
            bool offersOptOut,
            string? email,
            string? mobile
        )
        {
            return !offersOptOut
                && (
                    !string.IsNullOrWhiteSpace(email)
                    || !string.IsNullOrWhiteSpace(mobile)
                );
        }

        public static string DeriveMarketingStatus(
            bool offersOptOut,
            string? email,
            string? mobile
        )
        {
            if (!IsMarketingEligible(offersOptOut, email, mobile))
            {
                return "Not eligible";
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return "Eligible — Email";
            }

            return "Eligible — SMS";
        }

        public static LocationGuestFeedbackStats BuildFeedbackStats(
            IEnumerable<LocationGuestFeedbackFact> feedbacks
        )
        {
            var ordered = feedbacks
                .OrderByDescending(feedback => feedback.CreatedAt)
                .ToList();

            var latestSucceeded = ordered
                .FirstOrDefault(feedback =>
                    feedback.ClassificationStatus == ClassificationStatus.Succeeded
                );

            var latestFeedbackSentiment =
                latestSucceeded == null
                    ? "none"
                    : FeedbackClassificationMapping.ToWireSentiment(
                        latestSucceeded.Sentiment
                    ) ?? "none";

            return new LocationGuestFeedbackStats(
                FeedbackSubmissionCount: ordered.Count,
                LatestFeedbackSentiment: latestFeedbackSentiment,
                LastInteractionAt: ordered.FirstOrDefault()?.CreatedAt
            );
        }

        /// <summary>
        /// When the current Location Guest offers opt-out state began, from
        /// per-Feedback Offers opt-out audit. Newest contiguous streak matching
        /// <paramref name="currentOffersOptOut"/>; returns the oldest CreatedAt
        /// in that streak (first consent, or re-opt-in after a prior opt-out).
        /// </summary>
        public static DateTime? ResolveOffersConsentDetailAt(
            bool currentOffersOptOut,
            IEnumerable<LocationGuestOffersOptOutFact> feedbacks
        )
        {
            DateTime? streakStart = null;

            foreach (
                var feedback in feedbacks.OrderByDescending(fact => fact.CreatedAt)
            )
            {
                if (feedback.OffersOptOut != currentOffersOptOut)
                {
                    break;
                }

                streakStart = feedback.CreatedAt;
            }

            return streakStart;
        }

        public static IReadOnlyList<LocationGuestContactEligibilityRow> BuildContactEligibility(
            bool offersOptOut,
            string? email,
            string? mobile,
            DateTime? consentCapturedAt = null
        )
        {
            return
            [
                BuildContactEligibilityRow("email", email, offersOptOut, consentCapturedAt),
                BuildContactEligibilityRow("sms", mobile, offersOptOut, consentCapturedAt),
            ];
        }

        private static LocationGuestContactEligibilityRow BuildContactEligibilityRow(
            string channel,
            string? contact,
            bool offersOptOut,
            DateTime? consentCapturedAt
        )
        {
            if (string.IsNullOrWhiteSpace(contact))
            {
                return new LocationGuestContactEligibilityRow(
                    Channel: channel,
                    Status: "not_provided",
                    DetailKind: null,
                    DetailAt: null
                );
            }

            if (offersOptOut)
            {
                return new LocationGuestContactEligibilityRow(
                    Channel: channel,
                    Status: "unsubscribed",
                    DetailKind: "unsubscribed",
                    DetailAt: consentCapturedAt
                );
            }

            return new LocationGuestContactEligibilityRow(
                Channel: channel,
                Status: "eligible",
                DetailKind: "consent_captured",
                DetailAt: consentCapturedAt
            );
        }
    }
}
