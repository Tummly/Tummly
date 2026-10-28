using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Shared Workspace-wide active-QR capacity (G1 / V4). Capture and Shop
    /// checkout use this one service and lock the same <c>BillingAccounts</c> row.
    /// </summary>
    public class ActiveQrCapacity : IActiveQrCapacity
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebookCatalog;

        public ActiveQrCapacity(
            ApplicationDbContext context,
            IPricebookCatalog pricebookCatalog
        )
        {
            _context = context;
            _pricebookCatalog = pricebookCatalog;
        }

        public async Task<BillingAccount?> LockBillingAccountAsync(
            int restaurantId,
            CancellationToken ct
        )
        {
            if (_context.Database.IsSqlServer())
            {
                return await _context.BillingAccounts
                    .FromSqlInterpolated(
                        $"SELECT * FROM BillingAccounts WITH (UPDLOCK, ROWLOCK) WHERE RestaurantId = {restaurantId}"
                    )
                    .FirstOrDefaultAsync(ct);
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(
                row => row.RestaurantId == restaurantId,
                ct
            );
        }

        public async Task<ActiveQrAllowance?> GetAllowanceAsync(
            BillingAccount? account,
            CancellationToken ct
        )
        {
            try
            {
                return await ResolveAllowanceAsync(account, ct);
            }
            catch (OverflowException)
            {
                // A pool too large for int is a bad plan row, not a 500: fail closed.
                return null;
            }
        }

        public async Task<int> CountUsedForRestaurantAsync(
            int restaurantId,
            CancellationToken ct
        )
        {
            var locationIds = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .ToListAsync(ct);
            if (locationIds.Count == 0)
            {
                return 0;
            }

            var usedByLocation = await CountUsedByLocationAsync(locationIds, ct);
            return usedByLocation.Values.Sum();
        }

        public async Task<int> ActivationCostAsync(
            int locationId,
            QrType qrType,
            CancellationToken ct
        )
        {
            var heldOfType = await _context.ShopQrSlotReservations.AnyAsync(row =>
                row.RestaurantLocationId == locationId
                && row.QrType == qrType
                && (
                    row.Status == ShopQrSlotReservationStatuses.Open
                    || row.Status == ShopQrSlotReservationStatuses.Review
                ),
                ct
            );
            if (!heldOfType)
            {
                return 1;
            }

            // A held slot with no Active code of that type is taken over, not added to.
            var activeOfType = await _context.QrCodes.AnyAsync(qr =>
                qr.RestaurantLocationId == locationId
                && qr.QrType == qrType
                && qr.Status == QrCodeStatus.Active,
                ct
            );
            return activeOfType ? 1 : 0;
        }

        public async Task<ActiveQrCapacityDecision> EvaluateAsync(
            int restaurantId,
            int locationId,
            int increment,
            QrType? activatingType = null,
            CancellationToken ct = default
        )
        {
            if (increment <= 0)
            {
                return new ActiveQrCapacityDecision(
                    EntitlementsAvailable: true,
                    Fits: true,
                    WorkspacePool: 0,
                    Used: 0
                );
            }

            var billingAccount = await LockBillingAccountAsync(restaurantId, ct);
            var allowance = await GetAllowanceAsync(billingAccount, ct);
            if (allowance == null)
            {
                return new ActiveQrCapacityDecision(
                    EntitlementsAvailable: false,
                    Fits: false,
                    WorkspacePool: 0,
                    Used: 0
                );
            }

            var pool = allowance.WorkspacePool;
            var used = await CountUsedForRestaurantAsync(restaurantId, ct);

            // Activating a type with a held slot takes over that slot, so it leaves the count.
            var takenOverHeld = activatingType is { } type
                ? 1 - await ActivationCostAsync(locationId, type, ct)
                : 0;
            var usedBefore = used - takenOverHeld;

            var fits = usedBefore + increment <= pool;
            return new ActiveQrCapacityDecision(true, fits, pool, used);
        }

        /// <summary>
        /// Per Location: Active codes plus each distinct held reservation QR type
        /// that has no Active code of that type.
        /// </summary>
        private async Task<Dictionary<int, int>> CountUsedByLocationAsync(
            List<int> locationIds,
            CancellationToken ct
        )
        {
            var activeCodes = await _context.QrCodes
                .Where(qr =>
                    locationIds.Contains(qr.RestaurantLocationId)
                    && qr.Status == QrCodeStatus.Active
                )
                .Select(qr => new { qr.RestaurantLocationId, qr.QrType })
                .ToListAsync(ct);

            var heldTypes = await _context.ShopQrSlotReservations
                .Where(row =>
                    locationIds.Contains(row.RestaurantLocationId)
                    && (
                        row.Status == ShopQrSlotReservationStatuses.Open
                        || row.Status == ShopQrSlotReservationStatuses.Review
                    )
                )
                .Select(row => new { row.RestaurantLocationId, row.QrType })
                .Distinct()
                .ToListAsync(ct);

            var activeCountByLocation = activeCodes
                .GroupBy(qr => qr.RestaurantLocationId)
                .ToDictionary(group => group.Key, group => group.Count());
            var activeKeys = activeCodes
                .Select(qr => (qr.RestaurantLocationId, qr.QrType))
                .ToHashSet();

            var used = locationIds
                .Distinct()
                .ToDictionary(id => id, id => activeCountByLocation.GetValueOrDefault(id));

            foreach (var held in heldTypes)
            {
                if (!activeKeys.Contains((held.RestaurantLocationId, held.QrType)))
                {
                    used[held.RestaurantLocationId] += 1;
                }
            }

            return used;
        }

        /// <summary>
        /// Commercial V4 (25 Oct 2026) Workspace pool. Standard plans: pricebook
        /// base allowance, plus 10 per paid extra Group Location on Group; no
        /// purchase path for single slots yet, so purchased extras are 0. Custom
        /// v2: <c>Quantities.QrPlacements</c> + (Group base only) 10 x Locations
        /// beyond the base plan's included Locations + <c>QrPlacementsAccountExtra</c>.
        /// Custom v1 (no base plan): <c>QrPlacements</c> + <c>QrPlacementsAccountExtra</c>.
        /// Returns null when the plan cannot be resolved (fail closed).
        /// </summary>
        private async Task<ActiveQrAllowance?> ResolveAllowanceAsync(
            BillingAccount? billingAccount,
            CancellationToken ct
        )
        {
            if (
                billingAccount == null
                || string.IsNullOrWhiteSpace(
                    billingAccount.ContractedPricebookId
                )
            )
            {
                return null;
            }

            var key = billingAccount.SubscriptionPlan.Trim().ToLowerInvariant();
            if (key == "custom")
            {
                return await ResolveCustomAllowanceAsync(billingAccount, ct);
            }

            PricebookSnapshot book;
            try
            {
                book = _pricebookCatalog.GetRequired(
                    billingAccount.ContractedPricebookId
                );
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            if (!book.Plans.TryGetValue(key, out var plan))
            {
                return null;
            }

            var additionalPaidLocations = key == "group"
                ? Math.Max(0, billingAccount.PaidExtraLocationCount)
                : 0;
            return new ActiveQrAllowance(
                WorkspaceQrPool.Compute(
                    plan.ActiveQrPlacementsPerLocation,
                    additionalPaidLocations,
                    purchasedExtras: 0
                )
            );
        }

        private async Task<ActiveQrAllowance?> ResolveCustomAllowanceAsync(
            BillingAccount billingAccount,
            CancellationToken ct
        )
        {
            var custom = CustomPlanConfig.TryParse(
                billingAccount.CustomPlanConfigJson
            );
            if (custom == null)
            {
                return null;
            }

            var quantities = custom.Quantities;
            var includedLocations = 0;
            // Only a Group base reads the included Locations (for the +10 per
            // additional paid Location part of the pool).
            if (
                string.Equals(
                    custom.BasePlan?.Trim(),
                    "group",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var resolved = await ResolveGroupIncludedLocationsAsync(
                    custom,
                    billingAccount,
                    ct
                );
                if (resolved == null)
                {
                    return null;
                }

                includedLocations = resolved.Value;
            }

            return new ActiveQrAllowance(
                WorkspaceQrPool.ForCustomPlan(
                    custom.BasePlan,
                    quantities.QrPlacements,
                    quantities.Locations,
                    includedLocations,
                    quantities.QrPlacementsAccountExtra
                )
            );
        }

        /// <summary>
        /// Locations the Group base includes, read from the catalog version the
        /// customer's quote was priced on (<c>CatalogPublicId</c>). Falls back to
        /// the contracted pricebook only when that catalog version has no Group
        /// row. Null when neither source can answer (fail closed).
        /// </summary>
        private async Task<int?> ResolveGroupIncludedLocationsAsync(
            CustomPlanConfig custom,
            BillingAccount billingAccount,
            CancellationToken ct
        )
        {
            var planKey = custom.BasePlan!.Trim().ToLowerInvariant();
            var catalogPublicId = custom.CatalogPublicId?.Trim();
            if (!string.IsNullOrEmpty(catalogPublicId))
            {
                var fromCatalog = await _context.CommercialCatalogVersions
                    .AsNoTracking()
                    .Where(version => version.PublicId == catalogPublicId)
                    .SelectMany(version => version.Plans)
                    .Where(plan => plan.PlanKey == planKey)
                    .Select(plan => (int?)plan.IncludedLocations)
                    .FirstOrDefaultAsync(ct);
                if (fromCatalog != null)
                {
                    return fromCatalog;
                }
            }

            try
            {
                var book = _pricebookCatalog.GetRequired(
                    billingAccount.ContractedPricebookId
                );
                return book.Plans.TryGetValue(planKey, out var basePlan)
                    ? basePlan.IncludedLocations
                    : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
