using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/overview")]
    [Authorize]
    public class CaptureOverviewController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public CaptureOverviewController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
        }

        [HttpGet]
        public async Task<IActionResult> GetOverview(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (from == null || to == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from and to are required."
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to."
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days."
                });
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == userId);

            if (restaurant == null)
            {
                return Ok(new
                {
                    success = true,
                    activeLocations = 0,
                    totalLocations = 0,
                    activeQrPlacements = 0,
                    qrScans = 0,
                    qrScansPrevious = 0,
                    feedbackSubmitted = 0,
                    feedbackSubmittedPrevious = 0,
                    marketingOptIns = 0,
                    marketingOptInsPrevious = 0,
                    offerClaims = 0,
                    offerClaimsHasRealData = false
                });
            }

            var ownedLocationIds =
                await _ownedLocation.ListOwnedLocationIdsAsync(
                    restaurant.Id,
                    userId
                );

            // Capture location status: count only Active Owned locations.
            var totalLocations = ownedLocationIds.Count;
            var activeLocations = ownedLocationIds.Count == 0
                ? 0
                : await _context.RestaurantLocations
                    .CountAsync(l =>
                        ownedLocationIds.Contains(l.Id)
                        && l.CaptureLocationStatus
                            == CaptureLocationStatus.Active
                    );

            var activeQrPlacements = ownedLocationIds.Count == 0
                ? 0
                : await _context.QrCodes
                    .CountAsync(q =>
                        ownedLocationIds.Contains(q.RestaurantLocationId)
                        && q.Status == QrCodeStatus.Active
                    );

            if (ownedLocationIds.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    activeLocations,
                    totalLocations,
                    activeQrPlacements,
                    qrScans = 0,
                    qrScansPrevious = 0,
                    feedbackSubmitted = 0,
                    feedbackSubmittedPrevious = 0,
                    marketingOptIns = 0,
                    marketingOptInsPrevious = 0,
                    offerClaims = 0,
                    offerClaimsHasRealData = false
                });
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var activeOrPausedQrCodeIds = await _context.QrCodes
                .Where(q =>
                    ownedLocationIds.Contains(q.RestaurantLocationId)
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .Select(q => q.Id)
                .ToListAsync();

            var qrScans = await _context.QrScanEvents
                .CountAsync(e =>
                    ownedLocationIds.Contains(e.RestaurantLocationId)
                    && e.QrCodeId != null
                    && activeOrPausedQrCodeIds.Contains(e.QrCodeId.Value)
                    && e.CreatedAt >= fromUtc
                    && e.CreatedAt < toUtc
                );

            var qrScansPrevious = await _context.QrScanEvents
                .CountAsync(e =>
                    ownedLocationIds.Contains(e.RestaurantLocationId)
                    && e.QrCodeId != null
                    && activeOrPausedQrCodeIds.Contains(e.QrCodeId.Value)
                    && e.CreatedAt >= previousFromUtc
                    && e.CreatedAt < previousToUtc
                );

            var feedbackSubmitted = await _context.Feedbacks
                .CountAsync(f =>
                    ownedLocationIds.Contains(f.RestaurantLocationId)
                    && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                );

            var feedbackSubmittedPrevious = await _context.Feedbacks
                .CountAsync(f =>
                    ownedLocationIds.Contains(f.RestaurantLocationId)
                    && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                    && f.CreatedAt >= previousFromUtc
                    && f.CreatedAt < previousToUtc
                );

            var marketingOptIns = await _context.Feedbacks
                .CountAsync(f =>
                    ownedLocationIds.Contains(f.RestaurantLocationId)
                    && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                    && !f.OffersOptOut
                );

            var marketingOptInsPrevious = await _context.Feedbacks
                .CountAsync(f =>
                    ownedLocationIds.Contains(f.RestaurantLocationId)
                    && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                    && f.CreatedAt >= previousFromUtc
                    && f.CreatedAt < previousToUtc
                    && !f.OffersOptOut
                );

            return Ok(new
            {
                success = true,
                activeLocations,
                totalLocations,
                activeQrPlacements,
                qrScans,
                qrScansPrevious,
                feedbackSubmitted,
                feedbackSubmittedPrevious,
                marketingOptIns,
                marketingOptInsPrevious,
                offerClaims = 0,
                offerClaimsHasRealData = false
            });
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
