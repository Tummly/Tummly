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

            var locationMeta = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == locationId)
                .Select(location => new
                {
                    location.LocationName,
                    SubscriptionPlan = location.Restaurant != null
                        && location.Restaurant.BillingAccount != null
                            ? location.Restaurant.BillingAccount.SubscriptionPlan
                            : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (locationMeta is null)
            {
                return new WeeklyBriefGenerateResult.Failed(
                    "Location was not found.",
                    Retryable: false
                );
            }

            if (WeeklyBriefWeekKey.IsPilotPlan(locationMeta.SubscriptionPlan))
            {
                return new WeeklyBriefGenerateResult.Failed(
                    "Weekly brief is not available on the Pilot plan.",
                    Retryable: false
                );
            }

            var metrics = await WeeklyBriefMetricsLoader.LoadAsync(
                _context,
                locationId,
                closedWeek.CoverageStartUtc,
                closedWeek.CoverageEndUtcExclusive,
                cancellationToken
            );

            var insightCandidates =
                await WeeklyBriefInsightCandidateBuild.BuildAsync(
                    _context,
                    locationId,
                    closedWeek.WeekKey,
                    closedWeek.CoverageStartUtc,
                    closedWeek.CoverageEndUtcExclusive,
                    metrics,
                    cancellationToken: cancellationToken
                );

            WeeklyBriefProviderResult providerResult;
            try
            {
                providerResult = await _provider.GenerateAsync(
                    new WeeklyBriefProviderInput(
                        LocationName: locationMeta.LocationName,
                        WeekKey: closedWeek.WeekKey,
                        CoverageStartUtc: closedWeek.CoverageStartUtc,
                        CoverageEndUtcExclusive: closedWeek.CoverageEndUtcExclusive,
                        Metrics: metrics,
                        InsightCandidates: insightCandidates
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
            var enrichment = WeeklyBriefInsightNarrativeValidation.AttachCandidates(
                succeeded.Enrichment,
                insightCandidates
            );
            if (
                !WeeklyBriefInsightNarrativeValidation.TryValidate(
                    body,
                    enrichment,
                    insightCandidates,
                    out var validationReason
                )
            )
            {
                _logger.LogWarning(
                    "Weekly brief narrative validation failed for location {LocationId} week {WeekKey}: {Reason}",
                    locationId,
                    closedWeek.WeekKey,
                    validationReason
                );
                return new WeeklyBriefGenerateResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            var generatedAtUtc = DateTime.UtcNow;
            var row = new WeeklyBrief
            {
                LocationId = locationId,
                WeekKey = closedWeek.WeekKey,
                Status = WeeklyBriefStatus.Succeeded,
                GeneratedAtUtc = generatedAtUtc,
                BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options),
                MetricsJson = JsonSerializer.Serialize(metrics, WeeklyBriefStoreJson.Options),
                EnrichmentJson = JsonSerializer.Serialize(
                    enrichment,
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
