using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestTagBackfillService : IGuestTagBackfillService
    {
        /// <summary>
        /// Keyset page size for Succeeded Feedback loads during startup backfill.
        /// </summary>
        private const int PageSize = 100;

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
            int? afterId = null;

            while (true)
            {
                var query = _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.LocationGuestId != null
                    );

                if (afterId is int cursor)
                {
                    query = query.Where(f => f.Id > cursor);
                }

                var page = await query
                    .OrderBy(f => f.Id)
                    .Take(PageSize)
                    .ToListAsync(cancellationToken);

                if (page.Count == 0)
                {
                    return;
                }

                foreach (var feedback in page)
                {
                    await _guestTagging.UnionDetectedTagsFromFeedbackAsync(
                        feedback,
                        cancellationToken
                    );
                }

                afterId = page[^1].Id;

                if (page.Count < PageSize)
                {
                    return;
                }
            }
        }
    }
}
