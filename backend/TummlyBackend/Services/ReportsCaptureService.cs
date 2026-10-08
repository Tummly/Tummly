using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Reports Capture aggregate — four-step funnel + placement rows.
    /// Lifetime empty is never-scanned Active/Paused QR (or no QR).
    /// </summary>
    public sealed class ReportsCaptureService : IReportsCaptureService
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _capture;

        public ReportsCaptureService(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate capture
        )
        {
            _context = context;
            _capture = capture;
        }

        public async Task<ReportsCaptureDto> GetCaptureAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            if (await IsLifetimeEmptyAsync(locationId, cancellationToken))
            {
                return new ReportsCaptureDto { LifetimeEmpty = true };
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var locationIds = new[] { locationId };
            var activeOrPausedQrIds =
                await _capture.ListActiveOrPausedQrCodeIdsAsync(locationIds);

            var qrScans = await MetricPairAsync(
                () =>
                    _capture.CountScansAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc
                    ),
                () =>
                    _capture.CountScansAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc
                    )
            );

            var feedbackSubmitted = await MetricPairAsync(
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc,
                        marketingOptInOnly: false
                    ),
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc,
                        marketingOptInOnly: false
                    )
            );

            var contactableGuests = await MetricPairAsync(
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc,
                        marketingOptInOnly: true
                    ),
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc,
                        marketingOptInOnly: true
                    )
            );

            var offerClaimed = await MetricPairAsync(
                () =>
                    CountThankYouClaimsAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountThankYouClaimsAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var placements = await BuildPlacementsAsync(
                locationId,
                activeOrPausedQrIds,
                fromUtc,
                toUtc,
                cancellationToken
            );

            return new ReportsCaptureDto
            {
                LifetimeEmpty = false,
                Funnel = new ReportsCaptureFunnelDto
                {
                    QrScans = qrScans,
                    FeedbackSubmitted = feedbackSubmitted,
                    ContactableGuests = contactableGuests,
                    OfferClaimed = offerClaimed,
                },
                Placements = placements,
            };
        }

        private async Task<bool> IsLifetimeEmptyAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var hasQr = await _context.QrCodes
                .AsNoTracking()
                .AnyAsync(
                    q => q.RestaurantLocationId == locationId,
                    cancellationToken
                );
            if (!hasQr)
            {
                return true;
            }

            var activeOrPausedQrIds =
                await _capture.ListActiveOrPausedQrCodeIdsAsync(
                    [locationId]
                );
            if (activeOrPausedQrIds.Count == 0)
            {
                return true;
            }

            var hasScan = await _context.QrScanEvents
                .AsNoTracking()
                .AnyAsync(
                    e =>
                        e.QrCodeId != null
                        && activeOrPausedQrIds.Contains(e.QrCodeId.Value),
                    cancellationToken
                );

            return !hasScan;
        }

        private async Task<
            IReadOnlyList<ReportsCapturePlacementDto>
        > BuildPlacementsAsync(
            int locationId,
            IReadOnlyList<int> activeOrPausedQrIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            if (activeOrPausedQrIds.Count == 0)
            {
                return [];
            }

            var qrRows = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && activeOrPausedQrIds.Contains(q.Id)
                    && q.QrType != QrType.DigitalGuestLink
                )
                .OrderBy(q => q.QrType)
                .Select(q => new
                {
                    q.Id,
                    q.QrType,
                    q.Status,
                })
                .ToListAsync(cancellationToken);

            var placementQrIds = qrRows.Select(q => q.Id).ToList();

            var scansByQr = (
                await _capture.GroupScansByQrCodeAsync(
                    placementQrIds,
                    fromUtc,
                    toUtc
                )
            ).ToDictionary(r => r.QrCodeId, r => r.Count);

            var feedbackByQr = (
                await _capture.GroupFeedbackByQrCodeAsync(
                    placementQrIds,
                    fromUtc,
                    toUtc
                )
            ).ToDictionary(
                r => r.QrCodeId,
                r => (r.FeedbackSubmitted, r.MarketingOptIns)
            );

            var claimsByQr = await (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                join f in _context.Feedbacks.AsNoTracking()
                    on i.FeedbackId equals f.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.Source == OfferIssueSources.GuestFormThankYou
                    && i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc
                    && placementQrIds.Contains(f.QrCodeId)
                group i by f.QrCodeId into g
                select new { QrCodeId = g.Key, Count = g.Count() }
            ).ToDictionaryAsync(
                row => row.QrCodeId,
                row => row.Count,
                cancellationToken
            );

            return qrRows
                .Select(q =>
                {
                    scansByQr.TryGetValue(q.Id, out var scans);
                    feedbackByQr.TryGetValue(q.Id, out var feedback);
                    claimsByQr.TryGetValue(q.Id, out var claims);
                    double? conversion =
                        scans > 0
                            ? Math.Round(
                                100.0 * feedback.FeedbackSubmitted / scans,
                                1
                            )
                            : null;
                    return new ReportsCapturePlacementDto
                    {
                        QrCodeId = q.Id,
                        Name = PlacementName(q.QrType),
                        Status = q.Status.ToString(),
                        Scans = scans,
                        FormOpens = null,
                        Feedback = feedback.FeedbackSubmitted,
                        Contactable = feedback.MarketingOptIns,
                        Claims = claims,
                        ConversionPercent = conversion,
                    };
                })
                .ToList();
        }

        private static string PlacementName(QrType qrType)
        {
            return FeedbackQrSourceMapping.ToDisplay(
                    new QrCode { QrType = qrType, LinkName = null }
                )
                ?? qrType.ToString();
        }

        private Task<int> CountThankYouClaimsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return (
                from i in _context.OfferIssues.AsNoTracking()
                join o in _context.CatalogOffers.AsNoTracking()
                    on i.CatalogOfferId equals o.Id
                where
                    o.RestaurantLocationId == locationId
                    && i.Source == OfferIssueSources.GuestFormThankYou
                    && i.ClaimedAtUtc != null
                    && i.ClaimedAtUtc >= fromUtc
                    && i.ClaimedAtUtc < toUtc
                select i.Id
            ).CountAsync(cancellationToken);
        }

        private static async Task<ReportsMetricDto> MetricPairAsync(
            Func<Task<int>> current,
            Func<Task<int>> previous
        )
        {
            return Metric(await current(), await previous());
        }

        private static ReportsMetricDto Metric(int value, int valuePrevious)
        {
            return new ReportsMetricDto
            {
                Value = value,
                ValuePrevious = valuePrevious,
            };
        }
    }
}
