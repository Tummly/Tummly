using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Produces <c>weekly-brief-ready</c> after a successful first-write generate.
    /// Preference gate and dedupe live in <see cref="IOperatorNotificationsService.ProduceAsync"/>.
    /// On Created, also sends the Weekly Brief email to the workspace owner.
    /// CTA opens the Reports Weekly Brief page for the location.
    /// </summary>
    public sealed class WeeklyBriefReadyNotifier : IWeeklyBriefReadyNotifier
    {
        public const string NotificationType = "weekly-brief-ready";

        public const string CtaLabel = "View weekly brief";

        public const string FallbackWhatChanged =
            "See your Weekly Brief for this week's summary.";

        public const string FallbackRecommendedNextStep =
            "Open your Weekly Brief for the full recommended next step.";

        /// <summary>
        /// Single-location: <c>/single-dashboard/reports/weekly-brief?location={id}</c>.
        /// Multi-location: <c>/multi-dashboard/reports/weekly-brief?location={id}</c>.
        /// Matches frontend <c>operatorDashboardWeeklyBriefPath</c>.
        /// </summary>
        public static string ReportsWeeklyBriefCtaHref(string accountType, int locationId)
            => string.Equals(accountType, "Multi", StringComparison.Ordinal)
                ? $"/multi-dashboard/reports/weekly-brief?location={locationId}"
                : $"/single-dashboard/reports/weekly-brief?location={locationId}";

        public static string DedupeKeyFor(int locationId, string weekKey)
            => $"{locationId}:{weekKey}";

        private readonly ApplicationDbContext _context;
        private readonly IOperatorNotificationsService _notifications;
        private readonly IEmailService _email;
        private readonly ILogger<WeeklyBriefReadyNotifier> _logger;

        public WeeklyBriefReadyNotifier(
            ApplicationDbContext context,
            IOperatorNotificationsService notifications,
            IEmailService email,
            ILogger<WeeklyBriefReadyNotifier>? logger = null
        )
        {
            _context = context;
            _notifications = notifications;
            _email = email;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    WeeklyBriefReadyNotifier
                >.Instance;
        }

        public async Task NotifyGeneratedAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken = default
        )
        {
            var owner = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == locationId)
                .Select(location => new
                {
                    location.LocationName,
                    location.Restaurant!.OwnerUserId,
                    location.Restaurant.AccountType,
                    OwnerEmail = location.Restaurant.OwnerUser!.Email,
                    OwnerFullName = location.Restaurant.OwnerUser.FullName,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (owner is null)
            {
                return;
            }

            var locationName = string.IsNullOrWhiteSpace(owner.LocationName)
                ? "Location"
                : owner.LocationName.Trim();
            var ctaHref = ReportsWeeklyBriefCtaHref(
                owner.AccountType,
                locationId
            );

            ProduceNotificationResult produceResult;
            try
            {
                produceResult = await _notifications.ProduceAsync(
                    new ProduceNotificationRequest
                    {
                        UserId = owner.OwnerUserId,
                        Type = NotificationType,
                        Title = $"Weekly brief ready — {locationName}",
                        Body =
                            $"Your weekly summary for {locationName} is ready.",
                        CtaLabel = CtaLabel,
                        CtaHref = ctaHref,
                        DedupeKey = DedupeKeyFor(
                            locationId,
                            closedWeek.WeekKey
                        ),
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to produce weekly-brief-ready for location {LocationId} week {WeekKey}",
                    locationId,
                    closedWeek.WeekKey
                );
                return;
            }

            if (produceResult.Status != ProduceNotificationStatus.Created)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(owner.OwnerEmail))
            {
                return;
            }

            try
            {
                await SendWeeklyBriefEmailAsync(
                    locationId,
                    closedWeek,
                    owner.OwnerEmail.Trim(),
                    SignInMetadataResolver.ExtractFirstName(owner.OwnerFullName),
                    locationName,
                    ctaHref,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send Weekly Brief email for location {LocationId} week {WeekKey}",
                    locationId,
                    closedWeek.WeekKey
                );
            }
        }

        private async Task SendWeeklyBriefEmailAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            string toEmail,
            string firstName,
            string locationName,
            string weeklyBriefUrl,
            CancellationToken cancellationToken
        )
        {
            var brief = await _context.WeeklyBriefs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.LocationId == locationId
                        && row.WeekKey == closedWeek.WeekKey
                        && row.Status == WeeklyBriefStatus.Succeeded,
                    cancellationToken
                );

            if (brief is null)
            {
                return;
            }

            WeeklyBriefBody? body;
            WeeklyBriefMetrics? metrics;
            try
            {
                body = JsonSerializer.Deserialize<WeeklyBriefBody>(
                    brief.BodyJson,
                    WeeklyBriefStoreJson.Options
                );
                metrics = JsonSerializer.Deserialize<WeeklyBriefMetrics>(
                    brief.MetricsJson,
                    WeeklyBriefStoreJson.Options
                );
            }
            catch (JsonException)
            {
                _logger.LogWarning(
                    "Stored weekly brief could not be read for email location {LocationId} week {WeekKey}",
                    locationId,
                    closedWeek.WeekKey
                );
                return;
            }

            if (body is null || metrics is null)
            {
                return;
            }

            var enrichment = WeeklyBriefEnrichmentApply.TryDeserialize(
                brief.EnrichmentJson
            );
            var phase1 = WeeklyBriefPhase1Meta.Build(
                body,
                metrics,
                closedWeek.WeekKey
            );
            var whatChanged = WeeklyBriefEnrichmentApply.ResolveExecutiveSummary(
                phase1.ExecutiveSummary,
                enrichment
            );
            if (string.IsNullOrWhiteSpace(whatChanged))
            {
                whatChanged = FallbackWhatChanged;
            }

            var recommendedNextStep = await ResolveRecommendedNextStepAsync(
                locationId,
                closedWeek,
                metrics,
                body,
                enrichment,
                cancellationToken
            );

            await _email.SendWeeklyBriefEmailAsync(
                toEmail,
                firstName,
                locationName,
                phase1.Meta.Period,
                metrics.QrScanEvents,
                metrics.FeedbackCount,
                metrics.GuestsJoined,
                metrics.ClaimsInWeek,
                metrics.RedemptionsInWeek,
                metrics.CampaignRecipientsReached,
                whatChanged,
                recommendedNextStep,
                weeklyBriefUrl
            );
        }

        private async Task<string> ResolveRecommendedNextStepAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            WeeklyBriefMetrics metrics,
            WeeklyBriefBody body,
            WeeklyBriefEnrichment? enrichment,
            CancellationToken cancellationToken
        )
        {
            var facts = await WeeklyBriefRecommendedActions.BuildFactsAsync(
                _context,
                locationId,
                metrics,
                closedWeek.CoverageStartUtc,
                closedWeek.CoverageEndUtcExclusive,
                cancellationToken
            );
            facts = WeeklyBriefEnrichmentApply.ApplyActionWording(
                facts,
                enrichment
            );

            if (facts.Count > 0)
            {
                return WeeklyBriefEnrichmentApply.FormatRecommendedActionLine(
                    facts[0]
                );
            }

            foreach (var watch in body.WatchNext)
            {
                if (!string.IsNullOrWhiteSpace(watch))
                {
                    return watch.Trim();
                }
            }

            return FallbackRecommendedNextStep;
        }
    }
}
