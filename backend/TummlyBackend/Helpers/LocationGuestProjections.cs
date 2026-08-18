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
        /// Marketing-eligible when preference is allowed and a reachable email
        /// or mobile is present. Keep in sync with
        /// <see cref="GuestsListQueryComposer.WhereMarketingEligible"/>.
        /// </summary>
        public static bool IsMarketingEligible(
            LocationGuestMarketingPreference preference,
            string? email,
            string? mobile
        )
        {
            return preference.IsAllowed()
                && (
                    !string.IsNullOrWhiteSpace(email)
                    || !string.IsNullOrWhiteSpace(mobile)
                );
        }

        public static string DeriveMarketingStatus(
            LocationGuestMarketingPreference preference,
            string? email,
            string? mobile
        )
        {
            if (!IsMarketingEligible(preference, email, mobile))
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
        /// When the current Location Guest marketing preference began, from
        /// per-Feedback Offers opt-out audit. Newest contiguous streak matching
        /// <paramref name="currentPreference"/>; returns the oldest CreatedAt
        /// in that streak (first consent, or re-opt-in after a prior opt-out).
        /// <see cref="LocationGuestMarketingPreference.NotRecorded"/> has no
        /// Feedback mapping and returns null.
        /// </summary>
        public static DateTime? ResolveOffersConsentDetailAt(
            LocationGuestMarketingPreference currentPreference,
            IEnumerable<LocationGuestOffersOptOutFact> feedbacks
        )
        {
            if (currentPreference == LocationGuestMarketingPreference.NotRecorded)
            {
                return null;
            }

            var currentOffersOptOut =
                currentPreference == LocationGuestMarketingPreference.OptedOut;
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
            LocationGuestMarketingPreference preference,
            string? email,
            string? mobile,
            DateTime? consentCapturedAt = null
        )
        {
            return
            [
                BuildContactEligibilityRow("email", email, preference, consentCapturedAt),
                BuildContactEligibilityRow("sms", mobile, preference, consentCapturedAt),
            ];
        }

        private static LocationGuestContactEligibilityRow BuildContactEligibilityRow(
            string channel,
            string? contact,
            LocationGuestMarketingPreference preference,
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

            if (preference == LocationGuestMarketingPreference.OptedOut)
            {
                return new LocationGuestContactEligibilityRow(
                    Channel: channel,
                    Status: "unsubscribed",
                    DetailKind: "unsubscribed",
                    DetailAt: consentCapturedAt
                );
            }

            if (preference == LocationGuestMarketingPreference.NotRecorded)
            {
                return new LocationGuestContactEligibilityRow(
                    Channel: channel,
                    Status: "not_recorded",
                    DetailKind: "not_recorded",
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
