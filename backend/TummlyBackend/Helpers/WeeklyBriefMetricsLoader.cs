using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Fresh Weekly brief aggregate metrics for a coverage window (no brief-row lookup).
    /// </summary>
    public static class WeeklyBriefMetricsLoader
    {
        public static async Task<WeeklyBriefMetrics> LoadAsync(
            ApplicationDbContext context,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var guestsJoined = await context.LocationGuests
                .AsNoTracking()
                .CountAsync(
                    guest =>
                        guest.RestaurantLocationId == locationId
                        && guest.CreatedAt >= fromUtc
                        && guest.CreatedAt < toUtc,
                    cancellationToken
                );

            var qrScanEvents = await context.QrScanEvents
                .AsNoTracking()
                .CountAsync(
                    scan =>
                        scan.RestaurantLocationId == locationId
                        && scan.CreatedAt >= fromUtc
                        && scan.CreatedAt < toUtc,
                    cancellationToken
                );

            var feedbackInWindow = context.Feedbacks
                .AsNoTracking()
                .Where(feedback =>
                    feedback.RestaurantLocationId == locationId
                    && feedback.CreatedAt >= fromUtc
                    && feedback.CreatedAt < toUtc
                );

            var feedbackCount = await feedbackInWindow.CountAsync(cancellationToken);

            var positiveFeedbackCount = await feedbackInWindow.CountAsync(
                feedback => feedback.Sentiment == FeedbackSentiment.Positive,
                cancellationToken
            );
            var neutralFeedbackCount = await feedbackInWindow.CountAsync(
                feedback => feedback.Sentiment == FeedbackSentiment.Neutral,
                cancellationToken
            );
            var negativeFeedbackCount = await feedbackInWindow.CountAsync(
                feedback => feedback.Sentiment == FeedbackSentiment.Negative,
                cancellationToken
            );

            var needsAttentionCount = await feedbackInWindow.CountAsync(
                feedback =>
                    feedback.ClassificationStatus == ClassificationStatus.Succeeded
                    && feedback.Sentiment == FeedbackSentiment.Negative
                    && feedback.WorkflowStatus != FeedbackWorkflowStatus.Resolved,
                cancellationToken
            );

            var tagJsonRows = await feedbackInWindow
                .Where(feedback =>
                    feedback.ClassificationStatus == ClassificationStatus.Succeeded
                    && feedback.DetectedTagsJson != null
                )
                .Select(feedback => feedback.DetectedTagsJson)
                .ToListAsync(cancellationToken);

            var detectedTagCounts = RollUpDetectedTagCounts(tagJsonRows);

            var activeOffers = await context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    offer =>
                        offer.RestaurantLocationId == locationId
                        && offer.Status == CatalogOfferStatus.Active,
                    cancellationToken
                );

            var offerIssuesInLocation = context.OfferIssues
                .AsNoTracking()
                .Where(issue =>
                    issue.CatalogOffer != null
                    && issue.CatalogOffer.RestaurantLocationId == locationId
                );

            var claimsInWeek = await offerIssuesInLocation.CountAsync(
                issue =>
                    issue.ClaimedAtUtc != null
                    && issue.ClaimedAtUtc >= fromUtc
                    && issue.ClaimedAtUtc < toUtc,
                cancellationToken
            );

            var redemptionsInWeek = await offerIssuesInLocation.CountAsync(
                issue =>
                    issue.RedeemedAtUtc != null
                    && issue.RedemptionVoidedAtUtc == null
                    && issue.RedeemedAtUtc >= fromUtc
                    && issue.RedeemedAtUtc < toUtc,
                cancellationToken
            );

            var campaignsSentInWeek = await context.Campaigns
                .AsNoTracking()
                .CountAsync(
                    campaign =>
                        campaign.RestaurantLocationId == locationId
                        && campaign.Status == CampaignLifecycleService.SentStatus
                        && campaign.UpdatedAt >= fromUtc
                        && campaign.UpdatedAt < toUtc,
                    cancellationToken
                );

            var campaignRecipientsReached = await context.CampaignFrozenRecipients
                .AsNoTracking()
                .CountAsync(
                    recipient =>
                        recipient.AcceptedAtUtc != null
                        && recipient.AcceptedAtUtc >= fromUtc
                        && recipient.AcceptedAtUtc < toUtc
                        && recipient.Campaign != null
                        && recipient.Campaign.RestaurantLocationId == locationId,
                    cancellationToken
                );

            var unsubscribesInWeek = await context.LocationActivities
                .AsNoTracking()
                .CountAsync(
                    activity =>
                        activity.LocationId == locationId
                        && activity.Kind
                            == LocationActivityKinds.GuestMarketingUnsubscribed
                        && activity.OccurredAt >= fromUtc
                        && activity.OccurredAt < toUtc,
                    cancellationToken
                );

            return new WeeklyBriefMetrics(
                GuestsJoined: guestsJoined,
                QrScanEvents: qrScanEvents,
                FeedbackCount: feedbackCount,
                PositiveFeedbackCount: positiveFeedbackCount,
                NeutralFeedbackCount: neutralFeedbackCount,
                NegativeFeedbackCount: negativeFeedbackCount,
                NeedsAttentionCount: needsAttentionCount,
                DetectedTagCounts: detectedTagCounts,
                ActiveOffers: activeOffers,
                ClaimsInWeek: claimsInWeek,
                RedemptionsInWeek: redemptionsInWeek,
                CampaignsSentInWeek: campaignsSentInWeek,
                CampaignRecipientsReached: campaignRecipientsReached,
                UnsubscribesInWeek: unsubscribesInWeek
            );
        }

        /// <summary>
        /// Fresh aggregate for the prior closed week of <paramref name="weekKey"/>.
        /// Returns null when the prior key or coverage window cannot be resolved.
        /// Does not require a stored prior Weekly brief row.
        /// </summary>
        public static async Task<WeeklyBriefMetrics?> LoadPriorWeekAsync(
            ApplicationDbContext context,
            int locationId,
            string weekKey,
            string ianaTimeZoneId,
            CancellationToken cancellationToken = default
        )
        {
            if (!WeeklyBriefWeekKey.TryPriorWeekKey(weekKey, out var priorKey))
            {
                return null;
            }

            if (
                !WeeklyBriefWeekKey.TryCoverageWindow(
                    priorKey,
                    ianaTimeZoneId,
                    out var fromUtc,
                    out var toUtc
                )
            )
            {
                return null;
            }

            return await LoadAsync(
                context,
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
        }

        private static IReadOnlyDictionary<string, int> RollUpDetectedTagCounts(
            IReadOnlyList<string?> tagJsonRows
        )
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var json in tagJsonRows)
            {
                var keys = FeedbackClassificationMapping.DeserializeDetectedTagKeys(
                    json
                );
                if (keys is null)
                {
                    continue;
                }

                foreach (var key in keys)
                {
                    if (!DetectedTagLabels.TryParseKey(key, out var tag))
                    {
                        continue;
                    }

                    var label = DetectedTagLabels.For(tag);
                    counts[label] = counts.TryGetValue(label, out var current)
                        ? current + 1
                        : 1;
                }
            }

            return counts;
        }
    }
}
