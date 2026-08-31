using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class LocationsActivityService : ILocationsActivityService
    {
        public const int MaxItems = 50;

        private readonly ApplicationDbContext _context;

        public LocationsActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetActivityAsync(LocationsActivityQuery query)
        {
            var scopedIds = query.LocationIds.ToHashSet();

            var items = await _context.LocationActivities
                .AsNoTracking()
                .Where(a =>
                    a.RestaurantId == query.RestaurantId
                    && (
                        a.LocationId == null
                        || scopedIds.Contains(a.LocationId.Value)
                    )
                )
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id)
                .Take(MaxItems)
                .Select(a => new
                {
                    id = a.Id,
                    locationId = a.LocationId,
                    kind = a.Kind,
                    description = a.Description,
                    occurredAt = a.OccurredAt.ToUniversalTime().ToString("O"),
                })
                .ToListAsync();

            return new
            {
                success = true,
                items,
            };
        }
    }
}
