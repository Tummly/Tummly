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
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly ILogger<ShopQrSlotReservationService> _logger;

        public ShopQrSlotReservationService(
            ApplicationDbContext context,
            IActiveQrCapacity capacity,
            IQrPlacementResolver resolver,
            IMaterialsCatalog catalog,
            TimeProvider clock,
            ISmartGuestLinkService smartGuestLink,
            ILogger<ShopQrSlotReservationService> logger
        )
        {
            _context = context;
            _capacity = capacity;
            _resolver = resolver;
            _catalog = catalog;
            _clock = clock;
            _smartGuestLink = smartGuestLink;
            _logger = logger;
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
                    // A fresh hold: the 24 h review clock starts again.
                    existing.CreatedAtUtc = now;
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

        public async Task<IReadOnlyList<int>> ConvertForPaidOrderAsync(
            ShopOrder order,
            CancellationToken ct
        )
        {
            var types = ShopQrSlotTypes.FromOrderLines(order.Lines, _catalog);
            if (types.Count == 0)
            {
                return [];
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            // Rows the caller already tracked can be stale; refresh them under the lock.
            var trackedBefore = _context.ChangeTracker
                .Entries<ShopQrSlotReservation>()
                .Where(entry => entry.Entity.ShopOrderId == order.Id)
                .Select(entry => entry.Entity.Id)
                .ToHashSet();
            var own = await _context.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == order.Id)
                .ToListAsync(ct);
            foreach (var row in own.Where(row => trackedBefore.Contains(row.Id)))
            {
                await _context.Entry(row).ReloadAsync(ct);
            }

            var qrCodeIds = new List<int>();

            foreach (var type in types)
            {
                var reservation = own.FirstOrDefault(row => row.QrType == type);
                if (
                    reservation is
                    {
                        Status: ShopQrSlotReservationStatuses.Converted,
                        ConvertedQrCodeId: int converted,
                    }
                )
                {
                    qrCodeIds.Add(converted);
                    continue;
                }

                var (qrCodeId, reused) = await ResolveCodeForConversionAsync(
                    order,
                    type,
                    reservation,
                    now,
                    ct
                );
                if (qrCodeId is not int resolvedId)
                {
                    order.ReviewReason =
                        ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment;
                    order.UpdatedAtUtc = now;
                    _logger.LogWarning(
                        "Shop order {ShopOrderId} was paid but the plan has no room for a new {QrType} QR code at location {LocationId}; order moved to review",
                        order.Id,
                        type,
                        order.LocationId
                    );
                    continue;
                }

                if (reservation == null && reused)
                {
                    // An Active code already existed and no slot was held.
                    qrCodeIds.Add(resolvedId);
                    continue;
                }

                if (reservation == null)
                {
                    reservation = new ShopQrSlotReservation
                    {
                        Id = Guid.NewGuid(),
                        ShopOrderId = order.Id,
                        RestaurantLocationId = order.LocationId,
                        QrType = type,
                        ReconcileAfterUtc = now,
                        CreatedAtUtc = now,
                    };
                    _context.ShopQrSlotReservations.Add(reservation);
                    own.Add(reservation);
                }

                reservation.Status = ShopQrSlotReservationStatuses.Converted;
                reservation.ConvertedQrCodeId = resolvedId;
                reservation.UpdatedAtUtc = now;
                qrCodeIds.Add(resolvedId);
            }

            await _context.SaveChangesAsync(ct);
            return qrCodeIds;
        }

        /// <summary>
        /// The Active code a paid order's type lands on: an existing Active
        /// code, a Paused code reactivated (token kept), or a new code. Returns
        /// null when a new slot is needed and the plan has no room.
        /// </summary>
        private async Task<(int? QrCodeId, bool Reused)> ResolveCodeForConversionAsync(
            ShopOrder order,
            QrType type,
            ShopQrSlotReservation? reservation,
            DateTime now,
            CancellationToken ct
        )
        {
            var resolution = await _resolver.ResolveAsync(order.LocationId, type, ct);
            if (resolution is Reuse reuse)
            {
                return (reuse.QrCodeId, true);
            }

            // Any held reservation on this (location, type) already holds the
            // slot, so no limit check applies (spec 4.4.3 step 3).
            var held = await HeldTypesAsync(order.LocationId, ct);
            if (!held.Contains(type))
            {
                var decision = await _capacity.EvaluateAsync(
                    order.RestaurantId,
                    order.LocationId,
                    1,
                    activatingType: null,
                    ct
                );
                if (!decision.EntitlementsAvailable || !decision.Fits)
                {
                    return (null, false);
                }
            }

            var reactivateId = await ValidReactivationIdAsync(
                order.LocationId,
                type,
                reservation?.ReactivateQrCodeId,
                ct
            ) ?? ((NeedsSlot)resolution).ReactivateQrCodeId;
            if (reactivateId is int pausedId)
            {
                var paused = await _context.QrCodes.SingleAsync(
                    row => row.Id == pausedId,
                    ct
                );
                paused.Status = QrCodeStatus.Active;
                paused.UpdatedAt = now;
                return (paused.Id, false);
            }

            var created = new QrCode
            {
                RestaurantLocationId = order.LocationId,
                QrType = type,
                Token = await _smartGuestLink.GenerateTokenAsync(),
                Status = QrCodeStatus.Active,
                CreatedAt = now,
            };
            _context.QrCodes.Add(created);
            // Saved now so the id exists and the next token check sees this one.
            await _context.SaveChangesAsync(ct);
            return (created.Id, false);
        }

        private async Task<int?> ValidReactivationIdAsync(
            int locationId,
            QrType type,
            int? qrCodeId,
            CancellationToken ct
        )
        {
            if (qrCodeId is not int id)
            {
                return null;
            }

            var stillPaused = await _context.QrCodes.AnyAsync(
                row =>
                    row.Id == id
                    && row.RestaurantLocationId == locationId
                    && row.QrType == type
                    && row.Status == QrCodeStatus.Paused
                    && row.ArchivedAt == null,
                ct
            );
            return stillPaused ? id : null;
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
