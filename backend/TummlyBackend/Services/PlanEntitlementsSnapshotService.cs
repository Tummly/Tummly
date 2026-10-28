using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
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
        private readonly IActiveQrCapacity _activeQrCapacity;

        public PlanEntitlementsSnapshotService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            IActiveQrCapacity activeQrCapacity
        )
        {
            _context = context;
            _pricebook = pricebook;
            _activeQrCapacity = activeQrCapacity;
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
            // Same rows the insert gate counts (Draft + Active).
            var locationCount = await PendingPlanLimits.CountInsertLocationsAsync(
                _context,
                restaurantId,
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

            var customLocs = CustomConfiguredLocations(billingAccount!);
            LocationCap.TryResolve(
                ResolveBook(billingAccount!),
                billingAccount!.SubscriptionPlan,
                billingAccount.PaidExtraLocationCount,
                customLocs,
                out var locationCap
            );

            var teamCap = plan!.IncludedTeamMembers
                + (
                    billingAccount.PaidExtraLocationCount * ExtraUsersPerLocation
                );
            var teamCurrent = activeMembers + pendingInvites;

            // Group: +3 per paid extra Location, same count as ActiveOfferCapGate.
            // Custom Offers already come from its quote (quantities.Offers).
            var additionalPaidLocations = string.Equals(
                billingAccount.SubscriptionPlan.Trim(),
                BillingSubscriptionPlans.Group,
                StringComparison.OrdinalIgnoreCase
            )
                ? billingAccount.PaidExtraLocationCount
                : 0;

            return new PlanEntitlementsAccountSnapshotDto
            {
                Locations = Limit(locationCap, locationCount),
                TeamMembers = Limit(teamCap, teamCurrent),
                ActiveOffers = Limit(
                    WorkspaceOfferPool.Compute(
                        plan.ActiveOffersAccount,
                        additionalPaidLocations
                    ),
                    activeOffers
                ),
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

            if (!TryResolvePlan(restaurantId, out var plan, out var billingAccount))
            {
                return new PlanEntitlementsLocationSnapshotDto
                {
                    ActiveQrPlacements = Unavailable(),
                    PublishedGuestForms = Unavailable(),
                    DraftGuestForms = Unavailable(),
                };
            }

            // Workspace pool and Workspace used (Commercial V4): the same numbers
            // for every Location of the Workspace.
            var qrAllowance = await _activeQrCapacity.GetAllowanceAsync(
                billingAccount,
                cancellationToken
            );
            var activeQr = await _activeQrCapacity.CountUsedForRestaurantAsync(
                restaurantId,
                cancellationToken
            );

            // Standard private feedback form is always published per location.
            const int publishedGuestFormsCurrent = 1;
            const int draftGuestFormsCurrent = 0;

            return new PlanEntitlementsLocationSnapshotDto
            {
                ActiveQrPlacements = qrAllowance == null
                    ? Unavailable()
                    : Limit(qrAllowance.WorkspacePool, activeQr),
                PublishedGuestForms = Limit(
                    plan!.PublishedGuestFormsPerLocation,
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
            if (key == "custom")
            {
                var custom = CustomPlanConfig.TryParse(
                    billingAccount.CustomPlanConfigJson
                );
                if (custom == null)
                {
                    return false;
                }

                plan = new PricebookPlan
                {
                    Key = "custom",
                    DisplayName = "Custom",
                    MonthlyNetPence = custom.MonthlyNetPence,
                    AnnualNetPence = custom.PeriodNetPence,
                    IncludedLocations = Math.Min(
                        custom.Quantities.Locations,
                        CustomPlanConfig.MaxLocations
                    ),
                    IncludedTeamMembers = Math.Max(0, custom.Quantities.TeamUsers),
                    ActiveOffersAccount = Math.Max(0, custom.Quantities.Offers),
                    ActiveQrPlacementsPerLocation = Math.Max(
                        0,
                        custom.Quantities.QrPlacements
                    ),
                    PublishedGuestFormsPerLocation = 1,
                    DraftGuestFormsPerLocation = 1,
                    CreditsMonthly = new PricebookChannelCredits
                    {
                        Ai = custom.Quantities.AiCredits,
                        Email = custom.Quantities.EmailCredits,
                        Sms = custom.Quantities.SmsCredits,
                    },
                };
                return true;
            }

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

        private static int? CustomConfiguredLocations(BillingAccount billingAccount)
        {
            if (
                !string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.Ordinal
                )
            )
            {
                return null;
            }

            var custom = CustomPlanConfig.TryParse(billingAccount.CustomPlanConfigJson);
            return custom?.Quantities.Locations;
        }

        private static PlanEntitlementLimitDto Limit(int cap, int current)
        {
            if (cap < 0)
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
