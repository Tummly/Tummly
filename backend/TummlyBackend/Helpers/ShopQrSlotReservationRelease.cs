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
        /// Marks the order's open reservations released on the tracked
        /// context. The caller saves. Returns how many rows changed.
        /// </summary>
        public static async Task<int> ReleaseOpenForOrderAsync(
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
