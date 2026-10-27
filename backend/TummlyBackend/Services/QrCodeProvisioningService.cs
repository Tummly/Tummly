using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Services
{
    public class QrCodeProvisioningService : IQrCodeProvisioningService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IActiveQrCapacity _activeQrCapacity;

        public QrCodeProvisioningService(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink,
            IActiveQrCapacity activeQrCapacity
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
            _activeQrCapacity = activeQrCapacity;
        }

        public async Task<IReadOnlyList<QrCode>> MintDefaultQrCodesAsync(
            RestaurantLocation location
        )
        {
            var starterTypes = await StarterQrMaterialTypes.LoadAsync(_context);
            if (starterTypes.Count == 0)
            {
                return Array.Empty<QrCode>();
            }

            // Free (no plan) mints Paused; Pilot / paid activation or Pilot start
            // activates plan-paused codes within cap (WS3b). With a plan, codes mint
            // Active only within the Workspace headroom (pool - used, Commercial V4);
            // the rest mint Paused and use no capacity. Codes of this batch are not
            // saved yet, so headroom is read once and spent as the batch mints.
            var account = await LoadBillingAccountAsync(location);
            var headroom = await ActiveHeadroomAsync(account, location);

            var existingTypes = await LoadExistingPlacementTypesAsync(location);
            var qrCodes = new List<QrCode>();

            foreach (var qrType in starterTypes)
            {
                if (existingTypes.Contains(qrType))
                {
                    continue;
                }

                // Generated + added one at a time so each subsequent
                // GenerateTokenAsync's change-tracker uniqueness check sees
                // every token already minted in this batch.
                var token = await _smartGuestLink.GenerateTokenAsync();

                var status = QrCodeStatus.Paused;
                if (headroom is null)
                {
                    status = QrCodeStatus.Active;
                }
                else if (headroom > 0)
                {
                    status = QrCodeStatus.Active;
                    headroom--;
                }

                var qrCode = new QrCode
                {
                    RestaurantLocation = location,
                    QrType = qrType,
                    Token = token,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.QrCodes.Add(qrCode);
                qrCodes.Add(qrCode);
                existingTypes.Add(qrType);
            }

            return qrCodes;
        }

        /// <summary>
        /// Active codes this mint may still create. Null means no limit applies
        /// (no billing account to check). Zero means every code mints Paused: no
        /// plan, or the plan cannot be resolved (fail closed).
        /// </summary>
        private async Task<int?> ActiveHeadroomAsync(
            BillingAccount? account,
            RestaurantLocation location
        )
        {
            if (account == null)
            {
                return null;
            }

            if (!OperatorPlanGate.HasPlan(account))
            {
                return 0;
            }

            var allowance = await _activeQrCapacity.GetAllowanceAsync(
                account,
                CancellationToken.None
            );
            if (allowance == null)
            {
                return 0;
            }

            var restaurantId = account.RestaurantId;
            var used = await _activeQrCapacity.CountUsedForRestaurantAsync(
                restaurantId,
                CancellationToken.None
            );
            used += CountUnsavedActiveCodes(restaurantId, location);

            return Math.Max(0, allowance.WorkspacePool - used);
        }

        /// <summary>
        /// Active codes already added to the context for this Workspace but not
        /// saved (earlier Locations of the same signup batch).
        /// </summary>
        private int CountUnsavedActiveCodes(
            int restaurantId,
            RestaurantLocation location
        )
        {
            var count = 0;
            foreach (var entry in _context.ChangeTracker.Entries<QrCode>())
            {
                if (
                    entry.State != EntityState.Added
                    || entry.Entity.Status != QrCodeStatus.Active
                )
                {
                    continue;
                }

                var owner = entry.Entity.RestaurantLocation;
                var ownerRestaurantId = owner?.RestaurantId ?? 0;
                if (owner != null && ReferenceEquals(owner, location))
                {
                    ownerRestaurantId = restaurantId;
                }

                if (ownerRestaurantId == restaurantId)
                {
                    count++;
                }
            }

            return count;
        }

        private async Task<BillingAccount?> LoadBillingAccountAsync(
            RestaurantLocation location
        )
        {
            var restaurantId = location.RestaurantId != 0
                ? location.RestaurantId
                : location.Restaurant?.Id ?? 0;
            if (restaurantId == 0)
            {
                return null;
            }

            // Signup adds the account to the change tracker before it is saved.
            foreach (var entry in _context.ChangeTracker.Entries<BillingAccount>())
            {
                if (
                    entry.State is EntityState.Deleted or EntityState.Detached
                )
                {
                    continue;
                }

                if (entry.Entity.RestaurantId == restaurantId)
                {
                    return entry.Entity;
                }
            }

            return await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
        }

        private async Task<HashSet<QrType>> LoadExistingPlacementTypesAsync(
            RestaurantLocation location
        )
        {
            var existing = new HashSet<QrType>();

            foreach (var entry in _context.ChangeTracker.Entries<QrCode>())
            {
                if (
                    entry.State is EntityState.Deleted or EntityState.Detached
                )
                {
                    continue;
                }

                var row = entry.Entity;
                var sameLocation =
                    ReferenceEquals(row.RestaurantLocation, location)
                    || (
                        location.Id != 0
                        && row.RestaurantLocationId == location.Id
                    );
                if (
                    !sameLocation
                    || row.Status == QrCodeStatus.Archived
                )
                {
                    continue;
                }

                existing.Add(row.QrType);
            }

            if (location.Id == 0)
            {
                return existing;
            }

            var persisted = await _context.QrCodes
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantLocationId == location.Id
                    && row.Status != QrCodeStatus.Archived
                )
                .Select(row => row.QrType)
                .ToListAsync();
            foreach (var qrType in persisted)
            {
                existing.Add(qrType);
            }

            return existing;
        }
    }
}
