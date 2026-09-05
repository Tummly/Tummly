using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RestaurantContextSnapshotService : IRestaurantContextSnapshotService
    {
        private static readonly string[] ActiveCampaignStatuses =
        [
            CampaignLifecycleService.ScheduledStatus,
            CampaignLifecycleService.SendingStatus,
            CampaignLifecycleService.PausedStatus,
        ];

        private static readonly string[] RecentlyEndedCampaignStatuses =
        [
            CampaignLifecycleService.SentStatus,
            CampaignLifecycleService.PartiallySentStatus,
            CampaignLifecycleService.FailedStatus,
            CampaignLifecycleService.CancelledStatus,
        ];

        private readonly IAssistantFeedbackRetrieve _feedbackRetrieve;
        private readonly IAssistantOffersRetrieve _offersRetrieve;
        private readonly IAssistantCampaignsRetrieve _campaignsRetrieve;
        private readonly IAssistantCaptureRetrieve _captureRetrieve;
        private readonly IAssistantHomeKpiRetrieve _homeRetrieve;
        private readonly IAssistantGuestsRetrieve _guestsRetrieve;
        private readonly ApplicationDbContext _context;
        private readonly RestaurantContextSnapshotSettings _settings;
        private readonly TimeProvider _clock;
        private readonly IMemoryCache _cache;

        public RestaurantContextSnapshotService(
            IAssistantFeedbackRetrieve feedbackRetrieve,
            IAssistantOffersRetrieve offersRetrieve,
            IAssistantCampaignsRetrieve campaignsRetrieve,
            IAssistantCaptureRetrieve captureRetrieve,
            IAssistantHomeKpiRetrieve homeRetrieve,
            IAssistantGuestsRetrieve guestsRetrieve,
            ApplicationDbContext context,
            IOptions<RestaurantContextSnapshotSettings> settings,
            TimeProvider clock,
            IMemoryCache cache
        )
        {
            _feedbackRetrieve = feedbackRetrieve;
            _offersRetrieve = offersRetrieve;
            _campaignsRetrieve = campaignsRetrieve;
            _captureRetrieve = captureRetrieve;
            _homeRetrieve = homeRetrieve;
            _guestsRetrieve = guestsRetrieve;
            _context = context;
            _settings = settings.Value;
            _clock = clock;
            _cache = cache;
        }

        public async Task<RestaurantContextSnapshot> BuildAsync(
            int ownerUserId,
            LocationScope scope,
            PeriodWindow? currentOverride,
            PeriodWindow? comparisonOverride,
            CancellationToken cancellationToken = default
        )
        {
            var utcNow = _clock.GetUtcNow().UtcDateTime;
            var (current, comparison) = ResolveWindows(
                utcNow,
                currentOverride,
                comparisonOverride
            );
            var locationIds = ResolveLocationIds(scope);
            var cacheKey = BuildCacheKey(ownerUserId, scope, current, comparison);
            if (_cache.TryGetValue(cacheKey, out RestaurantContextSnapshot? cached)
                && cached is not null)
            {
                return cached;
            }

            var snapshot = await ComposeAsync(
                ownerUserId,
                scope,
                locationIds,
                current,
                comparison,
                utcNow,
                cancellationToken
            );
            _cache.Set(
                cacheKey,
                snapshot,
                TimeSpan.FromSeconds(Math.Max(1, _settings.CacheTtlSeconds))
            );
            return snapshot;
        }

        private async Task<RestaurantContextSnapshot> ComposeAsync(
            int ownerUserId,
            LocationScope scope,
            IReadOnlyList<int> locationIds,
            PeriodWindow current,
            PeriodWindow comparison,
            DateTime utcNow,
            CancellationToken cancellationToken
        )
        {
            var currentRange = ToUtcRange(current);
            var comparisonRange = ToUtcRange(comparison);
            var priorComparison = new PeriodWindow(
                comparison.Start.AddDays(-(comparison.End.DayNumber - comparison.Start.DayNumber)),
                comparison.Start.AddDays(-1)
            );
            var priorComparisonRange = ToUtcRange(priorComparison);

            var currentHome = await SumHomeAsync(
                locationIds,
                currentRange.FromUtc,
                currentRange.ToUtc,
                cancellationToken
            );
            var comparisonHome = await SumHomeAsync(
                locationIds,
                comparisonRange.FromUtc,
                comparisonRange.ToUtc,
                cancellationToken
            );
            var priorHome = await SumHomeAsync(
                locationIds,
                priorComparisonRange.FromUtc,
                priorComparisonRange.ToUtc,
                cancellationToken
            );

            var currentCampaigns = await MergeCampaignsAsync(
                locationIds,
                currentRange.FromUtc,
                currentRange.ToUtc,
                cancellationToken
            );
            var comparisonCampaigns = await MergeCampaignsAsync(
                locationIds,
                comparisonRange.FromUtc,
                comparisonRange.ToUtc,
                cancellationToken
            );
            var currentOffers = await MergeOffersAsync(
                locationIds,
                currentRange.FromUtc,
                currentRange.ToUtc,
                cancellationToken
            );
            var comparisonOffers = await MergeOffersAsync(
                locationIds,
                comparisonRange.FromUtc,
                comparisonRange.ToUtc,
                cancellationToken
            );
            var currentFeedback = await MergeFeedbackAsync(
                locationIds,
                currentRange.FromUtc,
                currentRange.ToUtc,
                cancellationToken
            );
            var comparisonFeedback = await MergeFeedbackAsync(
                locationIds,
                comparisonRange.FromUtc,
                comparisonRange.ToUtc,
                cancellationToken
            );
            var currentCapture = await MergeCaptureAsync(
                locationIds,
                currentRange.FromUtc,
                currentRange.ToUtc,
                cancellationToken
            );
            var comparisonCapture = await MergeCaptureAsync(
                locationIds,
                comparisonRange.FromUtc,
                comparisonRange.ToUtc,
                cancellationToken
            );
            _ = await MergeGuestsAsync(locationIds, cancellationToken);

            var historyDays = await ComputeHistoryDaysAsync(locationIds, utcNow, cancellationToken);
            var allowTrends = historyDays >= _settings.MinDaysForTrendClaim;
            var isNewAccount = historyDays < _settings.NewAccountHistoryDays;

            var locationNames = await LoadLocationNamesAsync(locationIds, cancellationToken);
            var coversByLocation = await LoadCoversByLocationAsync(
                locationIds,
                locationNames,
                currentRange.FromUtc,
                currentRange.ToUtc,
                comparisonRange.FromUtc,
                comparisonRange.ToUtc,
                allowTrends,
                cancellationToken
            );

            var insufficient = new List<string>();
            var account = BuildAccount(
                currentHome,
                comparisonHome,
                priorHome,
                coversByLocation,
                allowTrends,
                insufficient
            );
            var campaigns = BuildCampaigns(
                currentCampaigns,
                comparisonCampaigns,
                allowTrends,
                insufficient
            );
            var offers = BuildOffers(
                currentOffers,
                comparisonOffers,
                utcNow,
                allowTrends,
                insufficient
            );
            var feedback = BuildFeedback(
                currentFeedback,
                comparisonFeedback,
                campaigns.Active,
                allowTrends,
                insufficient
            );
            var guests = BuildGuests(currentHome, comparisonHome, allowTrends, insufficient);
            var capture = BuildCapture(
                currentCapture,
                comparisonCapture,
                allowTrends,
                insufficient
            );
            var recentActions = await LoadRecentActionsAsync(
                ownerUserId,
                utcNow,
                cancellationToken
            );

            return new RestaurantContextSnapshot(
                _settings.SchemaVersion,
                scope,
                current,
                comparison,
                account,
                campaigns,
                offers,
                feedback,
                guests,
                capture,
                recentActions,
                new SnapshotMeta(
                    isNewAccount,
                    historyDays,
                    insufficient.Distinct(StringComparer.Ordinal).ToArray()
                )
            );
        }

        private AccountSection BuildAccount(
            AssistantHomeKpiEvidence current,
            AssistantHomeKpiEvidence comparison,
            AssistantHomeKpiEvidence prior,
            List<LocationMetricPoint> coversByLocation,
            bool allowTrends,
            List<string> insufficient
        )
        {
            var flags = new List<Flag>();
            var covers = Metric(
                current.GuestsJoined,
                comparison.GuestsJoined,
                allowTrends
            );
            // Revenue / avg ticket / true repeat rate are not in Evidence yet.
            var revenue = UnsupportedMetric();
            var avgTicket = UnsupportedMetric();
            var repeat = covers;
            if (current.IsEmpty && comparison.IsEmpty)
            {
                insufficient.Add("Account");
            }

            if (coversByLocation.Count >= 2)
            {
                var rates = coversByLocation
                    .Select(row => PctChange(row.Metric.Current, row.Metric.Prior))
                    .Where(rate => rate is not null)
                    .Select(rate => rate!.Value)
                    .ToList();
                if (rates.Count >= 2)
                {
                    var spread = rates.Max() - rates.Min();
                    if (spread >= _settings.LocationDivergenceThresholdPts)
                    {
                        flags.Add(
                            new Flag(
                                "REPEAT_RATE_DIVERGENT_BY_LOCATION",
                                "Guest-join change diverges across venues.",
                                FlagSeverity.Notable,
                                coversByLocation
                                    .Select(row => row.LocationId)
                                    .ToArray()
                            )
                        );
                    }
                }
            }

            if (allowTrends
                && HasReversedTrend(
                    covers.Current,
                    covers.Prior,
                    prior.GuestsJoined
                ))
            {
                flags.Add(
                    new Flag(
                        "METRIC_REVERSED_TREND",
                        "Guest-join trend reversed versus the prior comparison window.",
                        FlagSeverity.Notable,
                        ["Account.Covers"]
                    )
                );
            }

            return new AccountSection(
                covers,
                revenue,
                avgTicket,
                repeat,
                flags,
                coversByLocation.Count > 1 ? coversByLocation : null
            );
        }

        private CampaignsSection BuildCampaigns(
            AssistantCampaignsEvidence current,
            AssistantCampaignsEvidence comparison,
            bool allowTrends,
            List<string> insufficient
        )
        {
            var flags = new List<Flag>();
            if (current.IsEmpty && comparison.IsEmpty)
            {
                insufficient.Add("Campaigns");
            }

            var active = current.Rows
                .Where(row => ActiveCampaignStatuses.Contains(row.Status, StringComparer.Ordinal))
                .Select(row => ToCampaignSummary(row, current, comparison, allowTrends))
                .ToList();
            var recentlyEnded = current.Rows
                .Where(row =>
                    RecentlyEndedCampaignStatuses.Contains(row.Status, StringComparer.Ordinal)
                )
                .Select(row => ToCampaignSummary(row, current, comparison, allowTrends))
                .ToList();

            if (active.Count == 0)
            {
                flags.Add(
                    new Flag(
                        "NO_ACTIVE_CAMPAIGN",
                        "No in-flight campaign is active.",
                        FlagSeverity.Notable,
                        ["Campaigns.Active"]
                    )
                );
            }

            foreach (var campaign in active)
            {
                if (campaign.Engagement.Current
                        < _settings.CampaignUnderperformingThresholdPct
                    && campaign.Engagement.Prior is not null)
                {
                    flags.Add(
                        new Flag(
                            "CAMPAIGN_UNDERPERFORMING",
                            $"{campaign.Name} engagement is under the threshold.",
                            FlagSeverity.Notable,
                            [campaign.Id]
                        )
                    );
                }
            }

            return new CampaignsSection(active, recentlyEnded, flags);
        }

        private OffersSection BuildOffers(
            AssistantOffersEvidence current,
            AssistantOffersEvidence comparison,
            DateTime utcNow,
            bool allowTrends,
            List<string> insufficient
        )
        {
            var flags = new List<Flag>();
            if (current.IsEmpty && comparison.IsEmpty)
            {
                insufficient.Add("Offers");
            }

            var metricsById = current.PerOfferMetrics
                .ToDictionary(row => row.OfferId, row => row);
            var comparisonMetrics = comparison.PerOfferMetrics
                .ToDictionary(row => row.OfferId, row => row);
            var linked = current.LinkedCampaigns
                .GroupBy(row => row.OfferId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );

            var active = current.Catalog
                .Where(row =>
                    string.Equals(
                        row.Status,
                        CatalogOfferStatus.Active,
                        StringComparison.Ordinal
                    )
                )
                .Select(row =>
                    ToOfferSummary(
                        row,
                        metricsById,
                        comparisonMetrics,
                        linked,
                        allowTrends
                    )
                )
                .ToList();

            var today = DateOnly.FromDateTime(utcNow);
            var expiringUnused = active
                .Where(offer =>
                    offer.EndsAt is DateOnly ends
                    && ends <= today.AddDays(_settings.OfferExpiringWindowDays)
                    && offer.RedemptionRate.Current == 0m
                )
                .ToList();

            foreach (var offer in active)
            {
                if (offer.RedemptionRate.Current >= 50m
                    && offer.EndsAt is not null
                    && !offer.HasSuccessorScheduled)
                {
                    flags.Add(
                        new Flag(
                            "HIGH_PERFORMER_NOT_EXTENDED",
                            $"{offer.Name} is performing and has no scheduled successor.",
                            FlagSeverity.Info,
                            [offer.Id]
                        )
                    );
                }
            }

            return new OffersSection(active, expiringUnused, flags);
        }

        private FeedbackSection BuildFeedback(
            AssistantFeedbackEvidence current,
            AssistantFeedbackEvidence comparison,
            List<CampaignSummary> activeCampaigns,
            bool allowTrends,
            List<string> insufficient
        )
        {
            var flags = new List<Flag>();
            if (current.IsEmpty && comparison.IsEmpty)
            {
                insufficient.Add("Feedback");
            }

            var sentiment = Metric(
                SentimentMix(current),
                SentimentMix(comparison),
                allowTrends
            );
            var flagged = current.Rows
                .Where(row => row.NeedsAttention)
                .OrderByDescending(row => row.CreatedAt)
                .Take(Math.Max(1, _settings.FlaggedFeedbackCap))
                .Select(row => new FlaggedFeedbackItem(
                    row.Id.ToString(CultureInfo.InvariantCulture),
                    DateOnly.FromDateTime(row.CreatedAt),
                    Truncate(row.Excerpt, 120),
                    RecoverySent: false
                ))
                .ToList();
            var themes = current.TagCounts
                .OrderByDescending(tag => tag.Count)
                .Take(5)
                .Select(tag =>
                {
                    var onset = current.Rows
                        .Where(row => row.DetectedTags.Contains(tag.Tag, StringComparer.OrdinalIgnoreCase))
                        .Select(row => DateOnly.FromDateTime(row.CreatedAt))
                        .DefaultIfEmpty()
                        .Min();
                    return new RecurringThemeSummary(
                        tag.Tag,
                        tag.Count,
                        onset == default ? null : onset
                    );
                })
                .ToList();

            foreach (var theme in themes)
            {
                if (theme.OnsetDate is null || activeCampaigns.Count == 0)
                {
                    continue;
                }

                var nearCampaign = activeCampaigns.Any(campaign =>
                    Math.Abs(
                        theme.OnsetDate.Value.DayNumber - campaign.StartedAt.DayNumber
                    ) <= 7
                );
                if (nearCampaign)
                {
                    flags.Add(
                        new Flag(
                            "THEME_CORRELATES_WITH_CHANGE",
                            $"Theme {theme.Theme} began near a campaign start.",
                            FlagSeverity.Info,
                            [theme.Theme]
                        )
                    );
                }
            }

            var unresolved = current.Rows.Count(row =>
                row.NeedsAttention
                && !string.Equals(row.WorkflowStatus, "Resolved", StringComparison.OrdinalIgnoreCase)
            );

            return new FeedbackSection(
                sentiment,
                flagged,
                themes,
                unresolved,
                flags
            );
        }

        private static GuestsSection BuildGuests(
            AssistantHomeKpiEvidence current,
            AssistantHomeKpiEvidence comparison,
            bool allowTrends,
            List<string> insufficient
        )
        {
            if (current.GuestsJoined == 0 && comparison.GuestsJoined == 0)
            {
                insufficient.Add("Guests");
            }

            return new GuestsSection(
                Metric(current.GuestsJoined, comparison.GuestsJoined, allowTrends),
                UnsupportedMetric(),
                UnsupportedMetric(),
                []
            );
        }

        private static CaptureSection BuildCapture(
            AssistantCaptureEvidence current,
            AssistantCaptureEvidence comparison,
            bool allowTrends,
            List<string> insufficient
        )
        {
            if (!current.HasSnapshotFacts && !comparison.HasSnapshotFacts)
            {
                insufficient.Add("Capture");
            }

            var start = Metric(current.QrScans, comparison.QrScans, allowTrends);
            var complete = Metric(
                current.FeedbackSubmitted,
                comparison.FeedbackSubmitted,
                allowTrends
            );
            decimal? dropCurrent = current.QrScans == 0
                ? null
                : RoundPct(
                    (current.QrScans - current.FeedbackSubmitted)
                        / (decimal)current.QrScans
                        * 100m
                );
            decimal? dropPrior = comparison.QrScans == 0
                ? null
                : RoundPct(
                    (comparison.QrScans - comparison.FeedbackSubmitted)
                        / (decimal)comparison.QrScans
                        * 100m
                );
            var drop = new MetricPoint(
                dropCurrent ?? 0m,
                dropPrior,
                allowTrends ? PctDelta(dropCurrent ?? 0m, dropPrior) : null
            );
            string? stage = null;
            if (current.QrScans > 0
                && current.FeedbackSubmitted * 2 < current.QrScans)
            {
                stage = "scan-to-feedback";
            }

            return new CaptureSection(start, complete, drop, stage);
        }

        private async Task<RecentActionsSection> LoadRecentActionsAsync(
            int ownerUserId,
            DateTime utcNow,
            CancellationToken cancellationToken
        )
        {
            var fromUtc = utcNow.AddDays(-30);
            var rows = await _context.AssistantMessages
                .AsNoTracking()
                .Where(message =>
                    message.Conversation.OwnerUserId == ownerUserId
                    && message.Role == AssistantMessageRole.Assistant
                    && message.CreatedAt >= fromUtc
                    && (
                        message.ActionsJson != null
                        || message.Class == AssistantMessageClass.Gap
                        || message.Class == AssistantMessageClass.Grounded
                    )
                )
                .OrderByDescending(message => message.CreatedAt)
                .Take(30)
                .Select(message => new
                {
                    message.CreatedAt,
                    message.Class,
                    message.Title,
                    message.Body,
                    message.ActionsJson,
                })
                .ToListAsync(cancellationToken);

            var actions = rows
                .Select(row => new PriorAssistantAction(
                    DateOnly.FromDateTime(row.CreatedAt),
                    ActionType: row.Class?.ToWireString()
                        ?? (row.ActionsJson is null ? "grounded" : "actions"),
                    ShortDescription: Truncate(
                        string.IsNullOrWhiteSpace(row.Title) ? row.Body : row.Title!,
                        160
                    )
                ))
                .ToList();
            return new RecentActionsSection(actions);
        }

        private async Task<int> ComputeHistoryDaysAsync(
            IReadOnlyList<int> locationIds,
            DateTime utcNow,
            CancellationToken cancellationToken
        )
        {
            if (locationIds.Count == 0)
            {
                return 0;
            }

            DateTime? earliest = null;
            var feedbackEarliest = await _context.Feedbacks
                .AsNoTracking()
                .Where(row => locationIds.Contains(row.RestaurantLocationId))
                .Select(row => (DateTime?)row.CreatedAt)
                .MinAsync(cancellationToken);
            var campaignEarliest = await _context.Campaigns
                .AsNoTracking()
                .Where(row => locationIds.Contains(row.RestaurantLocationId))
                .Select(row => (DateTime?)row.CreatedAt)
                .MinAsync(cancellationToken);
            var offerEarliest = await _context.CatalogOffers
                .AsNoTracking()
                .Where(row => locationIds.Contains(row.RestaurantLocationId))
                .Select(row => (DateTime?)row.CreatedAt)
                .MinAsync(cancellationToken);

            foreach (var candidate in new[] { feedbackEarliest, campaignEarliest, offerEarliest })
            {
                if (candidate is DateTime value
                    && (earliest is null || value < earliest))
                {
                    earliest = value;
                }
            }

            if (earliest is null)
            {
                return 0;
            }

            return Math.Max(0, (int)(utcNow.Date - earliest.Value.Date).TotalDays);
        }

        private async Task<Dictionary<int, string>> LoadLocationNamesAsync(
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken
        )
        {
            if (locationIds.Count == 0)
            {
                return [];
            }

            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => locationIds.Contains(row.Id))
                .ToDictionaryAsync(
                    row => row.Id,
                    row => row.LocationName,
                    cancellationToken
                );
        }

        private async Task<List<LocationMetricPoint>> LoadCoversByLocationAsync(
            IReadOnlyList<int> locationIds,
            IReadOnlyDictionary<int, string> names,
            DateTime currentFrom,
            DateTime currentTo,
            DateTime comparisonFrom,
            DateTime comparisonTo,
            bool allowTrends,
            CancellationToken cancellationToken
        )
        {
            var points = new List<LocationMetricPoint>();
            foreach (var locationId in locationIds)
            {
                var current = await RetrieveHomeAsync(
                    locationId,
                    currentFrom,
                    currentTo,
                    cancellationToken
                );
                var comparison = await RetrieveHomeAsync(
                    locationId,
                    comparisonFrom,
                    comparisonTo,
                    cancellationToken
                );
                names.TryGetValue(locationId, out var name);
                points.Add(
                    new LocationMetricPoint(
                        locationId.ToString(CultureInfo.InvariantCulture),
                        name ?? locationId.ToString(CultureInfo.InvariantCulture),
                        Metric(current.GuestsJoined, comparison.GuestsJoined, allowTrends)
                    )
                );
            }

            return points;
        }

        private async Task<AssistantHomeKpiEvidence> SumHomeAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var feedback = 0;
            var feedbackPrevious = 0;
            var guests = 0;
            var guestsPrevious = 0;
            var scans = 0;
            var scansPrevious = 0;
            foreach (var locationId in locationIds)
            {
                var evidence = await RetrieveHomeAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                feedback += evidence.FeedbackSubmitted;
                feedbackPrevious += evidence.FeedbackSubmittedPrevious;
                guests += evidence.GuestsJoined;
                guestsPrevious += evidence.GuestsJoinedPrevious;
                scans += evidence.QrScans;
                scansPrevious += evidence.QrScansPrevious;
            }

            return new AssistantHomeKpiEvidence(
                feedback,
                feedbackPrevious,
                guests,
                guestsPrevious,
                scans,
                scansPrevious
            );
        }

        private async Task<AssistantHomeKpiEvidence> RetrieveHomeAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var result = await _homeRetrieve.RetrieveAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            return result is AssistantHomeKpiRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantHomeKpiEvidence.Empty;
        }

        private async Task<AssistantCampaignsEvidence> MergeCampaignsAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var rows = new List<AssistantCampaignListRow>();
            var eligibility = new List<AssistantCampaignEligibilityRow>();
            var details = new List<AssistantCampaignDetailRow>();
            var scheduled = 0;
            var sending = 0;
            var sent = 0;
            var total = 0;
            foreach (var locationId in locationIds)
            {
                var result = await _campaignsRetrieve.RetrieveAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    includeMessageCopy: false,
                    cancellationToken
                );
                if (result is not AssistantCampaignsRetrieveResult.Ok ok)
                {
                    continue;
                }

                total += ok.Evidence.ListTotalCount;
                scheduled += ok.Evidence.InFlightScheduled;
                sending += ok.Evidence.InFlightSending;
                sent += ok.Evidence.MessagesSentAccepted;
                rows.AddRange(ok.Evidence.Rows);
                eligibility.AddRange(ok.Evidence.Eligibility);
                details.AddRange(ok.Evidence.Details);
            }

            return new AssistantCampaignsEvidence(
                total,
                rows.Count,
                scheduled,
                sending,
                sent,
                rows,
                eligibility,
                details
            );
        }

        private async Task<AssistantOffersEvidence> MergeOffersAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var catalog = new List<AssistantOfferCatalogRow>();
            var metrics = new List<AssistantOfferMetricsRow>();
            var linked = new List<AssistantOfferLinkedCampaignRow>();
            var claims = new List<AssistantOfferLogRow>();
            var redemptions = new List<AssistantOfferLogRow>();
            var catalogTotal = 0;
            var active = 0;
            var issued = 0;
            var claimCount = 0;
            var redemptionCount = 0;
            foreach (var locationId in locationIds)
            {
                var result = await _offersRetrieve.RetrieveAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                if (result is not AssistantOffersRetrieveResult.Ok ok)
                {
                    continue;
                }

                catalogTotal += ok.Evidence.CatalogTotalCount;
                active += ok.Evidence.ActiveOffers;
                issued += ok.Evidence.OffersIssued;
                claimCount += ok.Evidence.Claims;
                redemptionCount += ok.Evidence.Redemptions;
                catalog.AddRange(ok.Evidence.Catalog);
                metrics.AddRange(ok.Evidence.PerOfferMetrics);
                linked.AddRange(ok.Evidence.LinkedCampaigns);
                claims.AddRange(ok.Evidence.ClaimLogs);
                redemptions.AddRange(ok.Evidence.RedemptionLogs);
            }

            double? rate = claimCount == 0
                ? null
                : redemptionCount / (double)claimCount;
            return new AssistantOffersEvidence(
                catalogTotal,
                catalog.Count,
                active,
                issued,
                claimCount,
                redemptionCount,
                rate,
                catalog,
                metrics,
                linked,
                claims,
                redemptions
            );
        }

        private async Task<AssistantFeedbackEvidence> MergeFeedbackAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var rows = new List<AssistantFeedbackEvidenceRow>();
            var tagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var total = 0;
            var positive = 0;
            var neutral = 0;
            var negative = 0;
            var needsAttention = 0;
            foreach (var locationId in locationIds)
            {
                var result = await _feedbackRetrieve.RetrieveAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                if (result is not AssistantFeedbackRetrieveResult.Ok ok)
                {
                    continue;
                }

                total += ok.Evidence.TotalCount;
                positive += ok.Evidence.SucceededPositive;
                neutral += ok.Evidence.SucceededNeutral;
                negative += ok.Evidence.SucceededNegative;
                needsAttention += ok.Evidence.NeedsAttention;
                rows.AddRange(ok.Evidence.Rows);
                foreach (var tag in ok.Evidence.TagCounts)
                {
                    tagCounts[tag.Tag] = tagCounts.GetValueOrDefault(tag.Tag) + tag.Count;
                }
            }

            return new AssistantFeedbackEvidence(
                total,
                rows.Count,
                positive,
                neutral,
                negative,
                needsAttention,
                tagCounts
                    .Select(pair => new AssistantFeedbackTagCount(pair.Key, pair.Value))
                    .ToList(),
                rows,
                [],
                [],
                []
            );
        }

        private async Task<AssistantCaptureEvidence> MergeCaptureAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var qrRows = new List<AssistantCaptureQrRow>();
            var scans = 0;
            var scansPrevious = 0;
            var feedback = 0;
            var feedbackPrevious = 0;
            var optIns = 0;
            var optInsPrevious = 0;
            foreach (var locationId in locationIds)
            {
                var result = await _captureRetrieve.RetrieveAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                if (result is not AssistantCaptureRetrieveResult.Ok ok)
                {
                    continue;
                }

                scans += ok.Evidence.QrScans;
                scansPrevious += ok.Evidence.QrScansPrevious;
                feedback += ok.Evidence.FeedbackSubmitted;
                feedbackPrevious += ok.Evidence.FeedbackSubmittedPrevious;
                optIns += ok.Evidence.MarketingOptIns;
                optInsPrevious += ok.Evidence.MarketingOptInsPrevious;
                qrRows.AddRange(ok.Evidence.QrRows);
            }

            return new AssistantCaptureEvidence(
                scans,
                scansPrevious,
                feedback,
                feedbackPrevious,
                optIns,
                optInsPrevious,
                qrRows
            );
        }

        private async Task<AssistantGuestsEvidence> MergeGuestsAsync(
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken
        )
        {
            var rows = new List<AssistantGuestEvidenceRow>();
            var total = 0;
            foreach (var locationId in locationIds)
            {
                var result = await _guestsRetrieve.RetrieveAsync(
                    locationId,
                    cancellationToken
                );
                if (result is not AssistantGuestsRetrieveResult.Ok ok)
                {
                    continue;
                }

                total += ok.Evidence.TotalCount;
                rows.AddRange(ok.Evidence.Rows);
            }

            return new AssistantGuestsEvidence(total, rows.Count, rows, []);
        }

        private CampaignSummary ToCampaignSummary(
            AssistantCampaignListRow row,
            AssistantCampaignsEvidence current,
            AssistantCampaignsEvidence comparison,
            bool allowTrends
        )
        {
            var currentElig = current.Eligibility
                .FirstOrDefault(item => item.CampaignId == row.Id);
            var comparisonElig = comparison.Eligibility
                .FirstOrDefault(item => item.CampaignId == row.Id);
            var engagementCurrent = EngagementPct(currentElig);
            var engagementPrior = EngagementPct(comparisonElig);
            return new CampaignSummary(
                row.Id.ToString(CultureInfo.InvariantCulture),
                row.Name,
                row.Status,
                UnsupportedMetric(),
                Metric(engagementCurrent, engagementPrior, allowTrends),
                DateOnly.FromDateTime(row.CreatedAt),
                EndsAt: null
            );
        }

        private static OfferSummary ToOfferSummary(
            AssistantOfferCatalogRow row,
            IReadOnlyDictionary<int, AssistantOfferMetricsRow> currentMetrics,
            IReadOnlyDictionary<int, AssistantOfferMetricsRow> comparisonMetrics,
            IReadOnlyDictionary<int, List<AssistantOfferLinkedCampaignRow>> linked,
            bool allowTrends
        )
        {
            currentMetrics.TryGetValue(row.Id, out var current);
            comparisonMetrics.TryGetValue(row.Id, out var prior);
            var redemption = Metric(
                (decimal)(current?.RedemptionRate ?? 0d) * 100m,
                prior?.RedemptionRate is double priorRate
                    ? (decimal)priorRate * 100m
                    : null,
                allowTrends
            );
            var hasSuccessor = linked.TryGetValue(row.Id, out var campaigns)
                && campaigns.Any(campaign =>
                    string.Equals(
                        campaign.Status,
                        CampaignLifecycleService.ScheduledStatus,
                        StringComparison.Ordinal
                    )
                );
            return new OfferSummary(
                row.Id.ToString(CultureInfo.InvariantCulture),
                row.Title,
                row.Status,
                redemption,
                EndsAt: null,
                hasSuccessor
            );
        }

        private static decimal EngagementPct(AssistantCampaignEligibilityRow? row)
        {
            if (row?.Matched is not int matched || matched <= 0)
            {
                return 0m;
            }

            var eligible = row.CurrentlyEligible ?? 0;
            return RoundPct(eligible / (decimal)matched * 100m) ?? 0m;
        }

        private static decimal SentimentMix(AssistantFeedbackEvidence evidence)
        {
            var scored = evidence.SucceededPositive
                + evidence.SucceededNeutral
                + evidence.SucceededNegative;
            if (scored == 0)
            {
                return 0m;
            }

            // Map mix to 0-100 where positive=100, neutral=50, negative=0.
            var score =
                (evidence.SucceededPositive * 100m)
                + (evidence.SucceededNeutral * 50m);
            return RoundPct(score / scored) ?? 0m;
        }

        private static MetricPoint Metric(
            decimal current,
            decimal? prior,
            bool allowTrends
        )
            => new(
                current,
                prior,
                allowTrends ? PctDelta(current, prior) : null
            );

        private static MetricPoint UnsupportedMetric()
            => new(0m, null, null);

        private static decimal? PctDelta(decimal current, decimal? prior)
        {
            if (prior is null || prior == 0m)
            {
                return null;
            }

            return RoundPct((current - prior.Value) / prior.Value * 100m);
        }

        private static decimal? PctChange(decimal current, decimal? prior)
            => PctDelta(current, prior);

        private static decimal? RoundPct(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static bool HasReversedTrend(
            decimal current,
            decimal? prior,
            decimal priorPrior
        )
        {
            if (prior is null || prior == 0m || priorPrior == 0m)
            {
                return false;
            }

            var recent = current - prior.Value;
            var earlier = prior.Value - priorPrior;
            return recent != 0m
                && earlier != 0m
                && Math.Sign(recent) != Math.Sign(earlier);
        }

        private static (PeriodWindow Current, PeriodWindow Comparison) ResolveWindows(
            DateTime utcNow,
            PeriodWindow? currentOverride,
            PeriodWindow? comparisonOverride
        )
        {
            var today = DateOnly.FromDateTime(utcNow);
            var current = currentOverride
                ?? new PeriodWindow(today.AddDays(-29), today);
            var comparison = comparisonOverride
                ?? new PeriodWindow(
                    current.Start.AddDays(-30),
                    current.Start.AddDays(-1)
                );
            return (current, comparison);
        }

        private static (DateTime FromUtc, DateTime ToUtc) ToUtcRange(PeriodWindow window)
        {
            var fromUtc = new DateTime(
                window.Start.Year,
                window.Start.Month,
                window.Start.Day,
                0,
                0,
                0,
                DateTimeKind.Utc
            );
            var toUtc = new DateTime(
                window.End.Year,
                window.End.Month,
                window.End.Day,
                0,
                0,
                0,
                DateTimeKind.Utc
            ).AddDays(1);
            return (fromUtc, toUtc);
        }

        private static IReadOnlyList<int> ResolveLocationIds(LocationScope scope)
            => scope switch
            {
                SingleLocation single => ParseIds([single.LocationId]),
                AllOwnedLocations all => ParseIds(all.LocationIds),
                NamedSubset named => ParseIds(named.LocationIds),
                _ => [],
            };

        private static IReadOnlyList<int> ParseIds(IEnumerable<string> ids)
            => ids
                .Select(id =>
                    int.TryParse(
                        id,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value
                    )
                        ? value
                        : (int?)null
                )
                .Where(id => id is not null)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

        private static string BuildCacheKey(
            int ownerUserId,
            LocationScope scope,
            PeriodWindow current,
            PeriodWindow comparison
        )
        {
            var ids = string.Join(',', ResolveLocationIds(scope));
            var scopeKind = scope switch
            {
                SingleLocation => "single",
                AllOwnedLocations => "all",
                NamedSubset => "subset",
                _ => "unknown",
            };
            return string.Create(
                CultureInfo.InvariantCulture,
                $"rcs:{ownerUserId}:{scopeKind}:{ids}:{current.Start:yyyyMMdd}:{current.End:yyyyMMdd}:{comparison.Start:yyyyMMdd}:{comparison.End:yyyyMMdd}"
            );
        }

        private static string Truncate(string value, int max)
        {
            var trimmed = value.Trim();
            if (trimmed.Length <= max)
            {
                return trimmed;
            }

            return trimmed[..max];
        }
    }
}
