using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Interfaces;

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
                .Include(l => l.Restaurant)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                return new OwnedLocationResult
                {
                    Status = OwnedLocationResolveStatus.NotFound
                };
            }

            if (
                location.Restaurant == null
                || location.Restaurant.OwnerUserId != userId
            )
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
    }
}
