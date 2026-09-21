using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Stages hard-delete for one Location Guest (no SaveChanges).
    /// Unlinks Feedback; removes notes/tags/activity/ledger; removes guest;
    /// removes orphan Master Guest.
    /// </summary>
    internal static class LocationGuestHardDelete
    {
        public static async Task ApplyAsync(
            ApplicationDbContext context,
            LocationGuest locationGuest,
            CancellationToken cancellationToken = default
        )
        {
            var locationGuestId = locationGuest.Id;
            var masterGuestId = locationGuest.MasterGuestId;

            // Feedback FK is NoAction — unlink before removing the Location Guest.
            var feedbacks = await context.Feedbacks
                .Where(f => f.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            foreach (var feedback in feedbacks)
            {
                feedback.LocationGuestId = null;
            }

            // Explicit removals: activity/tag FKs are NoAction (SQL Server
            // cascade-path limits); notes cascade in SQL. InMemory does not
            // enforce cascade — application policy deletes all.
            var activityEvents = await context.LocationGuestActivityEvents
                .Where(e => e.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var permissionLedgerEntries =
                await context.LocationGuestPermissionLedgerEntries
                    .Where(e => e.LocationGuestId == locationGuestId)
                    .ToListAsync(cancellationToken);
            var notes = await context.LocationGuestNotes
                .Where(n => n.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var tagMemberships = await context.LocationGuestTags
                .Where(m => m.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            context.LocationGuestActivityEvents.RemoveRange(activityEvents);
            context.LocationGuestPermissionLedgerEntries.RemoveRange(
                permissionLedgerEntries
            );
            context.LocationGuestNotes.RemoveRange(notes);
            context.LocationGuestTags.RemoveRange(tagMemberships);
            context.LocationGuests.Remove(locationGuest);

            var otherLocationGuestsRemain = await context.LocationGuests
                .AnyAsync(
                    lg =>
                        lg.MasterGuestId == masterGuestId
                        && lg.Id != locationGuestId,
                    cancellationToken
                );

            if (!otherLocationGuestsRemain)
            {
                var master = await context.MasterGuests
                    .FirstOrDefaultAsync(
                        m => m.Id == masterGuestId,
                        cancellationToken
                    );

                if (master != null)
                {
                    context.MasterGuests.Remove(master);
                }
            }
        }
    }
}
