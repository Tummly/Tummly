using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Window aggregates for Offers Performance + Details Overview KPIs (ticket 29).
    /// </summary>
    public sealed class OffersMetricsService : IOffersMetricsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPlanEntitlementsSnapshot? _entitlements;

        public OffersMetricsService(
            ApplicationDbContext context,
            IPlanEntitlementsSnapshot? entitlements = null
        )
        {
            _context = context;
            _entitlements = entitlements;
        }

        public async Task<OffersPerformanceDto> GetPerformanceAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var activeOffers = await _context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    o =>
                        o.RestaurantLocationId == locationId
                        && o.Status == CatalogOfferStatus.Active,
                    cancellationToken
                );

            var issuesAtLocation =
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where o.RestaurantLocationId == locationId
                select i;

            var offersIssued = await issuesAtLocation.CountAsync(
                i => i.IssuedAtUtc >= fromUtc && i.IssuedAtUtc < toUtc,
                cancellationToken
            );

            var claims = await issuesAtLocation.CountAsync(
                i =>
                    i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc,
                cancellationToken
            );

            var redemptions = await issuesAtLocation.CountAsync(
                i =>
                    i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc,
                cancellationToken
            );

            var entitlements = await ResolveAccountEntitlementsAsync(
                locationId,
                cancellationToken
            );

            return new OffersPerformanceDto
            {
                ActiveOffers = activeOffers,
                OffersIssued = offersIssued,
                Claims = claims,
                Redemptions = redemptions,
                ClaimToRedemptionRate = RateOrNull(redemptions, claims),
                Entitlements = entitlements,
            };
        }

        private async Task<PlanEntitlementsAccountSnapshotDto> ResolveAccountEntitlementsAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (_entitlements == null)
            {
                return new PlanEntitlementsAccountSnapshotDto
                {
                    Locations = UnavailableEntitlement(),
                    TeamMembers = UnavailableEntitlement(),
                    ActiveOffers = UnavailableEntitlement(),
                };
            }

            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId < 1)
            {
                return new PlanEntitlementsAccountSnapshotDto
                {
                    Locations = UnavailableEntitlement(),
                    TeamMembers = UnavailableEntitlement(),
                    ActiveOffers = UnavailableEntitlement(),
                };
            }

            return await _entitlements.GetAccountAsync(restaurantId, cancellationToken);
        }

        private static PlanEntitlementLimitDto UnavailableEntitlement()
        {
            return new PlanEntitlementLimitDto { Available = false };
        }

        public async Task<OfferMetricsDto?> GetOfferMetricsAsync(
            int offerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var offerExists = await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(o => o.Id == offerId, cancellationToken);

            if (!offerExists)
            {
                return null;
            }

            var issues = _context.OfferIssues
                .AsNoTracking()
                .Where(i => i.CatalogOfferId == offerId);

            var claims = await issues.CountAsync(
                i =>
                    i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc,
                cancellationToken
            );

            var redemptions = await issues.CountAsync(
                i =>
                    i.RedeemedAtUtc != null
                    && i.RedemptionVoidedAtUtc == null
                    && i.RedeemedAtUtc >= fromUtc
                    && i.RedeemedAtUtc < toUtc,
                cancellationToken
            );

            var expiredUnused = await issues.CountAsync(
                i =>
                    i.ExpiryAtUtc >= fromUtc
                    && i.ExpiryAtUtc < toUtc
                    && i.RedeemedAtUtc == null
                    && i.CancelledAtUtc == null,
                cancellationToken
            );

            var failedAttempts = await _context.OfferRedeemFailedAttempts
                .AsNoTracking()
                .CountAsync(
                    a =>
                        a.CatalogOfferId == offerId
                        && a.AttemptedAtUtc >= fromUtc
                        && a.AttemptedAtUtc < toUtc,
                    cancellationToken
                );

            return new OfferMetricsDto
            {
                Claims = claims,
                Redemptions = redemptions,
                RedemptionRate = RateOrNull(redemptions, claims),
                ExpiredUnused = expiredUnused,
                FailedAttempts = failedAttempts,
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
