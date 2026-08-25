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
            IReadOnlyList<int> scopedLocationIds,
            RestaurantLocation shellLocation,
            string? locationScope,
            int[]? locationIds
        )
        {
            var allowed = scopedLocationIds.ToHashSet();
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
                if (!allowed.Contains(shellLocation.Id))
                {
                    return GuestsEffectiveLocationResult.Forbidden(
                        "You do not have access to this location."
                    );
                }

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

                var scoped = await _context.RestaurantLocations
                    .AsNoTracking()
                    .Where(row =>
                        row.RestaurantId == shellLocation.RestaurantId
                        && allowed.Contains(row.Id)
                    )
                    .Select(row => new { row.Id, row.LocationName })
                    .ToListAsync();

                return GuestsEffectiveLocationResult.Ok(
                    scoped.Select(row => row.Id).ToList(),
                    scoped.ToDictionary(row => row.Id, row => row.LocationName)
                );
            }

            var distinctIds = locationIds!.Distinct().ToList();
            var found = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => distinctIds.Contains(row.Id))
                .Select(row => new
                {
                    row.Id,
                    row.RestaurantId,
                    row.LocationName,
                })
                .ToListAsync();

            if (found.Count != distinctIds.Count)
            {
                return GuestsEffectiveLocationResult.NotFound(
                    "Location not found."
                );
            }

            if (found.Any(row =>
                row.RestaurantId != shellLocation.RestaurantId
                || !allowed.Contains(row.Id)
            ))
            {
                return GuestsEffectiveLocationResult.Forbidden(
                    "You do not have access to this location."
                );
            }

            return GuestsEffectiveLocationResult.Ok(
                found.Select(row => row.Id).ToList(),
                found.ToDictionary(row => row.Id, row => row.LocationName)
            );
        }
    }
}
