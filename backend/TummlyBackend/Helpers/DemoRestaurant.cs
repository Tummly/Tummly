using Microsoft.EntityFrameworkCore;
using TummlyBackend.Configurations;
using TummlyBackend.Data;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Identifies the seeded public product-demo restaurant (owner =
    /// DemoSession:OwnerEmail). Its public QR token must not store visitor
    /// data, even when the demo session itself is switched off.
    /// </summary>
    public static class DemoRestaurant
    {
        public const string NothingSavedMessage =
            "Product demo only. Nothing was saved and no messages are sent.";

        public static async Task<bool> IsDemoRestaurantAsync(
            ApplicationDbContext context,
            DemoSessionOptions options,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var ownerEmail = options.OwnerEmail?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(ownerEmail))
            {
                return false;
            }

            return await context.Restaurants
                .Where(r => r.Id == restaurantId)
                .Join(
                    context.Users,
                    r => r.OwnerUserId,
                    u => u.Id,
                    (_, u) => u.Email
                )
                .AnyAsync(
                    email => email.ToLower() == ownerEmail,
                    cancellationToken
                );
        }
    }
}
