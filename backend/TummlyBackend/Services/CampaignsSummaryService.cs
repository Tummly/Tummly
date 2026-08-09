using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Overview Campaign summary — KPI in-flight is Scheduled + Sending only
    /// (not Paused / not list In flight tab); messages use accepted counts.
    /// </summary>
    public sealed class CampaignsSummaryService : ICampaignsSummaryService
    {
        public const string EmailChannel = "email";

        private readonly ApplicationDbContext _context;

        public CampaignsSummaryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CampaignsSummaryDto> GetSummaryAsync(
            CampaignsSummaryQuery query,
            CancellationToken cancellationToken = default
        )
        {
            if (query.LocationId < 1)
            {
                throw new ArgumentException("locationId must be at least 1.");
            }

            ValidateOverviewWindow(query);

            var locationId = query.LocationId;

            var scheduled = await _context.Campaigns
                .AsNoTracking()
                .CountAsync(
                    campaign =>
                        campaign.RestaurantLocationId == locationId
                        && campaign.Status == CampaignsListService.ScheduledStatus,
                    cancellationToken
                );

            var sending = await _context.Campaigns
                .AsNoTracking()
                .CountAsync(
                    campaign =>
                        campaign.RestaurantLocationId == locationId
                        && campaign.Status == CampaignsListService.SendingStatus,
                    cancellationToken
                );

            var fromUtc = query.OverviewDateFrom
                ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var toUtc = query.OverviewDateTo
                ?? DateTime.UtcNow.AddYears(1);

            var messagesAcceptedEmail =
                await CampaignAcceptedMessageCounts.CountAcceptedAsync(
                    _context,
                    fromUtcInclusive: fromUtc,
                    toUtcExclusive: toUtc,
                    locationId: locationId,
                    channel: EmailChannel,
                    cancellationToken: cancellationToken
                );

            return new CampaignsSummaryDto
            {
                CampaignsInFlightScheduled = scheduled,
                CampaignsInFlightSending = sending,
                MessagesSentAccepted = messagesAcceptedEmail,
                MessagesSentAcceptedEmail = messagesAcceptedEmail,
            };
        }

        private static void ValidateOverviewWindow(CampaignsSummaryQuery query)
        {
            var hasFrom = query.OverviewDateFrom != null;
            var hasTo = query.OverviewDateTo != null;
            if (hasFrom != hasTo)
            {
                throw new ArgumentException(
                    "overviewDateFrom and overviewDateTo are both required when either is set."
                );
            }

            if (
                hasFrom
                && hasTo
                && query.OverviewDateFrom!.Value >= query.OverviewDateTo!.Value
            )
            {
                throw new ArgumentException(
                    "overviewDateFrom must be before overviewDateTo."
                );
            }
        }
    }
}
