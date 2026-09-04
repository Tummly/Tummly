using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Phase-1 Weekly brief recommended-action facts + suggested Draft campaign
    /// for the shared ready envelope (derived at GET time; not stored in BodyJson).
    /// </summary>
    public static class WeeklyBriefRecommendedActions
    {
        /// <summary>
        /// Same thresholds as <c>ReportsOffersService</c> control signals.
        /// </summary>
        public const int LowRedemptionMinClaims = 5;

        /// <summary>
        /// Same threshold as <c>ReportsOffersService</c> control signals (rate &lt; 0.4).
        /// </summary>
        public const double LowRedemptionRateThreshold = 0.4;

        public const int Cap = 3;

        public sealed record FeedbackNeedsAttentionFactDto(
            string Kind,
            int Count,
            string Target,
            string? Title = null,
            string? Subtitle = null
        );

        public sealed record RepeatedInvalidFactDto(
            string Kind,
            int Count,
            string Target,
            string? Title = null,
            string? Subtitle = null
        );

        public sealed record LowRedemptionFactDto(
            string Kind,
            int OfferId,
            string OfferTitle,
            int Claims,
            int Redemptions,
            double Rate,
            string Target,
            string? Title = null,
            string? Subtitle = null
        );

        public sealed record SuggestedCampaignDto(
            int CampaignId,
            string Name,
            string? AudienceKey
        );

        /// <summary>
        /// Priority: feedback-needs-attention → repeated-invalid → low-redemption;
        /// then <see cref="Cap"/>. Offer facts need a coverage window; without one,
        /// only feedback may emit. Does not apply Offers <c>LifetimeEmpty</c> gate.
        /// </summary>
        public static async Task<IReadOnlyList<object>> BuildFactsAsync(
            ApplicationDbContext context,
            int locationId,
            WeeklyBriefMetrics metrics,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken
        )
        {
            var facts = new List<object>(Cap);

            if (metrics.NeedsAttentionCount > 0)
            {
                facts.Add(
                    new FeedbackNeedsAttentionFactDto(
                        "feedback-needs-attention",
                        metrics.NeedsAttentionCount,
                        "feedback-needs-attention"
                    )
                );
            }

            if (fromUtc is DateTime from && toUtc is DateTime to)
            {
                var repeatedInvalidCount = await context.OfferRedeemFailedAttempts
                    .AsNoTracking()
                    .CountAsync(
                        a =>
                            a.RestaurantLocationId == locationId
                            && a.AttemptedAtUtc >= from
                            && a.AttemptedAtUtc < to
                            && (
                                a.Reason == OfferRedeemFailureReasons.AlreadyUsed
                                || a.Reason == OfferRedeemFailureReasons.Expired
                            ),
                        cancellationToken
                    );

                if (repeatedInvalidCount >= 2)
                {
                    facts.Add(
                        new RepeatedInvalidFactDto(
                            "repeated-invalid",
                            repeatedInvalidCount,
                            "redemption-log"
                        )
                    );
                }

                var lowRedemption = await FindWorstLowRedemptionAsync(
                    context,
                    locationId,
                    from,
                    to,
                    cancellationToken
                );
                if (lowRedemption != null)
                {
                    facts.Add(lowRedemption);
                }
            }

            return facts.Take(Cap).ToList();
        }

        /// <summary>
        /// Newest Draft at the location with create or update in the coverage window.
        /// </summary>
        public static async Task<SuggestedCampaignDto?> FindSuggestedCampaignAsync(
            ApplicationDbContext context,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var draft = await context.Campaigns
                .AsNoTracking()
                .Where(c =>
                    c.RestaurantLocationId == locationId
                    && c.Status == CampaignDraftService.DraftStatus
                    && (
                        (c.CreatedAt >= fromUtc && c.CreatedAt < toUtc)
                        || (c.UpdatedAt >= fromUtc && c.UpdatedAt < toUtc)
                    )
                )
                .OrderByDescending(c => c.UpdatedAt)
                .ThenByDescending(c => c.Id)
                .Select(c => new SuggestedCampaignDto(
                    c.Id,
                    c.Name,
                    c.AudienceKey
                ))
                .FirstOrDefaultAsync(cancellationToken);

            return draft;
        }

        private static async Task<LowRedemptionFactDto?> FindWorstLowRedemptionAsync(
            ApplicationDbContext context,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var claimsByOffer = await (
                from i in context.OfferIssues.AsNoTracking()
                join o in context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc
                group i by new { o.Id, o.Title } into g
                select new
                {
                    OfferId = g.Key.Id,
                    OfferTitle = g.Key.Title,
                    Claims = g.Count(),
                }
            ).ToListAsync(cancellationToken);

            if (claimsByOffer.Count == 0)
            {
                return null;
            }

            var offerIds = claimsByOffer.Select(r => r.OfferId).ToList();
            var redemptionsByOffer = await (
                from i in context.OfferIssues.AsNoTracking()
                join o in context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && offerIds.Contains(o.Id)
                    && i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc
                group i by o.Id into g
                select new { OfferId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            var redemptionsMap = redemptionsByOffer.ToDictionary(
                r => r.OfferId,
                r => r.Count
            );

            return claimsByOffer
                .Select(row =>
                {
                    redemptionsMap.TryGetValue(row.OfferId, out var redemptions);
                    var rate = row.Claims == 0
                        ? (double?)null
                        : (double)redemptions / row.Claims;
                    return new
                    {
                        row.OfferId,
                        row.OfferTitle,
                        row.Claims,
                        Redemptions = redemptions,
                        Rate = rate,
                    };
                })
                .Where(row =>
                    row.Claims >= LowRedemptionMinClaims
                    && row.Rate != null
                    && row.Rate < LowRedemptionRateThreshold
                )
                .OrderBy(row => row.Rate)
                .ThenByDescending(row => row.Claims)
                .ThenBy(row => row.OfferId)
                .Select(row => new LowRedemptionFactDto(
                    "low-redemption",
                    row.OfferId,
                    row.OfferTitle,
                    row.Claims,
                    row.Redemptions,
                    row.Rate!.Value,
                    "offers"
                ))
                .FirstOrDefault();
        }
    }
}
