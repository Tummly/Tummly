using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/locations")]
    [Authorize]
    public class CaptureLocationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly ICaptureMultiLocationReadsService _reads;
        private readonly ICaptureLocationSnapshotService _snapshot;

        public CaptureLocationsController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            ICaptureMultiLocationReadsService reads,
            ICaptureLocationSnapshotService snapshot
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _reads = reads;
            _snapshot = snapshot;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? q,
            [FromQuery] string[]? status,
            [FromQuery] int[]? locationIds,
            [FromQuery] string sort = "highest-qr-scans",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            try
            {
                var result = await _reads.GetLocationsAsync(
                    new CaptureLocationsQuery
                    {
                        OwnerUserId = userId,
                        From = from,
                        To = to,
                        Q = q,
                        Status = status,
                        LocationIds = locationIds,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                    }
                );

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("{locationId:int}/snapshot")]
        public async Task<IActionResult> GetSnapshot(
            int locationId,
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

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _snapshot.GetSnapshotAsync(
                    new CaptureLocationSnapshotQuery
                    {
                        LocationId = locationId,
                        From = from,
                        To = to,
                    }
                );

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpPost("{locationId:int}/pause")]
        public async Task<IActionResult> PauseLocationCapture(int locationId)
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

            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == locationId);

            if (location.CaptureLocationStatus == CaptureLocationStatus.Paused)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Location capture is already paused."
                });
            }

            var activeCodes = await _context.QrCodes
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && q.Status == QrCodeStatus.Active
                )
                .ToListAsync();

            var restoreIds = activeCodes.Select(q => q.Id).ToList();
            foreach (var qr in activeCodes)
            {
                qr.Status = QrCodeStatus.Paused;
            }

            location.CaptureLocationStatus = CaptureLocationStatus.Paused;
            location.CaptureLocationPauseRestoreQrCodeIdsJson =
                CaptureLocationPauseRestore.Serialize(restoreIds);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                locationId = location.Id,
                status = location.CaptureLocationStatus.ToString(),
                pausedCount = restoreIds.Count,
                pauseRestoreQrCodeCount = restoreIds.Count
            });
        }

        [HttpPost("{locationId:int}/activate")]
        public async Task<IActionResult> ActivateLocationCapture(int locationId)
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

            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == locationId);

            if (location.CaptureLocationStatus != CaptureLocationStatus.Paused)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Location capture is not paused."
                });
            }

            var restoreIds = CaptureLocationPauseRestore.Parse(
                location.CaptureLocationPauseRestoreQrCodeIdsJson
            );

            var codesToActivate = await _context.QrCodes
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && restoreIds.Contains(q.Id)
                    && q.Status == QrCodeStatus.Paused
                )
                .ToListAsync();

            foreach (var qr in codesToActivate)
            {
                qr.Status = QrCodeStatus.Active;
            }

            location.CaptureLocationStatus = CaptureLocationStatus.Active;
            location.CaptureLocationPauseRestoreQrCodeIdsJson = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                locationId = location.Id,
                status = location.CaptureLocationStatus.ToString(),
                activatedCount = codesToActivate.Count,
                pauseRestoreQrCodeCount = 0
            });
        }
    }
}
