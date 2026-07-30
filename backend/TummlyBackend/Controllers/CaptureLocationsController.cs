using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/locations")]
    [Authorize]
    public class CaptureLocationsController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly ICaptureMultiLocationReadsService _reads;
        private readonly ICaptureLocationSnapshotService _snapshot;
        private readonly ICapturePreviewOptionsService _previewOptions;
        private readonly ICaptureQrLifecycleService _lifecycle;

        public CaptureLocationsController(
            IOwnedLocationService ownedLocation,
            ICaptureMultiLocationReadsService reads,
            ICaptureLocationSnapshotService snapshot,
            ICapturePreviewOptionsService previewOptions,
            ICaptureQrLifecycleService lifecycle
        )
        {
            _ownedLocation = ownedLocation;
            _reads = reads;
            _snapshot = snapshot;
            _previewOptions = previewOptions;
            _lifecycle = lifecycle;
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

        [HttpGet("{locationId:int}/preview-options")]
        public async Task<IActionResult> GetPreviewOptions(int locationId)
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

            var result = await _previewOptions.GetPreviewOptionsAsync(
                new CapturePreviewOptionsQuery
                {
                    LocationId = locationId,
                }
            );

            return Ok(result);
        }

        [HttpPost("{locationId:int}/pause")]
        public Task<IActionResult> PauseLocationCapture(int locationId) =>
            MutateLocationCaptureAsync(
                locationId,
                _lifecycle.PauseLocationCaptureAsync
            );

        [HttpPost("{locationId:int}/activate")]
        public Task<IActionResult> ActivateLocationCapture(int locationId) =>
            MutateLocationCaptureAsync(
                locationId,
                _lifecycle.ActivateLocationCaptureAsync
            );

        private async Task<IActionResult> MutateLocationCaptureAsync(
            int locationId,
            Func<LocationCaptureLifecycleCommand, Task<QrLifecycleResult>> action
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

            var result = await action(
                new LocationCaptureLifecycleCommand
                {
                    UserId = userId,
                    LocationId = locationId,
                }
            );

            return QrLifecycleHttp.ToActionResult(this, result);
        }
    }
}
