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
    [Route("api/capture/placements")]
    [Authorize]
    public class CapturePlacementsController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly ISmartGuestLinkService _smartGuestLink;

        public CapturePlacementsController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            ISmartGuestLinkService smartGuestLink
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _smartGuestLink = smartGuestLink;
        }

        [HttpGet]
        public async Task<IActionResult> GetPlacements(
            [FromQuery] int locationId,
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

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var qrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .OrderBy(q => q.QrType)
                .ToListAsync();

            var qrCodeIds = qrCodes.Select(q => q.Id).ToList();

            var windowedScans = await _context.QrScanEvents
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

            var windowedFeedback = await _context.Feedbacks
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

            var scanLookup = windowedScans.ToDictionary(
                x => x.QrCodeId,
                x => x.Count
            );
            var feedbackLookup = windowedFeedback.ToDictionary(
                x => x.QrCodeId
            );
            var lastScanLookup = lastScans.ToDictionary(
                x => x.QrCodeId,
                x => x.LastScanAt
            );

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
                    qrLinkUrl = _smartGuestLink.BuildGuestUrl(qr.Token),
                    qrScans = scanLookup.GetValueOrDefault(qr.Id),
                    feedbackSubmitted = feedback?.FeedbackSubmitted ?? 0,
                    marketingOptIns = feedback?.MarketingOptIns ?? 0,
                    offerClaims = 0,
                    lastScanAt
                };
            });

            return Ok(new
            {
                success = true,
                placements
            });
        }

        [HttpPost("{qrCodeId:int}/pause")]
        public Task<IActionResult> PausePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        )
        {
            return UpdatePlacementStatus(
                qrCodeId,
                locationId,
                expectedStatus: QrCodeStatus.Active,
                nextStatus: QrCodeStatus.Paused,
                invalidTransitionMessage: "Only Active QR codes can be paused."
            );
        }

        [HttpPost("{qrCodeId:int}/resume")]
        public Task<IActionResult> ResumePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        )
        {
            return UpdatePlacementStatus(
                qrCodeId,
                locationId,
                expectedStatus: QrCodeStatus.Paused,
                nextStatus: QrCodeStatus.Active,
                invalidTransitionMessage: "Only Paused QR codes can be resumed."
            );
        }

        private async Task<IActionResult> UpdatePlacementStatus(
            int qrCodeId,
            int locationId,
            QrCodeStatus expectedStatus,
            QrCodeStatus nextStatus,
            string invalidTransitionMessage
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var qrCode = await _context.QrCodes
                .FirstOrDefaultAsync(q =>
                    q.Id == qrCodeId
                    && q.RestaurantLocationId == locationId
                );

            if (qrCode == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "QR code not found."
                });
            }

            if (qrCode.Status != expectedStatus)
            {
                return BadRequest(new
                {
                    success = false,
                    message = invalidTransitionMessage
                });
            }

            qrCode.Status = nextStatus;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString()
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
