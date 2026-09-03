using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Reports hub overview aggregate — Area Reports KPI strips + top sources.
    /// </summary>
    public sealed class ReportsOverviewService : IReportsOverviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _capture;

        public ReportsOverviewService(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate capture
        )
        {
            _context = context;
            _capture = capture;
        }

        public async Task<ReportsOverviewDto> GetOverviewAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            if (await IsLifetimeEmptyAsync(locationId, cancellationToken))
            {
                return new ReportsOverviewDto { LifetimeEmpty = true };
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var locationIds = new[] { locationId };
            var activeOrPausedQrIds =
                await _capture.ListActiveOrPausedQrCodeIdsAsync(locationIds);

            var qrScans = await MetricPairAsync(
                () =>
                    _capture.CountScansAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc
                    ),
                () =>
                    _capture.CountScansAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc
                    )
            );

            var feedbackReceived = await MetricPairAsync(
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc,
                        marketingOptInOnly: false
                    ),
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc,
                        marketingOptInOnly: false
                    )
            );

            var marketingOptIns = await MetricPairAsync(
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc,
                        marketingOptInOnly: true
                    ),
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc,
                        marketingOptInOnly: true
                    )
            );

            var feedbackMessages = await MetricPairAsync(
                () => CountFeedbackAtLocationAsync(locationId, fromUtc, toUtc, cancellationToken),
                () =>
                    CountFeedbackAtLocationAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var followUpNeeded = await MetricPairAsync(
                () =>
                    CountNeedsAttentionAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountNeedsAttentionAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var followedUp = await MetricPairAsync(
                () =>
                    CountFollowedUpAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountFollowedUpAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var offerClaims = await MetricPairAsync(
                () => CountOfferClaimsAsync(locationId, fromUtc, toUtc, cancellationToken),
                () =>
                    CountOfferClaimsAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var offerRedemptions = await MetricPairAsync(
                () =>
                    CountOfferRedemptionsAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountOfferRedemptionsAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var campaignsSent = await MetricPairAsync(
                () =>
                    CountCampaignsSentAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountCampaignsSentAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var unsubscribes = await MetricPairAsync(
                () =>
                    CountUnsubscribesAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountUnsubscribesAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var activeOffersCurrent = await _context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    o =>
                        o.RestaurantLocationId == locationId
                        && o.Status == CatalogOfferStatus.Active,
                    cancellationToken
                );

            // No StatusChangedAt — previous = currently Active and created
            // before previous window end (honest without lifecycle history).
            var activeOffersPrevious = await _context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    o =>
                        o.RestaurantLocationId == locationId
                        && o.Status == CatalogOfferStatus.Active
                        && o.CreatedAt < previousToUtc,
                    cancellationToken
                );

            var topCaptureSources = await BuildTopCaptureSourcesAsync(
                locationId,
                activeOrPausedQrIds,
                fromUtc,
                toUtc,
                cancellationToken
            );

            return new ReportsOverviewDto
            {
                LifetimeEmpty = false,
                Funnel = new ReportsOverviewFunnelDto
                {
                    QrScans = qrScans,
                    FeedbackReceived = feedbackReceived,
                    MarketingOptIns = marketingOptIns,
                    OfferRedemptions = offerRedemptions,
                    CampaignsSent = campaignsSent,
                },
                PrivateFeedback = new ReportsOverviewPrivateFeedbackDto
                {
                    FeedbackMessages = feedbackMessages,
                    MarketingOptIns = marketingOptIns,
                    FollowUpNeeded = followUpNeeded,
                    FollowedUp = followedUp,
                },
                OffersAndCampaigns = new ReportsOverviewOffersCampaignsDto
                {
                    ActiveOffers = Metric(activeOffersCurrent, activeOffersPrevious),
                    OfferClaims = offerClaims,
                    OfferRedemptions = offerRedemptions,
                    CampaignsSent = campaignsSent,
                    Unsubscribes = unsubscribes,
                },
                TopCaptureSources = topCaptureSources,
            };
        }

        private async Task<bool> IsLifetimeEmptyAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var hasFeedback = await _context.Feedbacks
                .AsNoTracking()
                .AnyAsync(
                    f => f.RestaurantLocationId == locationId,
                    cancellationToken
                );
            if (hasFeedback)
            {
                return false;
            }

            var hasScan = await _context.QrScanEvents
                .AsNoTracking()
                .AnyAsync(
                    e => e.RestaurantLocationId == locationId,
                    cancellationToken
                );
            if (hasScan)
            {
                return false;
            }

            var hasClaimOrRedemption = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && (
                        i.ClaimedAtUtc != null
                        || (
                            i.RedeemedAtUtc != null
                            && i.RedemptionVoidedAtUtc == null
                        )
                    )
                select i.Id
            ).AnyAsync(cancellationToken);
            if (hasClaimOrRedemption)
            {
                return false;
            }

            var hasCampaignSend = await _context.Campaigns
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.RestaurantLocationId == locationId
                        && c.Status == CampaignLifecycleService.SentStatus,
                    cancellationToken
                );

            return !hasCampaignSend;
        }

        private async Task<
            IReadOnlyList<ReportsOverviewCaptureSourceDto>
        > BuildTopCaptureSourcesAsync(
            int locationId,
            IReadOnlyList<int> activeOrPausedQrIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            if (activeOrPausedQrIds.Count == 0)
            {
                return [];
            }

            var qrRows = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && activeOrPausedQrIds.Contains(q.Id)
                )
                .Select(q => new
                {
                    q.Id,
                    q.QrType,
                    q.LinkName,
                })
                .ToListAsync(cancellationToken);

            var scansByQr = (
                await _capture.GroupScansByQrCodeAsync(
                    activeOrPausedQrIds,
                    fromUtc,
                    toUtc
                )
            ).ToDictionary(r => r.QrCodeId, r => r.Count);

            var feedbackByQr = (
                await _capture.GroupFeedbackByQrCodeAsync(
                    activeOrPausedQrIds,
                    fromUtc,
                    toUtc
                )
            ).ToDictionary(
                r => r.QrCodeId,
                r => (r.FeedbackSubmitted, r.MarketingOptIns)
            );

            return qrRows
                .Select(q =>
                {
                    scansByQr.TryGetValue(q.Id, out var scans);
                    feedbackByQr.TryGetValue(q.Id, out var feedback);
                    return new ReportsOverviewCaptureSourceDto
                    {
                        QrCodeId = q.Id,
                        Source = SourceLabel(q.LinkName, q.QrType),
                        Scans = scans,
                        Feedback = feedback.FeedbackSubmitted,
                        MarketingOptIns = feedback.MarketingOptIns,
                    };
                })
                .Where(row => row.Scans > 0 || row.Feedback > 0)
                .OrderByDescending(row => row.Feedback)
                .ThenByDescending(row => row.Scans)
                .Take(5)
                .ToList();
        }

        private static string SourceLabel(string? linkName, QrType qrType)
        {
            if (!string.IsNullOrWhiteSpace(linkName))
            {
                return linkName.Trim();
            }

            return FeedbackQrSourceMapping.ToDisplay(
                    new QrCode { QrType = qrType, LinkName = null }
                )
                ?? qrType.ToString();
        }

        private Task<int> CountFeedbackAtLocationAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return _context.Feedbacks
                .AsNoTracking()
                .CountAsync(
                    f =>
                        f.RestaurantLocationId == locationId
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc,
                    cancellationToken
                );
        }

        private Task<int> CountNeedsAttentionAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return _context.Feedbacks
                .AsNoTracking()
                .CountAsync(
                    f =>
                        f.RestaurantLocationId == locationId
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc
                        && f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Negative
                        && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved,
                    cancellationToken
                );
        }

        private Task<int> CountFollowedUpAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            // Resolved transitions in window (honest "followed up" grain).
            return (
                from c in _context.FeedbackWorkflowStatusChanges.AsNoTracking()
                join f in _context.Feedbacks.AsNoTracking()
                    on c.FeedbackId equals f.Id
                where
                    f.RestaurantLocationId == locationId
                    && c.ToStatus == FeedbackWorkflowStatus.Resolved
                    && c.CreatedAt >= fromUtc
                    && c.CreatedAt < toUtc
                select c.Id
            ).CountAsync(cancellationToken);
        }

        private Task<int> CountOfferClaimsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc
                select i.Id
            ).CountAsync(cancellationToken);
        }

        private Task<int> CountOfferRedemptionsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc
                select i.Id
            ).CountAsync(cancellationToken);
        }

        private Task<int> CountCampaignsSentAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return _context.Campaigns
                .AsNoTracking()
                .CountAsync(
                    c =>
                        c.RestaurantLocationId == locationId
                        && c.Status == CampaignLifecycleService.SentStatus
                        && c.UpdatedAt >= fromUtc
                        && c.UpdatedAt < toUtc,
                    cancellationToken
                );
        }

        private Task<int> CountUnsubscribesAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return _context.LocationActivities
                .AsNoTracking()
                .CountAsync(
                    a =>
                        a.LocationId == locationId
                        && a.Kind
                            == LocationActivityKinds.GuestMarketingUnsubscribed
                        && a.OccurredAt >= fromUtc
                        && a.OccurredAt < toUtc,
                    cancellationToken
                );
        }

        private static async Task<ReportsMetricDto> MetricPairAsync(
            Func<Task<int>> current,
            Func<Task<int>> previous
        )
        {
            return Metric(await current(), await previous());
        }

        private static ReportsMetricDto Metric(int value, int valuePrevious)
        {
            return new ReportsMetricDto
            {
                Value = value,
                ValuePrevious = valuePrevious,
            };
        }
    }
}
