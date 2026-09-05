using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

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

            return campaigns
                .Select(c => new ReportsCampaignsPerformanceRowDto
                {
                    CampaignId = c.Id,
                    Name = c.Name,
                    Goal = c.GoalId,
                    Channel = c.Channel,
                    Sent = unitsById.GetValueOrDefault(c.Id),
                    Status = c.Status,
                })
                .ToList();
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
