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
    /// Recipients: restaurant owner plus active team members with Reports view (or
    /// higher) and access to the Owned location. Preference gate and dedupe live in
    /// <see cref="IOperatorNotificationsService.ProduceAsync"/> per user.
    /// On Created for a recipient, also sends the Weekly Brief system email (0
    /// Campaign credits). CTA opens the Reports Weekly Brief page for the location.
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
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => new
                {
                    row.LocationName,
                    RestaurantId = row.Restaurant!.Id,
                    row.Restaurant.OwnerUserId,
                    row.Restaurant.AccountType,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (location is null)
            {
                return;
            }

            var locationName = string.IsNullOrWhiteSpace(location.LocationName)
                ? "Location"
                : location.LocationName.Trim();
            var ctaHref = ReportsWeeklyBriefCtaHref(
                location.AccountType,
                locationId
            );
            var recipients = await ResolveRecipientsAsync(
                location.RestaurantId,
                locationId,
                location.OwnerUserId,
                cancellationToken
            );
            if (recipients.Count == 0)
            {
                return;
            }

            var emailPayload = await TryBuildEmailPayloadAsync(
                locationId,
                closedWeek,
                locationName,
                ctaHref,
                cancellationToken
            );

            var title = $"Weekly brief ready — {locationName}";
            var body = $"Your weekly summary for {locationName} is ready.";
            var dedupeKey = DedupeKeyFor(locationId, closedWeek.WeekKey);

            foreach (var recipient in recipients)
            {
                ProduceNotificationResult produceResult;
                try
                {
                    produceResult = await _notifications.ProduceAsync(
                        new ProduceNotificationRequest
                        {
                            UserId = recipient.UserId,
                            Type = NotificationType,
                            Title = title,
                            Body = body,
                            CtaLabel = CtaLabel,
                            CtaHref = ctaHref,
                            DedupeKey = dedupeKey,
                        }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to produce weekly-brief-ready for user {UserId} location {LocationId} week {WeekKey}",
                        recipient.UserId,
                        locationId,
                        closedWeek.WeekKey
                    );
                    continue;
                }

                if (produceResult.Status != ProduceNotificationStatus.Created)
                {
                    continue;
                }

                if (emailPayload is null
                    || string.IsNullOrWhiteSpace(recipient.Email))
                {
                    continue;
                }

                try
                {
                    await _email.SendWeeklyBriefEmailAsync(
                        recipient.Email.Trim(),
                        recipient.FirstName,
                        emailPayload.LocationName,
                        emailPayload.PeriodLabel,
                        emailPayload.QrScans,
                        emailPayload.FeedbackReceived,
                        emailPayload.GuestsCaptured,
                        emailPayload.OfferClaimed,
                        emailPayload.Redemptions,
                        emailPayload.CampaignEngagement,
                        emailPayload.WhatChanged,
                        emailPayload.RecommendedNextStep,
                        emailPayload.WeeklyBriefUrl
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send Weekly Brief email for user {UserId} location {LocationId} week {WeekKey}",
                        recipient.UserId,
                        locationId,
                        closedWeek.WeekKey
                    );
                }
            }
        }

        private async Task<IReadOnlyList<WeeklyBriefEmailRecipient>> ResolveRecipientsAsync(
            int restaurantId,
            int locationId,
            int ownerUserId,
            CancellationToken cancellationToken
        )
        {
            var memberships = await _context.RestaurantMemberships
                .AsNoTracking()
                .Include(row => row.User)
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                )
                .ToListAsync(cancellationToken);

            var adminOverrides = await _context.RestaurantAdminPermissionCells
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToDictionaryAsync(
                    row => row.AreaId,
                    row => row.Level,
                    cancellationToken
                );

            var byUserId = new Dictionary<int, WeeklyBriefEmailRecipient>();

            void TryAdd(User? user, RestaurantMembership? membership, bool isOwner)
            {
                if (user is null || byUserId.ContainsKey(user.Id))
                {
                    return;
                }

                if (!isOwner)
                {
                    if (membership is null
                        || !LocationDetailTeamAccessBuilder.HasAccessToLocation(
                            membership,
                            locationId
                        ))
                    {
                        return;
                    }
                }

                var permissionRole = membership?.PermissionRole
                    ?? (isOwner ? PermissionRoles.Owner : PermissionRoles.Staff);
                var reportsLevel = DefaultPermissionMatrix.LevelFor(
                    permissionRole,
                    OperatorAreaIds.Reports,
                    adminOverrides
                );
                if (!DefaultPermissionMatrix.Meets(
                        reportsLevel,
                        PermissionLevel.View
                    ))
                {
                    return;
                }

                byUserId[user.Id] = new WeeklyBriefEmailRecipient(
                    user.Id,
                    user.Email ?? string.Empty,
                    SignInMetadataResolver.ExtractFirstName(user.FullName)
                );
            }

            var ownerUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == ownerUserId, cancellationToken);
            var ownerMembership = memberships.FirstOrDefault(
                row => row.UserId == ownerUserId
            );
            TryAdd(ownerUser, ownerMembership, isOwner: true);

            foreach (var membership in memberships)
            {
                if (membership.UserId == ownerUserId)
                {
                    continue;
                }

                TryAdd(membership.User, membership, isOwner: false);
            }

            return byUserId.Values.ToList();
        }

        private async Task<WeeklyBriefEmailPayload?> TryBuildEmailPayloadAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
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
                return null;
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
                return null;
            }

            if (body is null || metrics is null)
            {
                return null;
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

            return new WeeklyBriefEmailPayload(
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

        private sealed record WeeklyBriefEmailRecipient(
            int UserId,
            string Email,
            string FirstName
        );

        private sealed record WeeklyBriefEmailPayload(
            string LocationName,
            string PeriodLabel,
            int QrScans,
            int FeedbackReceived,
            int GuestsCaptured,
            int OfferClaimed,
            int Redemptions,
            int CampaignEngagement,
            string WhatChanged,
            string RecommendedNextStep,
            string WeeklyBriefUrl
        );
    }
}
