using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <remarks>
    /// Assistant quotes stay: this delete does not change stored Assistant
    /// messages. Snapshot Name and excerpt remain.
    /// </remarks>
    public class LocationGuestDeleteService : ILocationGuestDeleteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public LocationGuestDeleteService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
        }

        public async Task<LocationGuestDeleteOutcome> DeleteAsync(
            int userId,
            int locationGuestId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var owned = await _ownedLocation.ResolveAsync(userId, locationId);

            switch (owned.Status)
            {
                case OwnedLocationResolveStatus.Found:
                    break;
                case OwnedLocationResolveStatus.NotFound:
                    return LocationGuestDeleteOutcome.LocationNotFound();
                case OwnedLocationResolveStatus.Forbidden:
                    return LocationGuestDeleteOutcome.Forbidden();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(owned),
                        owned.Status,
                        "Unexpected owned-location resolve status."
                    );
            }

            var locationGuest = await _context.LocationGuests
                .FirstOrDefaultAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (locationGuest == null)
            {
                return LocationGuestDeleteOutcome.NotFound();
            }

            await LocationGuestHardDelete.ApplyAsync(
                _context,
                locationGuest,
                cancellationToken
            );
            await _context.SaveChangesAsync(cancellationToken);
            return LocationGuestDeleteOutcome.Deleted();
        }
    }
}
