using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestsEffectiveLocationService : IGuestsEffectiveLocationService
    {
        private readonly ApplicationDbContext _context;

        public GuestsEffectiveLocationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string ResolveScopeToken(
            string? locationScope,
            int[]? locationIds,
            int shellLocationId
        )
        {
            if (!string.IsNullOrWhiteSpace(locationScope)
                && locationScope.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return "all";
            }

            if (locationIds is { Length: > 1 })
            {
                return "multi";
            }

            if (locationIds is { Length: 1 })
            {
                return locationIds[0].ToString();
            }

            return shellLocationId.ToString();
        }

        public async Task<GuestsEffectiveLocationResult> ResolveAsync(
            int ownerUserId,
            RestaurantLocation shellLocation,
            string? locationScope,
            int[]? locationIds
        )
        {
            var hasScope = !string.IsNullOrWhiteSpace(locationScope);
            var hasIds = locationIds is { Length: > 0 };

            if (hasScope && hasIds)
            {
                throw new ArgumentException(
                    "locationScope and locationIds are mutually exclusive."
                );
            }

            if (!hasScope && !hasIds)
            {
                return GuestsEffectiveLocationResult.Ok(
                    new[] { shellLocation.Id },
                    new Dictionary<int, string>
                    {
                        [shellLocation.Id] = shellLocation.LocationName,
                    }
                );
            }

            if (hasScope)
            {
                if (!string.Equals(
                    locationScope,
                    "all",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    throw new ArgumentException("Invalid locationScope.");
                }

                var allLocations = await _context.RestaurantLocations
                    .AsNoTracking()
                    .Include(l => l.Restaurant)
                    .Where(l =>
                        l.RestaurantId == shellLocation.RestaurantId
                        && l.Restaurant!.OwnerUserId == ownerUserId
                    )
                    .Select(l => new { l.Id, l.LocationName })
                    .ToListAsync();

                return GuestsEffectiveLocationResult.Ok(
                    allLocations.Select(l => l.Id).ToList(),
                    allLocations.ToDictionary(l => l.Id, l => l.LocationName)
                );
            }

            var distinctIds = locationIds!.Distinct().ToList();
            var owned = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(l => l.Restaurant)
                .Where(l =>
                    distinctIds.Contains(l.Id)
                    && l.Restaurant!.OwnerUserId == ownerUserId
                )
                .Select(l => new { l.Id, l.LocationName })
                .ToListAsync();

            if (owned.Count != distinctIds.Count)
            {
                return GuestsEffectiveLocationResult.Forbidden(
                    "You do not have access to this location."
                );
            }

            return GuestsEffectiveLocationResult.Ok(
                owned.Select(l => l.Id).ToList(),
                owned.ToDictionary(l => l.Id, l => l.LocationName)
            );
        }
    }
}
