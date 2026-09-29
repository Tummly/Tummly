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

        /// <summary>
        /// Strongest peer placement must have at least this many scans in-window
        /// before an underperform fact may fire.
        /// </summary>
        public const int UnderperformMinPeerScans = 5;

        /// <summary>
        /// Worst placement scans must be at or below best × this ratio.
        /// </summary>
        public const double UnderperformMaxScanRatio = 0.5;

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

        public sealed record UnderperformQrFactDto(
            string Kind,
            int QrCodeId,
            string PlacementLabel,
            int Scans,
            int Contactable,
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
        /// Priority: feedback-needs-attention → underperform-qr → repeated-invalid
        /// → low-redemption; then <see cref="Cap"/>. Windowed facts need a coverage
        /// window; without one, only feedback may emit. Does not apply Offers
        /// <c>LifetimeEmpty</c> gate.
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
                var underperformQr = await FindWorstUnderperformQrAsync(
                    context,
                    locationId,
                    from,
                    to,
                    cancellationToken
                );
                if (underperformQr != null)
                {
                    facts.Add(underperformQr);
                }

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

        /// <summary>
        /// Active/Paused physical or Digital placements only (excludes Smart Guest).
        /// Needs ≥2 candidates; strongest peer ≥ <see cref="UnderperformMinPeerScans"/>;
        /// worst scans strictly below best × <see cref="UnderperformMaxScanRatio"/>.
        /// </summary>
        public static async Task<UnderperformQrFactDto?> FindWorstUnderperformQrAsync(
            ApplicationDbContext context,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var placements = await context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && q.QrType != QrType.SmartGuest
                    && (
                        q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused
                    )
                )
                .Select(q => new
                {
                    q.Id,
                    q.QrType,
                    q.LinkName,
                })
                .ToListAsync(cancellationToken);

            if (placements.Count < 2)
            {
                return null;
            }

            var qrIds = placements.Select(p => p.Id).ToList();
            var scansByQr = (
                await context.QrScanEvents
                    .AsNoTracking()
                    .Where(e =>
                        e.QrCodeId != null
                        && qrIds.Contains(e.QrCodeId.Value)
                        && e.CreatedAt >= fromUtc
                        && e.CreatedAt < toUtc
                    )
                    .GroupBy(e => e.QrCodeId!.Value)
                    .Select(g => new { QrCodeId = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken)
            ).ToDictionary(r => r.QrCodeId, r => r.Count);

            var contactableByQr = (
                await context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        qrIds.Contains(f.QrCodeId)
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc
                        && !f.OffersOptOut
                    )
                    .GroupBy(f => f.QrCodeId)
                    .Select(g => new { QrCodeId = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken)
            ).ToDictionary(r => r.QrCodeId, r => r.Count);

            var scored = placements
                .Select(p =>
                {
                    scansByQr.TryGetValue(p.Id, out var scans);
                    contactableByQr.TryGetValue(p.Id, out var contactable);
                    var label =
                        FeedbackQrSourceMapping.ToDisplay(
                            new QrCode
                            {
                                QrType = p.QrType,
                                LinkName = p.LinkName,
                            }
                        )
                        ?? p.QrType.ToString();
                    return new
                    {
                        p.Id,
                        p.QrType,
                        PlacementLabel = label,
                        Scans = scans,
                        Contactable = contactable,
                    };
                })
                .ToList();

            var bestScans = scored.Max(r => r.Scans);
            if (bestScans < UnderperformMinPeerScans)
            {
                return null;
            }

            var maxAllowed = (int)Math.Floor(bestScans * UnderperformMaxScanRatio);
            return scored
                .Where(r => r.Scans < bestScans && r.Scans <= maxAllowed)
                .OrderBy(r => r.Scans)
                .ThenBy(r => r.Contactable)
                .ThenBy(r => r.Id)
                .Select(r => new UnderperformQrFactDto(
                    "underperform-qr",
                    r.Id,
                    r.PlacementLabel,
                    r.Scans,
                    r.Contactable,
                    "capture"
                ))
                .FirstOrDefault();
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
