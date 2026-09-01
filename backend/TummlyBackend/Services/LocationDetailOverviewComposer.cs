using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Calendar-month overview metrics for Location detail (ticket 02).
    /// </summary>
    public sealed class LocationDetailOverviewComposer
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _engagement;
        private readonly IOffersMetricsService _offersMetrics;

        public LocationDetailOverviewComposer(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate engagement,
            IOffersMetricsService offersMetrics
        )
        {
            _context = context;
            _engagement = engagement;
            _offersMetrics = offersMetrics;
        }

        public async Task<LocationDetailOverviewMetricsDto> ComposeAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var locationIds = new[] { locationId };
            var qrCodeIds = await _engagement.ListActiveOrPausedQrCodeIdsAsync(
                locationIds
            );

            var qrScans = await _engagement.CountScansAsync(
                locationIds,
                qrCodeIds,
                fromUtc,
                toUtc
            );

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .CountAsync(
                    f =>
                        f.RestaurantLocationId == locationId
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc,
                    cancellationToken
                );

            var guestsCaptured = await _context.LocationGuests
                .AsNoTracking()
                .CountAsync(
                    lg =>
                        lg.RestaurantLocationId == locationId
                        && lg.CreatedAt >= fromUtc
                        && lg.CreatedAt < toUtc,
                    cancellationToken
                );

            var optIns = await _engagement.CountFeedbackAsync(
                locationIds,
                qrCodeIds,
                fromUtc,
                toUtc,
                marketingOptInOnly: true
            );

            var offersPerformance = await _offersMetrics.GetPerformanceAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );

            return new LocationDetailOverviewMetricsDto
            {
                QrScans = qrScans,
                FormStarts = 0,
                Feedback = feedback,
                GuestsCaptured = guestsCaptured,
                OptIns = optIns,
                OffersClaimed = offersPerformance.Claims,
                OffersRedeemed = offersPerformance.Redemptions,
            };
        }
    }
}
