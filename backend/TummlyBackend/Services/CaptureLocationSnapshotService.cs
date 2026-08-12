using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class CaptureLocationSnapshotService
        : ICaptureLocationSnapshotService
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _engagement;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly ICaptureThankYouOfferService _thankYouOffer;

        public CaptureLocationSnapshotService(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate engagement,
            ISmartGuestLinkService smartGuestLink,
            ICaptureThankYouOfferService thankYouOffer
        )
        {
            _context = context;
            _engagement = engagement;
            _smartGuestLink = smartGuestLink;
            _thankYouOffer = thankYouOffer;
        }

        public async Task<object> GetSnapshotAsync(
            CaptureLocationSnapshotQuery query
        )
        {
            var (fromUtc, toUtc) = CaptureDateWindows.Resolve(
                query.From,
                query.To
            );

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == query.LocationId);

            var qrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == query.LocationId
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .OrderBy(q => q.QrType)
                .ToListAsync();

            var qrCodeIds = qrCodes.Select(q => q.Id).ToList();
            var locationIds = new[] { query.LocationId };

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var windowedScans = await _engagement.GroupScansByQrCodeAsync(
                qrCodeIds,
                fromUtc,
                toUtc
            );
            var windowedFeedback = await _engagement.GroupFeedbackByQrCodeAsync(
                qrCodeIds,
                fromUtc,
                toUtc
            );

            var scanLookup = windowedScans.ToDictionary(
                x => x.QrCodeId,
                x => x.Count
            );
            var feedbackLookup = windowedFeedback.ToDictionary(
                x => x.QrCodeId
            );

            // Current location totals = sum of the same current-window per-code
            // aggregates that populate rows (one group-by, then sum).
            var qrScans = windowedScans.Sum(x => x.Count);
            var feedbackSubmitted = windowedFeedback.Sum(
                x => x.FeedbackSubmitted
            );
            var marketingOptIns = windowedFeedback.Sum(x => x.MarketingOptIns);

            var qrScansPrevious = await _engagement.CountScansAsync(
                locationIds,
                qrCodeIds,
                previousFromUtc,
                previousToUtc
            );
            var feedbackSubmittedPrevious =
                await _engagement.CountFeedbackAsync(
                    locationIds,
                    qrCodeIds,
                    previousFromUtc,
                    previousToUtc,
                    marketingOptInOnly: false
                );
            var marketingOptInsPrevious =
                await _engagement.CountFeedbackAsync(
                    locationIds,
                    qrCodeIds,
                    previousFromUtc,
                    previousToUtc,
                    marketingOptInOnly: true
                );

            Dictionary<int, DateTime> lastScanLookup;
            if (qrCodeIds.Count == 0)
            {
                lastScanLookup = new Dictionary<int, DateTime>();
            }
            else
            {
                var lastScans = await _context.QrScanEvents
                    .AsNoTracking()
                    .Where(e =>
                        e.QrCodeId != null
                        && qrCodeIds.Contains(e.QrCodeId.Value)
                    )
                    .GroupBy(e => e.QrCodeId!.Value)
                    .Select(g => new
                    {
                        QrCodeId = g.Key,
                        LastScanAt = g.Max(e => e.CreatedAt)
                    })
                    .ToListAsync();

                lastScanLookup = lastScans.ToDictionary(
                    x => x.QrCodeId,
                    x => x.LastScanAt
                );
            }

            object? lastJourneyUpdate = null;
            if (qrCodeIds.Count > 0)
            {
                var latestFeedback = await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => qrCodeIds.Contains(f.QrCodeId))
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new
                    {
                        f.CreatedAt,
                        f.GuestName
                    })
                    .FirstOrDefaultAsync();

                if (latestFeedback != null)
                {
                    lastJourneyUpdate = new
                    {
                        createdAt = latestFeedback.CreatedAt,
                        guestName = latestFeedback.GuestName
                    };
                }
            }

            var placements = qrCodes.Select(qr =>
            {
                feedbackLookup.TryGetValue(qr.Id, out var feedback);
                DateTime? lastScanAt = lastScanLookup.TryGetValue(
                    qr.Id,
                    out var scannedAt
                )
                    ? scannedAt
                    : null;

                return new
                {
                    qrCodeId = qr.Id,
                    qrType = qr.QrType.ToString(),
                    status = qr.Status.ToString(),
                    linkName = qr.LinkName,
                    channel = qr.Channel?.ToString(),
                    internalDescription = qr.InternalDescription,
                    createdAt = qr.CreatedAt,
                    createdByDisplayName = qr.CreatedByDisplayName,
                    updatedAt = qr.UpdatedAt,
                    updatedByDisplayName = qr.UpdatedByDisplayName,
                    qrLinkUrl = _smartGuestLink.BuildGuestUrl(qr.Token),
                    qrScans = scanLookup.GetValueOrDefault(qr.Id),
                    feedbackSubmitted = feedbackLookup.ContainsKey(qr.Id)
                        ? feedback.FeedbackSubmitted
                        : 0,
                    marketingOptIns = feedbackLookup.ContainsKey(qr.Id)
                        ? feedback.MarketingOptIns
                        : 0,
                    offerClaims = 0,
                    lastScanAt
                };
            });

            var thankYou = await _thankYouOffer.GetAsync(query.LocationId);

            return new
            {
                success = true,
                captureLocationStatus = location.CaptureLocationStatus.ToString(),
                qrScans,
                qrScansPrevious,
                feedbackSubmitted,
                feedbackSubmittedPrevious,
                marketingOptIns,
                marketingOptInsPrevious,
                offerClaims = 0,
                offerClaimsHasRealData = false,
                placements,
                lastJourneyUpdate,
                thankYouOfferId = thankYou.ThankYouOfferId,
                thankYouOfferTitle = thankYou.ThankYouOfferTitle,
                thankYouOfferLive = thankYou.ThankYouOfferLive,
            };
        }
    }
}
