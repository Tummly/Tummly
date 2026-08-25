using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class OwnedLocationService : IOwnedLocationService
    {
        private readonly ApplicationDbContext _context;

        public OwnedLocationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OwnedLocationResult> ResolveAsync(
            int userId,
            int locationId
        )
        {
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(row => row.Restaurant)
                .FirstOrDefaultAsync(row => row.Id == locationId);

            if (location == null)
            {
                return new OwnedLocationResult
                {
                    Status = OwnedLocationResolveStatus.NotFound
                };
            }

            var scoped = await ListScopedLocationIdsAsync(
                location.RestaurantId,
                userId
            );

            if (!scoped.Contains(locationId))
            {
                return new OwnedLocationResult
                {
                    Status = OwnedLocationResolveStatus.Forbidden
                };
            }

            return new OwnedLocationResult
            {
                Status = OwnedLocationResolveStatus.Found,
                Location = location
            };
        }

        public Task<IReadOnlyList<int>> ListOwnedLocationIdsAsync(
            int restaurantId,
            int userId,
            CancellationToken cancellationToken = default
        )
        {
            return ListScopedLocationIdsAsync(
                restaurantId,
                userId,
                cancellationToken
            );
        }

        private async Task<IReadOnlyList<int>> ListScopedLocationIdsAsync(
            int restaurantId,
            int userId,
            CancellationToken cancellationToken = default
        )
        {
            var membership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.UserId == userId
                        && row.RestaurantId == restaurantId
                        && row.Status == MembershipStatus.Active,
                    cancellationToken
                );

            if (membership != null)
            {
                if (
                    membership.LocationScope == LocationScopeKind.NamedList
                    && MembershipLocationScope
                        .ParseNamedIds(membership.NamedLocationIdsJson)
                        .Count == 0
                )
                {
                    return [];
                }

                return await FilterLiveIdsAsync(
                    restaurantId,
                    membership,
                    cancellationToken
                );
            }

            var hasMembershipRow = await _context.RestaurantMemberships
                .AsNoTracking()
                .AnyAsync(row => row.UserId == userId, cancellationToken);

            if (hasMembershipRow)
            {
                return [];
            }

            var ownsRestaurant = await _context.Restaurants
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == restaurantId && row.OwnerUserId == userId,
                    cancellationToken
                );

            if (!ownsRestaurant)
            {
                return [];
            }

            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<int>> FilterLiveIdsAsync(
            int restaurantId,
            RestaurantMembership membership,
            CancellationToken cancellationToken
        )
        {
            var live = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);

            if (membership.LocationScope != LocationScopeKind.NamedList)
            {
                return live;
            }

            var named = MembershipLocationScope
                .ParseNamedIds(membership.NamedLocationIdsJson)
                .ToHashSet();

            return live.Where(named.Contains).ToList();
        }
    }
}
