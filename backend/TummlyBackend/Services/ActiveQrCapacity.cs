using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Shared active-QR capacity rule (spec 4.4.2). Capture and Shop checkout
    /// use this one service, so both count slots the same way and lock the same
    /// <c>BillingAccounts</c> row.
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

        public Task<ActiveQrAllowance?> GetAllowanceAsync(
            BillingAccount? account,
            int locationId,
            CancellationToken ct
        )
        {
            // The plan cap is account-wide; locationId does not change it.
            return Task.FromResult(ResolveAllowance(account));
        }

        public async Task<int> CountUsedAsync(int locationId, CancellationToken ct)
        {
            var usedByLocation = await CountUsedByLocationAsync(
                new List<int> { locationId },
                ct
            );

            return usedByLocation.GetValueOrDefault(locationId);
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
                    PerLocationCap: 0,
                    Used: 0
                );
            }

            var billingAccount = await LockBillingAccountAsync(restaurantId, ct);
            var allowance = await GetAllowanceAsync(billingAccount, locationId, ct);
            if (allowance == null)
            {
                return new ActiveQrCapacityDecision(
                    EntitlementsAvailable: false,
                    Fits: false,
                    PerLocationCap: 0,
                    Used: 0
                );
            }

            var perLocCap = allowance.PerLocationCap;
            var used = await CountUsedAsync(locationId, ct);

            // Activating a type with a held slot takes over that slot, so it leaves the count.
            var takenOverHeld = activatingType is { } type
                ? 1 - await ActivationCostAsync(locationId, type, ct)
                : 0;
            var usedBefore = used - takenOverHeld;

            if (usedBefore + increment <= perLocCap)
            {
                return new ActiveQrCapacityDecision(true, true, perLocCap, used);
            }

            if (allowance.AccountExtra <= 0)
            {
                return new ActiveQrCapacityDecision(true, false, perLocCap, used);
            }

            // Account-wide pool: Σ max(0, used_l − perLocCap) may use accountExtra.
            var locationIds = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .ToListAsync(ct);

            var usedByLocation = await CountUsedByLocationAsync(locationIds, ct);

            var totalOverflow = 0;
            foreach (var locId in locationIds)
            {
                var count = usedByLocation.GetValueOrDefault(locId);
                if (locId == locationId)
                {
                    count = usedBefore + increment;
                }

                totalOverflow += Math.Max(0, count - perLocCap);
            }

            var fits = totalOverflow <= allowance.AccountExtra;
            return new ActiveQrCapacityDecision(true, fits, perLocCap, used);
        }

        /// <summary>
        /// Spec 4.4.2 per location: Active codes plus each distinct held
        /// reservation QR type that has no Active code of that type.
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

        private ActiveQrAllowance? ResolveAllowance(BillingAccount? billingAccount)
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
                var custom = CustomPlanConfig.TryParse(
                    billingAccount.CustomPlanConfigJson
                );
                if (custom == null)
                {
                    return null;
                }

                return new ActiveQrAllowance(
                    Math.Max(0, custom.Quantities.QrPlacements),
                    Math.Max(0, custom.Quantities.QrPlacementsAccountExtra)
                );
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

            return new ActiveQrAllowance(plan.ActiveQrPlacementsPerLocation, 0);
        }
    }
}
