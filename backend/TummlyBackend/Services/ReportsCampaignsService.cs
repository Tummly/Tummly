using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaigns report aggregate — send/delivery KPIs, completed-in-window
    /// performance, live failed/partially-sent attention.
    /// </summary>
    public sealed class ReportsCampaignsService : IReportsCampaignsService
    {
        private readonly ApplicationDbContext _context;

        public ReportsCampaignsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportsCampaignsDto> GetCampaignsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            if (await IsLifetimeEmptyAsync(locationId, cancellationToken))
            {
                return new ReportsCampaignsDto { LifetimeEmpty = true };
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

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

            var guestsMessaged = await MetricPairAsync(
                () =>
                    CampaignAcceptedMessageCounts.CountAcceptedAsync(
                        _context,
                        fromUtc,
                        toUtc,
                        locationId,
                        channel: null,
                        cancellationToken
                    ),
                () =>
                    CampaignAcceptedMessageCounts.CountAcceptedAsync(
                        _context,
                        previousFromUtc,
                        previousToUtc,
                        locationId,
                        channel: null,
                        cancellationToken
                    )
            );

            var failedSends = await MetricPairAsync(
                () =>
                    CountFailedSendsAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountFailedSendsAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var offerClaims = await MetricPairAsync(
                () =>
                    CountOfferClaimsAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
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

            var performance = await ListPerformanceAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var needsAttention = await ListNeedsAttentionAsync(
                locationId,
                cancellationToken
            );

            return new ReportsCampaignsDto
            {
                LifetimeEmpty = false,
                CampaignsSent = campaignsSent,
                GuestsMessaged = guestsMessaged,
                FailedSends = failedSends,
                OfferClaims = offerClaims,
                OfferRedemptions = offerRedemptions,
                Unsubscribes = unsubscribes,
                Performance = performance,
                NeedsAttention = needsAttention,
            };
        }

        private async Task<bool> IsLifetimeEmptyAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var hasTerminalSend = await _context.Campaigns
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.RestaurantLocationId == locationId
                        && (
                            c.Status == CampaignLifecycleService.SentStatus
                            || c.Status
                                == CampaignLifecycleService.PartiallySentStatus
                            || c.Status == CampaignLifecycleService.FailedStatus
                        ),
                    cancellationToken
                );

            return !hasTerminalSend;
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
                        && (
                            c.Status == CampaignLifecycleService.SentStatus
                            || c.Status
                                == CampaignLifecycleService.PartiallySentStatus
                            || c.Status == CampaignLifecycleService.FailedStatus
                        )
                        && c.UpdatedAt >= fromUtc
                        && c.UpdatedAt < toUtc,
                    cancellationToken
                );
        }

        private Task<int> CountFailedSendsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return (
                from row in _context.CampaignRecipientDeliveries.AsNoTracking()
                join campaign in _context.Campaigns.AsNoTracking()
                    on row.CampaignId equals campaign.Id
                where
                    campaign.RestaurantLocationId == locationId
                    && row.Outcome == CampaignFireService.RejectedOutcome
                    && row.UpdatedAtUtc >= fromUtc
                    && row.UpdatedAtUtc < toUtc
                select row.Id
            ).CountAsync(cancellationToken);
        }

        private async Task<
            IReadOnlyList<ReportsCampaignsPerformanceRowDto>
        > ListPerformanceAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var campaigns = await _context.Campaigns
                .AsNoTracking()
                .Where(c =>
                    c.RestaurantLocationId == locationId
                    && (
                        c.Status == CampaignLifecycleService.SentStatus
                        || c.Status
                            == CampaignLifecycleService.PartiallySentStatus
                        || c.Status == CampaignLifecycleService.FailedStatus
                    )
                    && c.UpdatedAt >= fromUtc
                    && c.UpdatedAt < toUtc
                )
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.GoalId,
                    c.Channel,
                    c.Status,
                })
                .ToListAsync(cancellationToken);

            if (campaigns.Count == 0)
            {
                return [];
            }

            var campaignIds = campaigns.Select(c => c.Id).ToList();
            var sentByCampaign = await _context.CampaignRecipientDeliveries
                .AsNoTracking()
                .Where(row =>
                    campaignIds.Contains(row.CampaignId)
                    && row.Outcome == CampaignFireService.AcceptedOutcome
                    && row.AcceptedAtUtc != null
                    && row.AcceptedAtUtc >= fromUtc
                    && row.AcceptedAtUtc < toUtc
                )
                .GroupBy(row => row.CampaignId)
                .Select(g => new
                {
                    CampaignId = g.Key,
                    Units = g.Sum(row => row.AcceptedUnits ?? 0),
                })
                .ToListAsync(cancellationToken);

            var unitsById = sentByCampaign.ToDictionary(
                row => row.CampaignId,
                row => row.Units
            );

            var deliveredByCampaign = await _context.CampaignRecipientDeliveries
                .AsNoTracking()
                .Where(row =>
                    campaignIds.Contains(row.CampaignId)
                    && row.Outcome == CampaignFireService.AcceptedOutcome
                )
                .GroupBy(row => row.CampaignId)
                .Select(g => new { CampaignId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var deliveredById = deliveredByCampaign.ToDictionary(
                row => row.CampaignId,
                row => row.Count
            );

            var failedByCampaign = await _context.CampaignRecipientDeliveries
                .AsNoTracking()
                .Where(row =>
                    campaignIds.Contains(row.CampaignId)
                    && row.Outcome == CampaignFireService.RejectedOutcome
                )
                .GroupBy(row => row.CampaignId)
                .Select(g => new { CampaignId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var failedById = failedByCampaign.ToDictionary(
                row => row.CampaignId,
                row => row.Count
            );

            var claimsByCampaign = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.CampaignId != null
                    && campaignIds.Contains(i.CampaignId.Value)
                    && i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc
                group i by i.CampaignId!.Value into g
                select new { CampaignId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            var claimsById = claimsByCampaign.ToDictionary(
                row => row.CampaignId,
                row => row.Count
            );

            var redemptionsByCampaign = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.CampaignId != null
                    && campaignIds.Contains(i.CampaignId.Value)
                    && i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc
                group i by i.CampaignId!.Value into g
                select new { CampaignId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            var redemptionsById = redemptionsByCampaign.ToDictionary(
                row => row.CampaignId,
                row => row.Count
            );

            return campaigns
                .Select(c => new ReportsCampaignsPerformanceRowDto
                {
                    CampaignId = c.Id,
                    Name = c.Name,
                    Goal = c.GoalId,
                    Channel = c.Channel,
                    Sent = unitsById.GetValueOrDefault(c.Id),
                    Delivered = deliveredById.GetValueOrDefault(c.Id),
                    Claims = claimsById.GetValueOrDefault(c.Id),
                    Redemptions = redemptionsById.GetValueOrDefault(c.Id),
                    // LocationActivity has no campaign attribution yet.
                    Unsubscribes = 0,
                    Failed = failedById.GetValueOrDefault(c.Id),
                    Status = c.Status,
                })
                .ToList();
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

        private async Task<
            IReadOnlyList<ReportsCampaignsAttentionRowDto>
        > ListNeedsAttentionAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.Campaigns
                .AsNoTracking()
                .Where(c =>
                    c.RestaurantLocationId == locationId
                    && (
                        c.Status == CampaignLifecycleService.FailedStatus
                        || c.Status
                            == CampaignLifecycleService.PartiallySentStatus
                    )
                )
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new ReportsCampaignsAttentionRowDto
                {
                    CampaignId = c.Id,
                    Name = c.Name,
                    Status = c.Status,
                })
                .ToListAsync(cancellationToken);
        }

        private static async Task<ReportsMetricDto> MetricPairAsync(
            Func<Task<int>> current,
            Func<Task<int>> previous
        )
        {
            return new ReportsMetricDto
            {
                Value = await current(),
                ValuePrevious = await previous(),
            };
        }
    }
}
