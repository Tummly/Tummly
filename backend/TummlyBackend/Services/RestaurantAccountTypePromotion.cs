using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RestaurantAccountTypePromotion
        : IRestaurantAccountTypePromotion
    {
        private readonly ApplicationDbContext _context;

        public RestaurantAccountTypePromotion(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> EnsureRoutingAccountTypeAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(
                row => row.Id == restaurantId,
                cancellationToken
            );
            if (restaurant == null)
            {
                return "Single";
            }

            var current = string.IsNullOrWhiteSpace(restaurant.AccountType)
                ? "Single"
                : restaurant.AccountType.Trim();

            if (
                !string.Equals(
                    current,
                    "Single",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return current;
            }

            var switcherEligibleCount = await _context.RestaurantLocations
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && (
                            row.LifecycleStatus
                                == LocationLifecycleStatus.Active
                            || row.LifecycleStatus
                                == LocationLifecycleStatus.Paused
                        ),
                    cancellationToken
                );

            if (switcherEligibleCount < 2)
            {
                return current;
            }

            restaurant.AccountType = "Multi";

            var users = await _context.Users
                .Where(row =>
                    row.SelectedRestaurantId == restaurantId
                    || row.Id == restaurant.OwnerUserId
                )
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.AccountType = "Multi";
            }

            await _context.SaveChangesAsync(cancellationToken);
            return "Multi";
        }
    }
}
