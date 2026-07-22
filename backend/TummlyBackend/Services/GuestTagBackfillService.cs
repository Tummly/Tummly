using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestTagBackfillService : IGuestTagBackfillService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestTaggingService _guestTagging;

        public GuestTagBackfillService(
            ApplicationDbContext context,
            IGuestTaggingService guestTagging
        )
        {
            _context = context;
            _guestTagging = guestTagging;
        }

        public async Task BackfillAsync(
            CancellationToken cancellationToken = default
        )
        {
            var feedbackIds = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.LocationGuestId != null
                )
                .OrderBy(f => f.Id)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            foreach (var feedbackId in feedbackIds)
            {
                var feedback = await _context.Feedbacks
                    .FirstAsync(f => f.Id == feedbackId, cancellationToken);

                if (
                    feedback.ClassificationStatus
                        != ClassificationStatus.Succeeded
                    || feedback.LocationGuestId is null
                )
                {
                    continue;
                }

                await _guestTagging.UnionDetectedTagsFromFeedbackAsync(
                    feedback,
                    cancellationToken
                );
            }
        }
    }
}
