using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Active offer + in-flight campaign cards for Location detail (ticket 02).
    /// </summary>
    public sealed class LocationDetailOfferCardsComposer
    {
        private const int MaxCards = 4;

        private static readonly HashSet<string> ActiveCampaignStatuses = new(
            StringComparer.Ordinal
        )
        {
            CampaignsListService.ScheduledStatus,
            CampaignsListService.SendingStatus,
            CampaignsListService.PausedStatus,
            CampaignsListService.PartiallySentStatus,
        };

        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _time;

        public LocationDetailOfferCardsComposer(
            ApplicationDbContext context,
            TimeProvider time
        )
        {
            _context = context;
            _time = time;
        }

        public async Task<IReadOnlyList<LocationDetailOfferCardDto>> ComposeAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var venueLocalToday = DateOnly.FromDateTime(
                LondonDateFormat.ToLondonLocal(_time.GetUtcNow().UtcDateTime)
            );

            var offers = await _context.CatalogOffers
                .AsNoTracking()
                .Where(o =>
                    o.RestaurantLocationId == locationId
                    && o.Status == CatalogOfferStatus.Active
                )
                .Select(o => new CardSource(
                    o.Id,
                    "offer",
                    o.Status,
                    o.Title,
                    o.UpdatedAt,
                    o.Validity,
                    o.CustomExpiryDate
                ))
                .ToListAsync(cancellationToken);

            var activeOffers = offers
                .Where(offer =>
                    string.Equals(
                        CatalogOfferStatus.ResolveEffectiveStatus(
                            offer.Status,
                            offer.Validity,
                            offer.CustomExpiryDate,
                            venueLocalToday
                        ),
                        CatalogOfferStatus.Active,
                        StringComparison.Ordinal
                    )
                )
                .ToList();

            var campaigns = await _context.Campaigns
                .AsNoTracking()
                .Where(c =>
                    c.RestaurantLocationId == locationId
                    && ActiveCampaignStatuses.Contains(c.Status)
                )
                .Select(c => new CardSource(
                    c.Id,
                    "campaign",
                    c.Status,
                    c.Name,
                    c.UpdatedAt,
                    CatalogOfferValidity.Days30AfterIssue,
                    null
                ))
                .ToListAsync(cancellationToken);

            var selected = activeOffers
                .Concat(campaigns)
                .OrderByDescending(card => card.UpdatedAt)
                .Take(MaxCards)
                .ToList();

            if (selected.Count == 0)
            {
                return Array.Empty<LocationDetailOfferCardDto>();
            }

            var offerIds = selected
                .Where(card => card.Kind == "offer")
                .Select(card => card.Id)
                .ToList();
            var campaignIds = selected
                .Where(card => card.Kind == "campaign")
                .Select(card => card.Id)
                .ToList();

            var offerStats = await LoadOfferStatsAsync(offerIds, cancellationToken);
            var deliveryCounts = await LoadCampaignDeliveryCountsAsync(
                campaignIds,
                cancellationToken
            );

            return selected
                .Select(card =>
                {
                    if (card.Kind == "campaign")
                    {
                        deliveryCounts.TryGetValue(card.Id, out var delivered);
                        return new LocationDetailOfferCardDto
                        {
                            EntityId = card.Id,
                            Kind = "campaign",
                            StatusLabel = FormatStatusLabel(card.Status),
                            Title = card.Title,
                            Meta = FormatCampaignMeta(delivered),
                            PrimaryCta = "View campaign",
                            SecondaryCta = "Preview",
                        };
                    }

                    offerStats.TryGetValue(
                        card.Id,
                        out var stats
                    );
                    return new LocationDetailOfferCardDto
                    {
                        EntityId = card.Id,
                        Kind = "offer",
                        StatusLabel = FormatStatusLabel(card.Status),
                        Title = card.Title,
                        Meta = FormatOfferMeta(
                            stats.Claims,
                            stats.Redemptions
                        ),
                        PrimaryCta = "View offer",
                        SecondaryCta = "View redemptions",
                    };
                })
                .ToList();
        }

        private async Task<
            Dictionary<int, (int Claims, int Redemptions)>
        > LoadOfferStatsAsync(
            IReadOnlyList<int> offerIds,
            CancellationToken cancellationToken
        )
        {
            if (offerIds.Count == 0)
            {
                return new Dictionary<int, (int Claims, int Redemptions)>();
            }

            var rows = await _context.OfferIssues
                .AsNoTracking()
                .Where(i => offerIds.Contains(i.CatalogOfferId))
                .GroupBy(i => i.CatalogOfferId)
                .Select(group => new
                {
                    OfferId = group.Key,
                    Claims = group.Count(i => i.ClaimedAtUtc != null),
                    Redemptions = group.Count(i =>
                        i.RedeemedAtUtc != null && i.RedemptionVoidedAtUtc == null
                    ),
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                row => row.OfferId,
                row => (row.Claims, row.Redemptions)
            );
        }

        private async Task<Dictionary<int, int>> LoadCampaignDeliveryCountsAsync(
            IReadOnlyList<int> campaignIds,
            CancellationToken cancellationToken
        )
        {
            if (campaignIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            var rows = await _context.CampaignRecipientDeliveries
                .AsNoTracking()
                .Where(row =>
                    campaignIds.Contains(row.CampaignId)
                    && row.Outcome == CampaignFireService.AcceptedOutcome
                )
                .GroupBy(row => row.CampaignId)
                .Select(group => new
                {
                    CampaignId = group.Key,
                    Count = group.Count(),
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(row => row.CampaignId, row => row.Count);
        }

        private static string FormatOfferMeta(int claims, int redemptions) =>
            $"{claims} claims · {redemptions} redemptions";

        private static string FormatCampaignMeta(int delivered) =>
            delivered > 0
                ? $"{delivered} delivered · — offer claims"
                : "— delivered · — offer claims";

        private static string FormatStatusLabel(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "—";
            }

            if (status.Equals("partially-sent", StringComparison.Ordinal))
            {
                return "Partially sent";
            }

            return char.ToUpperInvariant(status[0]) + status[1..];
        }

        private sealed record CardSource(
            int Id,
            string Kind,
            string Status,
            string Title,
            DateTime UpdatedAt,
            CatalogOfferValidity Validity,
            DateOnly? CustomExpiryDate
        );
    }
}
