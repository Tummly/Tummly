using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Submitted/accepted Campaign outbound counts for overview Messages sent
    /// (ticket 31 / 29).
    /// </summary>
    public static class CampaignAcceptedMessageCounts
    {
        public static Task<int> CountAcceptedAsync(
            ApplicationDbContext context,
            DateTime fromUtcInclusive,
            DateTime toUtcExclusive,
            int? locationId = null,
            string? channel = null,
            CancellationToken cancellationToken = default
        )
        {
            var normalizedChannel = string.IsNullOrWhiteSpace(channel)
                ? null
                : channel.Trim().ToLowerInvariant();

            var query =
                from row in context.CampaignRecipientDeliveries.AsNoTracking()
                join campaign in context.Campaigns.AsNoTracking()
                    on row.CampaignId equals campaign.Id
                where
                    row.Outcome == CampaignFireService.AcceptedOutcome
                    && row.AcceptedAtUtc != null
                    && row.AcceptedAtUtc >= fromUtcInclusive
                    && row.AcceptedAtUtc < toUtcExclusive
                    && (
                        locationId == null
                        || campaign.RestaurantLocationId == locationId
                    )
                    && (
                        normalizedChannel == null
                        || row.Channel == normalizedChannel
                    )
                select row;

            return query.CountAsync(cancellationToken);
        }
    }
}
