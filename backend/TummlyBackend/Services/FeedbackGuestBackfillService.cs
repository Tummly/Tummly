using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackGuestBackfillService : IFeedbackGuestBackfillService
    {
        private const int SaveBatchSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly IGuestUpsertService _guestUpsert;

        public FeedbackGuestBackfillService(
            ApplicationDbContext context,
            IGuestUpsertService guestUpsert
        )
        {
            _context = context;
            _guestUpsert = guestUpsert;
        }

        public async Task BackfillAsync(CancellationToken cancellationToken = default)
        {
            // Cheap satisfied gate: skip heavy catch-up when every Feedback
            // already has a LocationGuestId (EXISTS-style AnyAsync).
            var hasUnlinked = await _context.Feedbacks
                .AsNoTracking()
                .AnyAsync(f => f.LocationGuestId == null, cancellationToken);

            if (!hasUnlinked)
            {
                return;
            }

            var unlinked = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => f.LocationGuestId == null)
                .OrderBy(f => f.CreatedAt)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            var pendingSaves = 0;

            foreach (var feedbackId in unlinked)
            {
                await EnsurePendingGuestsPersistedAsync(cancellationToken);

                var feedback = await _context.Feedbacks
                    .Include(f => f.RestaurantLocation)
                    .FirstAsync(f => f.Id == feedbackId, cancellationToken);

                if (feedback.LocationGuestId != null)
                {
                    continue;
                }

                var location = feedback.RestaurantLocation
                    ?? throw new InvalidOperationException(
                        $"Feedback {feedback.Id} has no RestaurantLocation."
                    );

                var locationGuest = await _guestUpsert.ResolveOrCreateAsync(
                    location.RestaurantId,
                    location.Id,
                    feedback.GuestName,
                    feedback.GuestContact,
                    feedback.ContactType,
                    feedback.OffersOptOut,
                    feedback.CreatedAt,
                    cancellationToken
                );

                feedback.LocationGuest = locationGuest;
                pendingSaves++;

                if (pendingSaves >= SaveBatchSize)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    pendingSaves = 0;
                }
            }

            if (pendingSaves > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task EnsurePendingGuestsPersistedAsync(
            CancellationToken cancellationToken
        )
        {
            var hasPendingGuests = _context.ChangeTracker
                .Entries<MasterGuest>()
                .Any(entry => entry.State == EntityState.Added)
                || _context.ChangeTracker
                    .Entries<LocationGuest>()
                    .Any(entry => entry.State == EntityState.Added);

            if (!hasPendingGuests)
            {
                return;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
