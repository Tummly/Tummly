using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Releases a Shop order's <c>open</c> QR slot reservations. Static so the
    /// payment-failure helper (no DI) and the cancel service share one rule.
    /// A <c>converted</c> reservation is final and a <c>review</c> reservation
    /// stays held, so only <c>open</c> rows change.
    /// </summary>
    public static class ShopQrSlotReservationRelease
    {
        /// <summary>
        /// Releases the order's <c>open</c> reservations and returns how many
        /// rows changed. On a relational database this is one conditional
        /// UPDATE (<c>WHERE Status = 'open'</c>) that runs at once, so a row
        /// that another writer converted meanwhile is never overwritten. Rows
        /// the context already tracks are refreshed. The in-memory test
        /// provider has no such update and uses tracked changes the caller
        /// saves.
        /// </summary>
        public static async Task<int> ReleaseOpenForOrderAsync(
            ApplicationDbContext db,
            Guid shopOrderId,
            DateTime nowUtc,
            CancellationToken ct
        )
        {
            if (!db.Database.IsRelational())
            {
                return await ReleaseTrackedAsync(db, shopOrderId, nowUtc, ct);
            }

            var changed = await db.ShopQrSlotReservations
                .Where(row =>
                    row.ShopOrderId == shopOrderId
                    && row.Status == ShopQrSlotReservationStatuses.Open
                )
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            row => row.Status,
                            ShopQrSlotReservationStatuses.Released
                        )
                        .SetProperty(row => row.UpdatedAtUtc, nowUtc),
                    ct
                );

            // The update bypassed the change tracker, so refresh tracked rows.
            foreach (
                var entry in db.ChangeTracker
                    .Entries<ShopQrSlotReservation>()
                    .Where(entry =>
                        entry.Entity.ShopOrderId == shopOrderId
                        && entry.State == EntityState.Unchanged
                    )
                    .ToList()
            )
            {
                await entry.ReloadAsync(ct);
            }

            return changed;
        }

        private static async Task<int> ReleaseTrackedAsync(
            ApplicationDbContext db,
            Guid shopOrderId,
            DateTime nowUtc,
            CancellationToken ct
        )
        {
            var open = await db.ShopQrSlotReservations
                .Where(row =>
                    row.ShopOrderId == shopOrderId
                    && row.Status == ShopQrSlotReservationStatuses.Open
                )
                .ToListAsync(ct);

            foreach (var row in open)
            {
                row.Status = ShopQrSlotReservationStatuses.Released;
                row.UpdatedAtUtc = nowUtc;
            }

            return open.Count;
        }
    }
}
