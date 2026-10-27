using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Shop;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Shop checkout hold on QR capacity (spec 4.4). Uses the shared
    /// <see cref="IActiveQrCapacity"/> count and the billing-account lock that
    /// Capture takes.
    /// </summary>
    public sealed class ShopQrSlotReservationService : IShopQrSlotReservationService
    {
        public static readonly TimeSpan ReconcileDelay = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _context;
        private readonly IActiveQrCapacity _capacity;
        private readonly IQrPlacementResolver _resolver;
        private readonly IMaterialsCatalog _catalog;
        private readonly TimeProvider _clock;

        public ShopQrSlotReservationService(
            ApplicationDbContext context,
            IActiveQrCapacity capacity,
            IQrPlacementResolver resolver,
            IMaterialsCatalog catalog,
            TimeProvider clock
        )
        {
            _context = context;
            _capacity = capacity;
            _resolver = resolver;
            _catalog = catalog;
            _clock = clock;
        }

        public async Task<QrCapacityCheck> CheckForPlaceAsync(
            int restaurantId,
            int locationId,
            IReadOnlyCollection<QrType> types,
            CancellationToken ct
        )
        {
            var needsSlot = await ResolveNeedsSlotAsync(locationId, types, ct);
            var held = await HeldTypesAsync(locationId, ct);
            var needed = needsSlot.Count(slot => !held.Contains(slot.QrType));

            return await EvaluateAsync(restaurantId, locationId, needed, ct);
        }

        public async Task<QrCapacityCheck> ReserveForPayAsync(
            ShopOrder order,
            CancellationToken ct
        )
        {
            var types = ShopQrSlotTypes.FromOrderLines(order.Lines, _catalog);
            if (types.Count == 0)
            {
                return new QrCapacityCheck(true, 0);
            }

            await using var transaction =
                await QrCapacityTransaction.BeginIfNeededAsync(_context, ct);

            // One lock, held to the commit, for the count and the write.
            await _capacity.LockBillingAccountAsync(order.RestaurantId, ct);

            var needsSlot = await ResolveNeedsSlotAsync(order.LocationId, types, ct);
            if (needsSlot.Count == 0)
            {
                return new QrCapacityCheck(true, 0);
            }

            var held = await HeldTypesAsync(order.LocationId, ct);
            var needed = needsSlot.Count(slot => !held.Contains(slot.QrType));
            var check = await EvaluateAsync(
                order.RestaurantId,
                order.LocationId,
                needed,
                ct
            );
            if (!check.Ok)
            {
                // Nothing was written. Disposing the transaction ends the lock.
                return check;
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var own = await _context.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == order.Id)
                .ToListAsync(ct);

            foreach (var slot in needsSlot)
            {
                var existing = own.FirstOrDefault(row => row.QrType == slot.QrType);
                if (existing == null)
                {
                    _context.ShopQrSlotReservations.Add(
                        new ShopQrSlotReservation
                        {
                            Id = Guid.NewGuid(),
                            ShopOrderId = order.Id,
                            RestaurantLocationId = order.LocationId,
                            QrType = slot.QrType,
                            ReactivateQrCodeId = slot.ReactivateQrCodeId,
                            Status = ShopQrSlotReservationStatuses.Open,
                            ReconcileAfterUtc = now + ReconcileDelay,
                            CreatedAtUtc = now,
                            UpdatedAtUtc = now,
                        }
                    );
                }
                else if (
                    existing.Status == ShopQrSlotReservationStatuses.Released
                )
                {
                    // The sweeper released it before this later pay.
                    existing.Status = ShopQrSlotReservationStatuses.Open;
                    existing.ReactivateQrCodeId = slot.ReactivateQrCodeId;
                    existing.ReconcileAfterUtc = now + ReconcileDelay;
                    existing.ReconcileAttempts = 0;
                    existing.LastReconcileAtUtc = null;
                    existing.LastReconcileError = null;
                    existing.UpdatedAtUtc = now;
                }
            }

            await _context.SaveChangesAsync(ct);
            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }

            return check;
        }

        public async Task ReleaseForOrderAsync(Guid shopOrderId, CancellationToken ct)
        {
            var released = await ShopQrSlotReservationRelease.ReleaseOpenForOrderAsync(
                _context,
                shopOrderId,
                _clock.GetUtcNow().UtcDateTime,
                ct
            );
            if (released > 0)
            {
                await _context.SaveChangesAsync(ct);
            }
        }

        private async Task<QrCapacityCheck> EvaluateAsync(
            int restaurantId,
            int locationId,
            int needed,
            CancellationToken ct
        )
        {
            if (needed <= 0)
            {
                return new QrCapacityCheck(true, 0);
            }

            var decision = await _capacity.EvaluateAsync(
                restaurantId,
                locationId,
                needed,
                activatingType: null,
                ct
            );
            return new QrCapacityCheck(
                decision.EntitlementsAvailable && decision.Fits,
                decision.PerLocationCap,
                decision.EntitlementsAvailable
            );
        }

        private async Task<List<NeedsSlotType>> ResolveNeedsSlotAsync(
            int locationId,
            IEnumerable<QrType> types,
            CancellationToken ct
        )
        {
            var result = new List<NeedsSlotType>();
            foreach (var type in types.Distinct())
            {
                var resolution = await _resolver.ResolveAsync(locationId, type, ct);
                if (resolution is NeedsSlot needsSlot)
                {
                    result.Add(new NeedsSlotType(type, needsSlot.ReactivateQrCodeId));
                }
            }

            return result;
        }

        private async Task<HashSet<QrType>> HeldTypesAsync(
            int locationId,
            CancellationToken ct
        )
        {
            var held = await _context.ShopQrSlotReservations
                .Where(row =>
                    row.RestaurantLocationId == locationId
                    && (
                        row.Status == ShopQrSlotReservationStatuses.Open
                        || row.Status == ShopQrSlotReservationStatuses.Review
                    )
                )
                .Select(row => row.QrType)
                .Distinct()
                .ToListAsync(ct);
            return held.ToHashSet();
        }

        private sealed record NeedsSlotType(QrType QrType, int? ReactivateQrCodeId);
    }
}
