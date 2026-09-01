using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OwnedLocation;
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

            var masterGuestId = locationGuest.MasterGuestId;

            // Feedback FK is NoAction — unlink before removing the Location Guest.
            var feedbacks = await _context.Feedbacks
                .Where(f => f.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            foreach (var feedback in feedbacks)
            {
                feedback.LocationGuestId = null;
            }

            // Explicit removals: activity/tag FKs are NoAction (SQL Server
            // cascade-path limits); notes cascade in SQL. InMemory does not
            // enforce cascade — application policy deletes all.
            var activityEvents = await _context.LocationGuestActivityEvents
                .Where(e => e.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var permissionLedgerEntries =
                await _context.LocationGuestPermissionLedgerEntries
                    .Where(e => e.LocationGuestId == locationGuestId)
                    .ToListAsync(cancellationToken);
            var notes = await _context.LocationGuestNotes
                .Where(n => n.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var tagMemberships = await _context.LocationGuestTags
                .Where(m => m.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            _context.LocationGuestActivityEvents.RemoveRange(activityEvents);
            _context.LocationGuestPermissionLedgerEntries.RemoveRange(
                permissionLedgerEntries
            );
            _context.LocationGuestNotes.RemoveRange(notes);
            _context.LocationGuestTags.RemoveRange(tagMemberships);
            _context.LocationGuests.Remove(locationGuest);

            var otherLocationGuestsRemain = await _context.LocationGuests
                .AnyAsync(
                    lg =>
                        lg.MasterGuestId == masterGuestId
                        && lg.Id != locationGuestId,
                    cancellationToken
                );

            if (!otherLocationGuestsRemain)
            {
                var master = await _context.MasterGuests
                    .FirstOrDefaultAsync(
                        m => m.Id == masterGuestId,
                        cancellationToken
                    );

                if (master != null)
                {
                    _context.MasterGuests.Remove(master);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return LocationGuestDeleteOutcome.Deleted();
        }
    }
}
