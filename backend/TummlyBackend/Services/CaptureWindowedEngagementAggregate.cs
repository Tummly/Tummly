using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Shared windowed engagement aggregate kernel: Active/Paused QR scoping and
    /// date-windowed scan / feedback counts for Capture overview and snapshot.
    /// </summary>
    public class CaptureWindowedEngagementAggregate
    {
        private readonly ApplicationDbContext _context;

        public CaptureWindowedEngagementAggregate(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> ListActiveOrPausedQrCodeIdsAsync(
            IReadOnlyList<int> locationIds
        )
        {
            if (locationIds.Count == 0)
            {
                return [];
            }

            return await _context.QrCodes
                .Where(q =>
                    locationIds.Contains(q.RestaurantLocationId)
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .Select(q => q.Id)
                .ToListAsync();
        }

        public async Task<int> CountScansAsync(
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> activeOrPausedQrCodeIds,
            DateTime fromUtc,
            DateTime toUtc
        )
        {
            if (activeOrPausedQrCodeIds.Count == 0)
            {
                return 0;
            }

            return await _context.QrScanEvents
                .CountAsync(e =>
                    locationIds.Contains(e.RestaurantLocationId)
                    && e.QrCodeId != null
                    && activeOrPausedQrCodeIds.Contains(e.QrCodeId.Value)
                    && e.CreatedAt >= fromUtc
                    && e.CreatedAt < toUtc
                );
        }

        public async Task<int> CountFeedbackAsync(
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> activeOrPausedQrCodeIds,
            DateTime fromUtc,
            DateTime toUtc,
            bool marketingOptInOnly
        )
        {
            if (activeOrPausedQrCodeIds.Count == 0)
            {
                return 0;
            }

            var query = _context.Feedbacks.Where(f =>
                locationIds.Contains(f.RestaurantLocationId)
                && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                && f.CreatedAt >= fromUtc
                && f.CreatedAt < toUtc
            );

            if (marketingOptInOnly)
            {
                query = query.Where(f => !f.OffersOptOut);
            }

            return await query.CountAsync();
        }

        public async Task<
            List<(int QrCodeId, int Count)>
        > GroupScansByQrCodeAsync(
            IReadOnlyList<int> qrCodeIds,
            DateTime fromUtc,
            DateTime toUtc
        )
        {
            if (qrCodeIds.Count == 0)
            {
                return [];
            }

            var rows = await _context.QrScanEvents
                .AsNoTracking()
                .Where(e =>
                    e.QrCodeId != null
                    && qrCodeIds.Contains(e.QrCodeId.Value)
                    && e.CreatedAt >= fromUtc
                    && e.CreatedAt < toUtc
                )
                .GroupBy(e => e.QrCodeId!.Value)
                .Select(g => new { QrCodeId = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.Select(r => (r.QrCodeId, r.Count)).ToList();
        }

        public async Task<
            List<(
                int QrCodeId,
                int FeedbackSubmitted,
                int MarketingOptIns
            )>
        > GroupFeedbackByQrCodeAsync(
            IReadOnlyList<int> qrCodeIds,
            DateTime fromUtc,
            DateTime toUtc
        )
        {
            if (qrCodeIds.Count == 0)
            {
                return [];
            }

            var rows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    qrCodeIds.Contains(f.QrCodeId)
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                )
                .GroupBy(f => f.QrCodeId)
                .Select(g => new
                {
                    QrCodeId = g.Key,
                    FeedbackSubmitted = g.Count(),
                    MarketingOptIns = g.Count(f => !f.OffersOptOut)
                })
                .ToListAsync();

            return rows
                .Select(r =>
                    (r.QrCodeId, r.FeedbackSubmitted, r.MarketingOptIns)
                )
                .ToList();
        }
    }
}
