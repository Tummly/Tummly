using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Resolve the live Revolut subscription id from pending pay sessions
    /// (tickets 20 / 21).
    /// </summary>
    internal static class RevolutSubscriptionCorrelation
    {
        public static Task<string?> ResolveLatestSubscriptionIdAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return context.RevolutPendingPaySessions
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.RevolutSubscriptionId != ""
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => row.RevolutSubscriptionId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
