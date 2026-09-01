using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class PlanEntitlementsSnapshotService : IPlanEntitlementsSnapshot
    {
        private const int ExtraUsersPerLocation = 2;

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;

        public PlanEntitlementsSnapshotService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook
        )
        {
            _context = context;
            _pricebook = pricebook;
        }

        public async Task<PlanEntitlementsSnapshotDto> GetAsync(
            int restaurantId,
            int? locationId = null,
            CancellationToken cancellationToken = default
        )
        {
            return new PlanEntitlementsSnapshotDto
            {
                Account = await GetAccountAsync(restaurantId, cancellationToken),
                Location = locationId == null
                    ? null
                    : await GetLocationAsync(
                        restaurantId,
                        locationId.Value,
                        cancellationToken
                    ),
            };
        }

        public async Task<PlanEntitlementsAccountSnapshotDto> GetAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryResolvePlan(restaurantId, out var plan, out var billingAccount))
            {
                return UnavailableAccount();
            }

            var now = DateTime.UtcNow;
            var locationCount = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            var activeMembers = await _context.RestaurantMemberships
                .AsNoTracking()
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.Status == MembershipStatus.Active,
                    cancellationToken
                );
            var pendingInvites = await _context.TeamInvitations
                .AsNoTracking()
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.ExpiresAt > now,
                    cancellationToken
                );
            var activeOffers = await (
                from offer in _context.CatalogOffers.AsNoTracking()
                join location in _context.RestaurantLocations.AsNoTracking()
                    on offer.RestaurantLocationId equals location.Id
                where location.RestaurantId == restaurantId
                    && offer.Status == CatalogOfferStatus.Active
                select offer.Id
            ).CountAsync(cancellationToken);

            LocationCap.TryResolve(
                ResolveBook(billingAccount!),
                billingAccount!.SubscriptionPlan,
                billingAccount.PaidExtraLocationCount,
                out var locationCap
            );

            var teamCap = plan!.IncludedTeamMembers
                + (
                    billingAccount.PaidExtraLocationCount * ExtraUsersPerLocation
                );
            var teamCurrent = activeMembers + pendingInvites;

            return new PlanEntitlementsAccountSnapshotDto
            {
                Locations = Limit(locationCap, locationCount),
                TeamMembers = Limit(teamCap, teamCurrent),
                ActiveOffers = Limit(plan.ActiveOffersAccount, activeOffers),
            };
        }

        public async Task<PlanEntitlementsLocationSnapshotDto?> GetLocationAsync(
            int restaurantId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var ownsLocation = await _context.RestaurantLocations
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == locationId
                        && row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (!ownsLocation)
            {
                return null;
            }

            if (!TryResolvePlan(restaurantId, out var plan, out _))
            {
                return new PlanEntitlementsLocationSnapshotDto
                {
                    ActiveQrPlacements = Unavailable(),
                    PublishedGuestForms = Unavailable(),
                    DraftGuestForms = Unavailable(),
                };
            }

            var activeQr = await _context.QrCodes
                .AsNoTracking()
                .CountAsync(
                    row =>
                        row.RestaurantLocationId == locationId
                        && row.Status == QrCodeStatus.Active,
                    cancellationToken
                );

            // Standard private feedback form is always published per location.
            const int publishedGuestFormsCurrent = 1;
            const int draftGuestFormsCurrent = 0;

            return new PlanEntitlementsLocationSnapshotDto
            {
                ActiveQrPlacements = Limit(
                    plan!.ActiveQrPlacementsPerLocation,
                    activeQr
                ),
                PublishedGuestForms = Limit(
                    plan.PublishedGuestFormsPerLocation,
                    publishedGuestFormsCurrent
                ),
                DraftGuestForms = Limit(
                    plan.DraftGuestFormsPerLocation,
                    draftGuestFormsCurrent
                ),
            };
        }

        private bool TryResolvePlan(
            int restaurantId,
            out PricebookPlan? plan,
            out BillingAccount? billingAccount
        )
        {
            plan = null;
            billingAccount = _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefault(row => row.RestaurantId == restaurantId);
            if (
                billingAccount == null
                || string.IsNullOrWhiteSpace(billingAccount.ContractedPricebookId)
            )
            {
                return false;
            }

            PricebookSnapshot book;
            try
            {
                book = _pricebook.GetRequired(billingAccount.ContractedPricebookId);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var key = billingAccount.SubscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out plan))
            {
                return false;
            }

            return true;
        }

        private PricebookSnapshot ResolveBook(BillingAccount billingAccount)
        {
            return _pricebook.GetRequired(billingAccount.ContractedPricebookId);
        }

        private static PlanEntitlementLimitDto Limit(int cap, int current)
        {
            if (cap < 1)
            {
                return Unavailable();
            }

            return new PlanEntitlementLimitDto
            {
                Cap = cap,
                Current = current,
                AtCap = current >= cap,
                Available = true,
            };
        }

        private static PlanEntitlementLimitDto Unavailable()
        {
            return new PlanEntitlementLimitDto { Available = false };
        }

        private static PlanEntitlementsAccountSnapshotDto UnavailableAccount()
        {
            var unavailable = Unavailable();
            return new PlanEntitlementsAccountSnapshotDto
            {
                Locations = unavailable,
                TeamMembers = unavailable,
                ActiveOffers = unavailable,
            };
        }
    }
}
