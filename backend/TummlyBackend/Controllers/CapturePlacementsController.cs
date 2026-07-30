using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/placements")]
    [Authorize]
    public class CapturePlacementsController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly ICaptureArchiveListService _archiveList;
        private readonly ICaptureQrLifecycleService _lifecycle;

        public CapturePlacementsController(
            IOwnedLocationService ownedLocation,
            ICaptureArchiveListService archiveList,
            ICaptureQrLifecycleService lifecycle
        )
        {
            _ownedLocation = ownedLocation;
            _archiveList = archiveList;
            _lifecycle = lifecycle;
        }

        public sealed class CreateDigitalGuestLinkRequest
        {
            public string? LinkName { get; set; }

            public string? InternalDescription { get; set; }

            public string? Channel { get; set; }

            public string? Status { get; set; }
        }

        [HttpPost("digital-guest-links")]
        public async Task<IActionResult> CreateDigitalGuestLink(
            [FromQuery] int locationId,
            [FromBody] CreateDigitalGuestLinkRequest? body
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (body == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Request body is required."
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

            var result = await _lifecycle.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = userId,
                    LocationId = locationId,
                    LinkName = body.LinkName,
                    InternalDescription = body.InternalDescription,
                    Channel = body.Channel,
                    Status = body.Status,
                }
            );

            return QrLifecycleHttp.ToActionResult(this, result);
        }

        public sealed class UpdateInternalDescriptionRequest
        {
            public string? InternalDescription { get; set; }
        }

        [HttpPatch("{qrCodeId:int}/internal-description")]
        public async Task<IActionResult> UpdateInternalDescription(
            int qrCodeId,
            [FromQuery] int locationId,
            [FromBody] UpdateInternalDescriptionRequest? body
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (body == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Request body is required."
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

            var result = await _lifecycle.UpdateInternalDescriptionAsync(
                new UpdateInternalDescriptionCommand
                {
                    UserId = userId,
                    LocationId = locationId,
                    QrCodeId = qrCodeId,
                    InternalDescription = body.InternalDescription,
                }
            );

            return QrLifecycleHttp.ToActionResult(this, result);
        }

        [HttpPost("{qrCodeId:int}/pause")]
        public Task<IActionResult> PausePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        ) => MutateCodeAsync(qrCodeId, locationId, _lifecycle.PauseAsync);

        [HttpPost("{qrCodeId:int}/resume")]
        public Task<IActionResult> ResumePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        ) => MutateCodeAsync(qrCodeId, locationId, _lifecycle.ResumeAsync);

        [HttpPost("{qrCodeId:int}/rotate")]
        public Task<IActionResult> RotatePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        ) => MutateCodeAsync(qrCodeId, locationId, _lifecycle.RotateAsync);

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchivedPlacements(
            [FromQuery] string? q = null,
            [FromQuery] int[]? locationIds = null,
            [FromQuery] string[]? qrTypes = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string[]? archivedBy = null,
            [FromQuery] string sort = "recently-archived",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? utcOffsetMinutes = null
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
                var result = await _archiveList.ListAsync(
                    new CaptureArchiveListQuery
                    {
                        OwnerUserId = userId,
                        Q = q,
                        LocationIds = locationIds,
                        QrTypes = qrTypes,
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        ArchivedBy = archivedBy,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                        UtcOffsetMinutes = utcOffsetMinutes,
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

        [HttpPost("{qrCodeId:int}/archive")]
        public Task<IActionResult> ArchivePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        ) => MutateCodeAsync(qrCodeId, locationId, _lifecycle.ArchiveAsync);

        [HttpPost("{qrCodeId:int}/restore")]
        public Task<IActionResult> RestorePlacement(
            int qrCodeId,
            [FromQuery] int locationId
        ) => MutateCodeAsync(qrCodeId, locationId, _lifecycle.RestoreAsync);

        private async Task<IActionResult> MutateCodeAsync(
            int qrCodeId,
            int locationId,
            Func<QrCodeLifecycleCommand, Task<QrLifecycleResult>> action
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
                new QrCodeLifecycleCommand
                {
                    UserId = userId,
                    LocationId = locationId,
                    QrCodeId = qrCodeId,
                }
            );

            return QrLifecycleHttp.ToActionResult(this, result);
        }
    }
}
