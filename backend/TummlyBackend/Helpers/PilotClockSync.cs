using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Writes the Pilot clock (<see cref="User.ActivationExpiresAt"/>) for the
    /// owner and every active member of a restaurant. Members copy the owner
    /// clock, so both must move together. Methods track changes on
    /// <paramref name="context"/> and do not save; the caller owns the save.
    /// </summary>
    public static class PilotClockSync
    {
        public static Task SetForRestaurantAsync(
            ApplicationDbContext context,
            int restaurantId,
            DateTime endsAtUtc,
            CancellationToken cancellationToken
        ) => SetAsync(context, restaurantId, endsAtUtc, cancellationToken);

        public static Task ClearForRestaurantAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken
        ) => SetAsync(context, restaurantId, null, cancellationToken);

        private static async Task SetAsync(
            ApplicationDbContext context,
            int restaurantId,
            DateTime? expiresAtUtc,
            CancellationToken cancellationToken
        )
        {
            var ownerUserId = await context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == restaurantId)
                .Select(row => row.OwnerUserId)
                .FirstOrDefaultAsync(cancellationToken);

            var memberUserIds = await context.RestaurantMemberships
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                )
                .Select(row => row.UserId)
                .ToListAsync(cancellationToken);

            var userIds = memberUserIds.Append(ownerUserId).Distinct().ToList();
            var users = await context.Users
                .Where(row => userIds.Contains(row.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.ActivationExpiresAt = expiresAtUtc;
            }
        }
    }
}
