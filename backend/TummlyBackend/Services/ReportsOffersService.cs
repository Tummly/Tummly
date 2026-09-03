using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offers report aggregates — Area Reports Offers surface (ticket 15).
    /// </summary>
    public sealed class ReportsOffersService : IReportsOffersService
    {
        private const int PerformanceCap = 20;
        private const int RecentRedemptionsCap = 5;
        private const int LowRedemptionMinClaims = 5;
        private const double LowRedemptionRateThreshold = 0.4;

        private readonly ApplicationDbContext _context;

        public ReportsOffersService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportsOffersDto> GetOffersReportAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            if (await IsLifetimeEmptyAsync(locationId, cancellationToken))
            {
                return new ReportsOffersDto { LifetimeEmpty = true };
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var activeOffersCurrent = await CountActiveOffersAtWindowEndAsync(
                locationId,
                toUtc,
                cancellationToken
            );
            var activeOffersPrevious = await CountActiveOffersAtWindowEndAsync(
                locationId,
                previousToUtc,
                cancellationToken
            );

            var offerClaimsCurrent = await CountOfferClaimsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var offerClaimsPrevious = await CountOfferClaimsAsync(
                locationId,
                previousFromUtc,
                previousToUtc,
                cancellationToken
            );

            var redemptionsCurrent = await CountOfferRedemptionsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var redemptionsPrevious = await CountOfferRedemptionsAsync(
                locationId,
                previousFromUtc,
                previousToUtc,
                cancellationToken
            );

            var expiredCurrent = await CountExpiredUnusedAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var expiredPrevious = await CountExpiredUnusedAsync(
                locationId,
                previousFromUtc,
                previousToUtc,
                cancellationToken
            );

            var invalidCurrent = await CountInvalidAttemptsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var invalidPrevious = await CountInvalidAttemptsAsync(
                locationId,
                previousFromUtc,
                previousToUtc,
                cancellationToken
            );

            var performanceAll = await BuildPerformanceAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var performance = performanceAll.Take(PerformanceCap).ToList();

            var recentRedemptions = await BuildRecentRedemptionsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );

            var controlSignals = await BuildControlSignalsAsync(
                locationId,
                fromUtc,
                toUtc,
                performanceAll,
                cancellationToken
            );

            return new ReportsOffersDto
            {
                LifetimeEmpty = false,
                Kpis = new ReportsOffersKpisDto
                {
                    ActiveOffers = Metric(activeOffersCurrent, activeOffersPrevious),
                    OfferClaims = Metric(offerClaimsCurrent, offerClaimsPrevious),
                    Redemptions = Metric(redemptionsCurrent, redemptionsPrevious),
                    RedemptionRate = new ReportsRateMetricDto
                    {
                        Value = RateOrNull(redemptionsCurrent, offerClaimsCurrent),
                        ValuePrevious = RateOrNull(
                            redemptionsPrevious,
                            offerClaimsPrevious
                        ),
                    },
                    ExpiredClaims = Metric(expiredCurrent, expiredPrevious),
                    InvalidAttempts = Metric(invalidCurrent, invalidPrevious),
                },
                Performance = performance,
                RecentRedemptions = recentRedemptions,
                ControlSignals = controlSignals,
            };
        }

        private async Task<bool> IsLifetimeEmptyAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return !await (
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
        }

        private async Task<int> CountActiveOffersAtWindowEndAsync(
            int locationId,
            DateTime windowEndUtc,
            CancellationToken cancellationToken
        )
        {
            var endDay = DateOnly.FromDateTime(windowEndUtc);

            return await _context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    o =>
                        o.RestaurantLocationId == locationId
                        && o.Status == CatalogOfferStatus.Active
                        && o.CreatedAt < windowEndUtc
                        && !(
                            o.Validity == CatalogOfferValidity.ChooseExpiryDate
                            && o.CustomExpiryDate != null
                            && o.CustomExpiryDate < endDay
                        ),
                    cancellationToken
                );
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

        private Task<int> CountExpiredUnusedAsync(
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
                    && i.ExpiryAtUtc >= fromUtc
                    && i.ExpiryAtUtc < toUtc
                    && i.RedeemedAtUtc == null
                    && i.CancelledAtUtc == null
                select i.Id
            ).CountAsync(cancellationToken);
        }

        private Task<int> CountInvalidAttemptsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return _context.OfferRedeemFailedAttempts
                .AsNoTracking()
                .CountAsync(
                    a =>
                        a.RestaurantLocationId == locationId
                        && a.AttemptedAtUtc >= fromUtc
                        && a.AttemptedAtUtc < toUtc,
                    cancellationToken
                );
        }

        private async Task<
            IReadOnlyList<ReportsOffersPerformanceRowDto>
        > BuildPerformanceAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var claimsByOffer = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc
                group i by o.Id into g
                select new { OfferId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            var redemptionsByOffer = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc
                group i by o.Id into g
                select new { OfferId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            var expiredByOffer = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.ExpiryAtUtc >= fromUtc
                    && i.ExpiryAtUtc < toUtc
                    && i.RedeemedAtUtc == null
                    && i.CancelledAtUtc == null
                group i by o.Id into g
                select new { OfferId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            var invalidByOffer = await _context.OfferRedeemFailedAttempts
                .AsNoTracking()
                .Where(a =>
                    a.RestaurantLocationId == locationId
                    && a.AttemptedAtUtc >= fromUtc
                    && a.AttemptedAtUtc < toUtc
                )
                .GroupBy(a => a.CatalogOfferId)
                .Select(g => new { OfferId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var offerIds = claimsByOffer
                .Select(r => r.OfferId)
                .Concat(redemptionsByOffer.Select(r => r.OfferId))
                .Concat(expiredByOffer.Select(r => r.OfferId))
                .Concat(invalidByOffer.Select(r => r.OfferId))
                .Distinct()
                .ToList();

            if (offerIds.Count == 0)
            {
                return [];
            }

            var offers = await _context.CatalogOffers
                .AsNoTracking()
                .Where(o => offerIds.Contains(o.Id))
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    o.Status,
                })
                .ToListAsync(cancellationToken);

            var claimsMap = claimsByOffer.ToDictionary(r => r.OfferId, r => r.Count);
            var redemptionsMap = redemptionsByOffer.ToDictionary(
                r => r.OfferId,
                r => r.Count
            );
            var expiredMap = expiredByOffer.ToDictionary(r => r.OfferId, r => r.Count);
            var invalidMap = invalidByOffer.ToDictionary(r => r.OfferId, r => r.Count);

            return offers
                .Select(o =>
                {
                    claimsMap.TryGetValue(o.Id, out var claims);
                    redemptionsMap.TryGetValue(o.Id, out var redemptions);
                    expiredMap.TryGetValue(o.Id, out var expired);
                    invalidMap.TryGetValue(o.Id, out var invalid);
                    return new ReportsOffersPerformanceRowDto
                    {
                        OfferId = o.Id,
                        Offer = o.Title,
                        Status = o.Status,
                        Claims = claims,
                        Redemptions = redemptions,
                        Rate = RateOrNull(redemptions, claims),
                        Expired = expired,
                        Invalid = invalid,
                    };
                })
                .Where(row =>
                    row.Claims > 0
                    || row.Redemptions > 0
                    || row.Expired > 0
                    || row.Invalid > 0
                )
                .OrderByDescending(row => row.Redemptions)
                .ThenByDescending(row => row.Claims)
                .ThenBy(row => row.OfferId)
                .ToList();
        }

        private async Task<
            IReadOnlyList<ReportsOffersRecentRedemptionDto>
        > BuildRecentRedemptionsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                join g in _context.LocationGuests.AsNoTracking()
                    on i.LocationGuestId equals g.Id
                join loc in _context.RestaurantLocations.AsNoTracking()
                    on o.RestaurantLocationId equals loc.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc
                orderby i.RedeemedAtUtc descending, i.Id descending
                select new ReportsOffersRecentRedemptionDto
                {
                    Id = i.Id,
                    DateTimeUtc = i.RedeemedAtUtc!.Value,
                    OfferTitle = o.Title,
                    GuestName = g.Name,
                    LocationName = loc.LocationName,
                    Outcome = "redeemed",
                }
            )
                .Take(RecentRedemptionsCap)
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<object>> BuildControlSignalsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            IReadOnlyList<ReportsOffersPerformanceRowDto> performance,
            CancellationToken cancellationToken
        )
        {
            var signals = new List<object>();

            var repeatedInvalidCount = await _context.OfferRedeemFailedAttempts
                .AsNoTracking()
                .CountAsync(
                    a =>
                        a.RestaurantLocationId == locationId
                        && a.AttemptedAtUtc >= fromUtc
                        && a.AttemptedAtUtc < toUtc
                        && (
                            a.Reason == OfferRedeemFailureReasons.AlreadyUsed
                            || a.Reason == OfferRedeemFailureReasons.Expired
                        ),
                    cancellationToken
                );

            if (repeatedInvalidCount >= 2)
            {
                signals.Add(
                    new ReportsOffersRepeatedInvalidSignalDto
                    {
                        Kind = "repeated-invalid",
                        Count = repeatedInvalidCount,
                        Target = "redemption-log",
                    }
                );
            }

            var lowRedemption = performance
                .Where(row =>
                    row.Claims >= LowRedemptionMinClaims
                    && row.Rate != null
                    && row.Rate < LowRedemptionRateThreshold
                )
                .OrderBy(row => row.Rate)
                .ThenByDescending(row => row.Claims)
                .ThenBy(row => row.OfferId)
                .FirstOrDefault();

            if (lowRedemption != null)
            {
                signals.Add(
                    new ReportsOffersLowRedemptionSignalDto
                    {
                        Kind = "low-redemption",
                        OfferId = lowRedemption.OfferId,
                        OfferTitle = lowRedemption.Offer,
                        Claims = lowRedemption.Claims,
                        Redemptions = lowRedemption.Redemptions,
                        Rate = lowRedemption.Rate!.Value,
                        Target = "offers",
                    }
                );
            }

            return signals;
        }

        private static ReportsMetricDto Metric(int value, int valuePrevious)
        {
            return new ReportsMetricDto
            {
                Value = value,
                ValuePrevious = valuePrevious,
            };
        }

        private static double? RateOrNull(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                return null;
            }

            return (double)numerator / denominator;
        }
    }
}
