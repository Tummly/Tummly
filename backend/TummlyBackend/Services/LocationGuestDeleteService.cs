using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class LocationGuestDeleteService : ILocationGuestDeleteService
    {
        private readonly ApplicationDbContext _context;

        public LocationGuestDeleteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LocationGuestDeleteOutcome> DeleteAsync(
            int locationGuestId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
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

            // Explicit removals: activity FK is SetNull; notes/tags cascade in SQL
            // but InMemory does not enforce cascade — application policy deletes all.
            var activityEvents = await _context.LocationGuestActivityEvents
                .Where(e => e.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var notes = await _context.LocationGuestNotes
                .Where(n => n.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var tagMemberships = await _context.LocationGuestTags
                .Where(m => m.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            _context.LocationGuestActivityEvents.RemoveRange(activityEvents);
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
