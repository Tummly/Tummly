using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Weekly brief generate orchestrator: load aggregates → Azure/Fake → persist.
    /// Idempotent for location + week key. Prefer no row until success.
    /// Free call — no AI credit debit.
    /// </summary>
    public sealed class WeeklyBriefGenerateService : IWeeklyBriefGenerateService
    {
        private static readonly string FailMessage =
            "Could not generate a weekly brief. Please try again.";

        private readonly ApplicationDbContext _context;
        private readonly IWeeklyBriefProvider _provider;
        private readonly ILogger<WeeklyBriefGenerateService> _logger;

        public WeeklyBriefGenerateService(
            ApplicationDbContext context,
            IWeeklyBriefProvider provider,
            ILogger<WeeklyBriefGenerateService> logger
        )
        {
            _context = context;
            _provider = provider;
            _logger = logger;
        }

        public async Task<WeeklyBriefGenerateResult> GenerateAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken = default
        )
        {
            if (locationId <= 0)
            {
                throw new ArgumentException("locationId is required.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(closedWeek.WeekKey);

            var existing = await _context.WeeklyBriefs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.LocationId == locationId
                        && row.WeekKey == closedWeek.WeekKey
                        && row.Status == WeeklyBriefStatus.Succeeded,
                    cancellationToken
                );

            if (existing is not null)
            {
                return new WeeklyBriefGenerateResult.Succeeded(
                    existing,
                    Created: false
                );
            }

            var locationName = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == locationId)
                .Select(location => location.LocationName)
                .FirstOrDefaultAsync(cancellationToken);

            if (locationName is null)
            {
                return new WeeklyBriefGenerateResult.Failed(
                    "Location was not found.",
                    Retryable: false
                );
            }

            var metrics = await LoadMetricsAsync(
                locationId,
                closedWeek.CoverageStartUtc,
                closedWeek.CoverageEndUtcExclusive,
                cancellationToken
            );

            WeeklyBriefProviderResult providerResult;
            try
            {
                providerResult = await _provider.GenerateAsync(
                    new WeeklyBriefProviderInput(
                        LocationName: locationName,
                        WeekKey: closedWeek.WeekKey,
                        CoverageStartUtc: closedWeek.CoverageStartUtc,
                        CoverageEndUtcExclusive: closedWeek.CoverageEndUtcExclusive,
                        Metrics: metrics
                    ),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Weekly brief provider threw for location {LocationId} week {WeekKey}",
                    locationId,
                    closedWeek.WeekKey
                );
                return new WeeklyBriefGenerateResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            if (providerResult is WeeklyBriefProviderResult.Failed failed)
            {
                return new WeeklyBriefGenerateResult.Failed(
                    FailMessage,
                    failed.Retryable
                );
            }

            if (
                providerResult
                is not WeeklyBriefProviderResult.Succeeded succeeded
            )
            {
                return new WeeklyBriefGenerateResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            var body = AttachEchoedCounts(succeeded.Body, metrics);
            var generatedAtUtc = DateTime.UtcNow;
            var row = new WeeklyBrief
            {
                LocationId = locationId,
                WeekKey = closedWeek.WeekKey,
                Status = WeeklyBriefStatus.Succeeded,
                GeneratedAtUtc = generatedAtUtc,
                BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options),
                MetricsJson = JsonSerializer.Serialize(metrics, WeeklyBriefStoreJson.Options),
                EnrichmentJson = succeeded.Enrichment is null
                    ? null
                    : JsonSerializer.Serialize(
                        succeeded.Enrichment,
                        WeeklyBriefStoreJson.Options
                    ),
                ErrorInfo = null,
            };

            _context.WeeklyBriefs.Add(row);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Concurrent first-write race: unique (LocationId, WeekKey).
                _logger.LogInformation(
                    ex,
                    "Weekly brief row already exists for location {LocationId} week {WeekKey}; returning existing.",
                    locationId,
                    closedWeek.WeekKey
                );

                _context.Entry(row).State = EntityState.Detached;
                var raced = await _context.WeeklyBriefs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        existingRow =>
                            existingRow.LocationId == locationId
                            && existingRow.WeekKey == closedWeek.WeekKey
                            && existingRow.Status == WeeklyBriefStatus.Succeeded,
                        cancellationToken
                    );

                if (raced is not null)
                {
                    return new WeeklyBriefGenerateResult.Succeeded(
                        raced,
                        Created: false
                    );
                }

                return new WeeklyBriefGenerateResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            return new WeeklyBriefGenerateResult.Succeeded(row, Created: true);
        }

        private async Task<WeeklyBriefMetrics> LoadMetricsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var guestsJoined = await _context.LocationGuests
                .AsNoTracking()
                .CountAsync(
                    guest =>
                        guest.RestaurantLocationId == locationId
                        && guest.CreatedAt >= fromUtc
                        && guest.CreatedAt < toUtc,
                    cancellationToken
                );

            var qrScanEvents = await _context.QrScanEvents
                .AsNoTracking()
                .CountAsync(
                    scan =>
                        scan.RestaurantLocationId == locationId
                        && scan.CreatedAt >= fromUtc
                        && scan.CreatedAt < toUtc,
                    cancellationToken
                );

            var feedbackInWindow = _context.Feedbacks
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

            var activeOffers = await _context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    offer =>
                        offer.RestaurantLocationId == locationId
                        && offer.Status == CatalogOfferStatus.Active,
                    cancellationToken
                );

            var offerIssuesInLocation = _context.OfferIssues
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

            var campaignsSentInWeek = await _context.Campaigns
                .AsNoTracking()
                .CountAsync(
                    campaign =>
                        campaign.RestaurantLocationId == locationId
                        && campaign.Status == CampaignLifecycleService.SentStatus
                        && campaign.UpdatedAt >= fromUtc
                        && campaign.UpdatedAt < toUtc,
                    cancellationToken
                );

            var campaignRecipientsReached = await _context.CampaignFrozenRecipients
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

            var unsubscribesInWeek = await _context.LocationActivities
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

        private static WeeklyBriefBody AttachEchoedCounts(
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics
        )
            => body with
            {
                Capture = body.Capture with
                {
                    EchoedCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        ["guestsJoined"] = metrics.GuestsJoined,
                        ["qrScanEvents"] = metrics.QrScanEvents,
                    },
                },
                Feedback = body.Feedback with
                {
                    EchoedCounts = BuildFeedbackEchoedCounts(metrics),
                },
                Offers = body.Offers with
                {
                    EchoedCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        ["activeOffers"] = metrics.ActiveOffers,
                        ["claimsInWeek"] = metrics.ClaimsInWeek,
                        ["redemptionsInWeek"] = metrics.RedemptionsInWeek,
                    },
                },
                Campaigns = body.Campaigns with
                {
                    EchoedCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        ["campaignsSentInWeek"] = metrics.CampaignsSentInWeek,
                        ["campaignRecipientsReached"] =
                            metrics.CampaignRecipientsReached,
                    },
                },
            };

        private static IReadOnlyDictionary<string, int> BuildFeedbackEchoedCounts(
            WeeklyBriefMetrics metrics
        )
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["feedbackCount"] = metrics.FeedbackCount,
                ["positiveFeedbackCount"] = metrics.PositiveFeedbackCount,
                ["neutralFeedbackCount"] = metrics.NeutralFeedbackCount,
                ["negativeFeedbackCount"] = metrics.NegativeFeedbackCount,
                ["needsAttentionCount"] = metrics.NeedsAttentionCount,
            };

            foreach (var pair in metrics.DetectedTagCounts)
            {
                map[pair.Key] = pair.Value;
            }

            return map;
        }
    }
}
