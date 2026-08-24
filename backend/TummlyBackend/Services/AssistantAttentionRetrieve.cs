using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.OperatorHome;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Loads Home surfaces for Attention Retrieve at one Owned location.
    /// Free call — no AI credit debit.
    /// </summary>
    public sealed class AssistantAttentionRetrieve : IAssistantAttentionRetrieve
    {
        private const int FeedbackInboxPageSize = 25;
        private const int CampaignsPageSize = 25;
        private const int OffersPageSize = 100;
        private const int ExpiryWindowDays = 7;

        private readonly ApplicationDbContext _context;
        private readonly IFeedbackInboxListService _feedbackInbox;
        private readonly ICampaignsListService _campaignsList;
        private readonly IOffersCatalogService _offersCatalog;
        private readonly IOfferVoidRequestService _voidRequests;
        private readonly IHomeRecommendationService _recommendation;
        private readonly IWeeklyBriefGenerateService _weeklyBriefs;

        public AssistantAttentionRetrieve(
            ApplicationDbContext context,
            IFeedbackInboxListService feedbackInbox,
            ICampaignsListService campaignsList,
            IOffersCatalogService offersCatalog,
            IOfferVoidRequestService voidRequests,
            IHomeRecommendationService recommendation,
            IWeeklyBriefGenerateService weeklyBriefs
        )
        {
            _context = context;
            _feedbackInbox = feedbackInbox;
            _campaignsList = campaignsList;
            _offersCatalog = offersCatalog;
            _voidRequests = voidRequests;
            _recommendation = recommendation;
            _weeklyBriefs = weeklyBriefs;
        }

        public async Task<AssistantAttentionTurn> PresentAsync(
            AssistantAttentionSurface surface,
            int operatorUserId,
            int locationId,
            string locationName,
            AssistantReportingPeriodDto reportingPeriod,
            CancellationToken cancellationToken = default
        )
        {
            var periodPhrase = AssistantAnalysisScope.PeriodPhrase(reportingPeriod);
            return surface switch
            {
                AssistantAttentionSurface.NeedsAttention =>
                    await PresentNeedsAttentionAsync(
                        locationId,
                        locationName,
                        cancellationToken
                    ),
                AssistantAttentionSurface.RecommendedNextStep =>
                    await PresentRecommendedNextStepAsync(
                        operatorUserId,
                        locationId,
                        locationName,
                        reportingPeriod,
                        periodPhrase,
                        cancellationToken
                    ),
                AssistantAttentionSurface.WeeklyBrief =>
                    await PresentWeeklyBriefAsync(
                        locationId,
                        locationName,
                        cancellationToken
                    ),
                AssistantAttentionSurface.Mix =>
                    await PresentMixAsync(
                        operatorUserId,
                        locationId,
                        locationName,
                        reportingPeriod,
                        periodPhrase,
                        cancellationToken
                    ),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(surface),
                    surface,
                    "Attention Retrieve surface is required."
                ),
            };
        }

        private async Task<AssistantAttentionTurn> PresentNeedsAttentionAsync(
            int locationId,
            string locationName,
            CancellationToken cancellationToken
        )
        {
            var loaded = await TryLoadQueueAsync(
                locationId,
                locationName,
                cancellationToken
            );
            if (loaded is null)
            {
                return new AssistantAttentionTurn(
                    AssistantAttentionCopy.NeedsAttentionErrorTitle(locationName),
                    AssistantAttentionCopy.NeedsAttentionErrorBody(locationName),
                    []
                );
            }

            return new AssistantAttentionTurn(
                AssistantAttentionCopy.NeedsAttentionTitle(loaded.Count, locationName),
                AssistantAttentionCopy.NeedsAttentionBody(locationName, loaded),
                AssistantAttentionActions.ForNeedsAttention(loaded)
            );
        }

        private async Task<AssistantAttentionTurn> PresentRecommendedNextStepAsync(
            int operatorUserId,
            int locationId,
            string locationName,
            AssistantReportingPeriodDto reportingPeriod,
            string periodPhrase,
            CancellationToken cancellationToken
        )
        {
            var loaded = await TryLoadRecommendationAsync(
                operatorUserId,
                locationId,
                reportingPeriod,
                cancellationToken
            );
            if (loaded is null)
            {
                return new AssistantAttentionTurn(
                    "Recommended next step",
                    AssistantAttentionCopy.RecommendedNextStepErrorBody(
                        locationName,
                        periodPhrase
                    ),
                    []
                );
            }

            var title = AssistantAttentionCopy.IsNone(loaded)
                || string.IsNullOrWhiteSpace(loaded.Title)
                ? "Recommended next step"
                : loaded.Title!;
            return new AssistantAttentionTurn(
                title,
                AssistantAttentionCopy.RecommendedNextStepBody(
                    locationName,
                    periodPhrase,
                    loaded
                ),
                AssistantAttentionActions.ForRecommendedNextStep(
                    loaded.Type,
                    loaded.Action?.LocationGuestId,
                    loaded.Action?.OfferId
                )
            );
        }

        private async Task<AssistantAttentionTurn> PresentWeeklyBriefAsync(
            int locationId,
            string locationName,
            CancellationToken cancellationToken
        )
        {
            var weekStartsOn = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.Restaurant != null ? l.Restaurant.WeekStartsOn : null)
                .FirstOrDefaultAsync(cancellationToken);

            var closedWeek = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow,
                weekStartsOn
            );
            var loaded = await TryLoadWeeklyBriefAsync(
                locationId,
                closedWeek,
                cancellationToken
            );
            if (loaded is WeeklyBriefLoad.Error)
            {
                return new AssistantAttentionTurn(
                    "Weekly brief",
                    AssistantAttentionCopy.WeeklyBriefErrorBody(
                        locationName,
                        closedWeek.WeekKey
                    ),
                    []
                );
            }

            if (loaded is WeeklyBriefLoad.Ready ready)
            {
                return new AssistantAttentionTurn(
                    ready.Body.Headline,
                    AssistantAttentionCopy.WeeklyBriefBodyText(
                        locationName,
                        closedWeek.WeekKey,
                        ready.Body
                    ),
                    []
                );
            }

            return new AssistantAttentionTurn(
                AssistantAttentionCopy.WeeklyBriefEmptyTitle,
                AssistantAttentionCopy.WeeklyBriefEmptyBody(
                    locationName,
                    closedWeek.WeekKey
                ),
                []
            );
        }

        private async Task<AssistantAttentionTurn> PresentMixAsync(
            int operatorUserId,
            int locationId,
            string locationName,
            AssistantReportingPeriodDto reportingPeriod,
            string periodPhrase,
            CancellationToken cancellationToken
        )
        {
            var queueTask = TryLoadQueueAsync(
                locationId,
                locationName,
                cancellationToken
            );
            var recTask = TryLoadRecommendationAsync(
                operatorUserId,
                locationId,
                reportingPeriod,
                cancellationToken
            );
            await Task.WhenAll(queueTask, recTask);
            var queue = queueTask.Result;
            var recommendation = recTask.Result;
            var queueError = queue is null;
            var recError = recommendation is null;
            var body = AssistantAttentionCopy.MixBody(
                locationName,
                periodPhrase,
                queue,
                queueError,
                recommendation,
                recError
            );
            var actions = AssistantAttentionActions.Union(
                queueError || queue is null
                    ? []
                    : AssistantAttentionActions.ForNeedsAttention(queue),
                recError || recommendation is null
                    ? []
                    : AssistantAttentionActions.ForRecommendedNextStep(
                        recommendation.Type,
                        recommendation.Action?.LocationGuestId,
                        recommendation.Action?.OfferId
                    )
            );
            return new AssistantAttentionTurn(
                AssistantAttentionCopy.MixTitle(locationName, queue, recommendation),
                body,
                actions
            );
        }

        private async Task<IReadOnlyList<AssistantHomeNeedsAttentionItem>?> TryLoadQueueAsync(
            int locationId,
            string locationName,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                var utcOffsetMinutes = LondonOffsetMinutes(nowUtc);
                var fromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var toUtc = nowUtc.AddDays(1);

                var feedbackTask = _feedbackInbox.ListAsync(
                    new FeedbackInboxListQuery
                    {
                        LocationId = locationId,
                        LocationName = locationName,
                        FromUtc = fromUtc,
                        ToUtc = toUtc,
                        Tab = "needs-attention",
                        Sort = "newest-submitted",
                        Page = 1,
                        PageSize = FeedbackInboxPageSize,
                        UtcOffsetMinutes = utcOffsetMinutes,
                    },
                    cancellationToken
                );
                var campaignsTask = _campaignsList.ListAsync(
                    new CampaignsListQuery
                    {
                        LocationId = locationId,
                        LocationIds = [locationId],
                        LocationNamesById = new Dictionary<int, string>
                        {
                            [locationId] = locationName,
                        },
                        View = "needs-attention",
                        Sort = "recent-activity",
                        Page = 1,
                        PageSize = CampaignsPageSize,
                        UtcOffsetMinutes = utcOffsetMinutes,
                    },
                    cancellationToken
                );
                var offersTask = _offersCatalog.ListAsync(
                    new CatalogOffersListQuery
                    {
                        LocationId = locationId,
                        View = "needs-attention",
                        Sort = "recent-activity",
                        Page = 1,
                        PageSize = OffersPageSize,
                        UtcOffsetMinutes = utcOffsetMinutes,
                    },
                    cancellationToken
                );
                var voidsTask = _voidRequests.ListOpenAttentionAsync(
                    locationId,
                    cancellationToken
                );
                await Task.WhenAll(feedbackTask, campaignsTask, offersTask, voidsTask);

                var feedback = feedbackTask.Result;
                var campaigns = campaignsTask.Result.Items
                    .Where(item =>
                        item.Status is "failed" or "partially-sent"
                    )
                    .Select(item => new AssistantHomeNeedsAttentionCampaignFact(
                        item.Id,
                        item.Name,
                        item.Status,
                        DateTime.SpecifyKind(item.UpdatedAt, DateTimeKind.Utc)
                    ))
                    .ToList();
                var voids = voidsTask.Result;
                var voidByOfferId = voids.ToDictionary(item => item.OfferId);
                var offerFacts = new List<AssistantHomeNeedsAttentionOfferFact>();
                var seen = new HashSet<int>();
                foreach (var item in offersTask.Result.Items)
                {
                    seen.Add(item.Id);
                    offerFacts.Add(
                        MapOfferFact(
                            item.Id,
                            item.Title,
                            item.LifetimeClaims,
                            item.LifetimeRedeemed,
                            item.ExpiryDate,
                            voidByOfferId.GetValueOrDefault(item.Id),
                            nowUtc,
                            utcOffsetMinutes
                        )
                    );
                }

                foreach (var openVoid in voids)
                {
                    if (!seen.Add(openVoid.OfferId))
                    {
                        continue;
                    }

                    offerFacts.Add(
                        MapOfferFact(
                            openVoid.OfferId,
                            openVoid.OfferTitle,
                            0,
                            0,
                            expiryDate: null,
                            openVoid,
                            nowUtc,
                            utcOffsetMinutes
                        )
                    );
                }

                DateTime? newestFeedback = feedback.Items.Count > 0
                    ? DateTime.SpecifyKind(
                        feedback.Items[0].CreatedAt,
                        DateTimeKind.Utc
                    )
                    : null;
                return AssistantHomeNeedsAttention.Project(
                    locationName,
                    nowUtc,
                    feedback.TabCounts.NeedsAttention,
                    newestFeedback,
                    campaigns,
                    offerFacts
                );
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<HomeRecommendationDto?> TryLoadRecommendationAsync(
            int operatorUserId,
            int locationId,
            AssistantReportingPeriodDto reportingPeriod,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var window = AssistantReportingPeriodWindow.Resolve(
                    reportingPeriod,
                    DateTime.UtcNow
                );
                var preset = string.Equals(
                    reportingPeriod.Kind,
                    "custom",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "custom"
                    : reportingPeriod.PresetId ?? "last7";
                var result = await _recommendation.RecommendAsync(
                    operatorUserId,
                    new HomeRecommendationRequest
                    {
                        LocationId = locationId,
                        OverviewDatePreset = preset,
                        From = window.FromUtc,
                        To = window.ToUtc,
                    },
                    cancellationToken
                );
                return result is HomeRecommendationServiceResult.Ok ok
                    ? ok.Recommendation
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<WeeklyBriefLoad> TryLoadWeeklyBriefAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var existing = await ReadSucceededBriefAsync(
                    locationId,
                    closedWeek.WeekKey,
                    cancellationToken
                );
                if (existing is not null)
                {
                    return existing;
                }

                var generated = await _weeklyBriefs.GenerateAsync(
                    locationId,
                    closedWeek,
                    cancellationToken
                );
                if (generated is WeeklyBriefGenerateResult.Failed)
                {
                    return new WeeklyBriefLoad.Error();
                }

                if (generated is WeeklyBriefGenerateResult.Succeeded succeeded)
                {
                    if (string.IsNullOrWhiteSpace(succeeded.Brief.BodyJson))
                    {
                        return new WeeklyBriefLoad.Empty();
                    }

                    var body = DeserializeBody(succeeded.Brief);
                    return body is null
                        ? new WeeklyBriefLoad.Error()
                        : new WeeklyBriefLoad.Ready(body);
                }

                return new WeeklyBriefLoad.Empty();
            }
            catch (Exception)
            {
                return new WeeklyBriefLoad.Error();
            }
        }

        private async Task<WeeklyBriefLoad.Ready?> ReadSucceededBriefAsync(
            int locationId,
            string weekKey,
            CancellationToken cancellationToken
        )
        {
            var row = await _context.WeeklyBriefs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    brief =>
                        brief.LocationId == locationId
                        && brief.WeekKey == weekKey
                        && brief.Status == WeeklyBriefStatus.Succeeded,
                    cancellationToken
                );
            if (row is null)
            {
                return null;
            }

            var body = DeserializeBody(row);
            return body is null ? null : new WeeklyBriefLoad.Ready(body);
        }

        private static WeeklyBriefBody? DeserializeBody(WeeklyBrief row)
        {
            try
            {
                return JsonSerializer.Deserialize<WeeklyBriefBody>(
                    row.BodyJson,
                    WeeklyBriefStoreJson.Options
                );
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static AssistantHomeNeedsAttentionOfferFact MapOfferFact(
            int offerId,
            string catalogTitle,
            int claims,
            int redeemed,
            string? expiryDate,
            OpenVoidAttentionOfferDto? openVoid,
            DateTime nowUtc,
            int utcOffsetMinutes
        )
        {
            var hasOpenVoid = openVoid is not null;
            var daysUntilExpiry = hasOpenVoid
                ? null
                : DaysUntilExpiry(expiryDate, nowUtc, utcOffsetMinutes);
            DateTime? metaAt = openVoid?.NewestPendingRequestedAtUtc;
            if (metaAt is null && daysUntilExpiry is int days)
            {
                var expiryKey = expiryDate!.Trim()[..10];
                if (DateTime.TryParse(expiryKey, out var expiry))
                {
                    metaAt = DateTime.SpecifyKind(
                        expiry.Date.AddDays(-ExpiryWindowDays),
                        DateTimeKind.Utc
                    );
                }
            }

            return new AssistantHomeNeedsAttentionOfferFact(
                offerId,
                AssistantHomeNeedsAttention.OfferTitle(
                    hasOpenVoid,
                    daysUntilExpiry,
                    catalogTitle
                ),
                AssistantHomeNeedsAttention.OfferBody(
                    hasOpenVoid,
                    openVoid?.PendingCount ?? 0,
                    daysUntilExpiry,
                    catalogTitle,
                    claims,
                    redeemed
                ),
                "warning",
                metaAt
            );
        }

        private static int? DaysUntilExpiry(
            string? expiryDate,
            DateTime nowUtc,
            int utcOffsetMinutes
        )
        {
            if (string.IsNullOrWhiteSpace(expiryDate))
            {
                return null;
            }

            var expiryKey = expiryDate.Trim().Length >= 10
                ? expiryDate.Trim()[..10]
                : expiryDate.Trim();
            var todayKey = nowUtc
                .AddMinutes(utcOffsetMinutes)
                .ToString("yyyy-MM-dd");
            if (!DateTime.TryParse(expiryKey, out var expiry)
                || !DateTime.TryParse(todayKey, out var today))
            {
                return null;
            }

            return (int)Math.Round((expiry.Date - today.Date).TotalDays);
        }

        private static int LondonOffsetMinutes(DateTime utcNow)
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId
                );
                return (int)zone.GetUtcOffset(utcNow).TotalMinutes;
            }
            catch (TimeZoneNotFoundException)
            {
                return 0;
            }
        }

        private abstract record WeeklyBriefLoad
        {
            public sealed record Ready(WeeklyBriefBody Body) : WeeklyBriefLoad;

            public sealed record Empty() : WeeklyBriefLoad;

            public sealed record Error() : WeeklyBriefLoad;
        }
    }
}
