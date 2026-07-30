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
    [Route("api/capture/placements")]
    [Authorize]
    public class CapturePlacementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly ICaptureArchiveListService _archiveList;

        public CapturePlacementsController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            ISmartGuestLinkService smartGuestLink,
            ICaptureArchiveListService archiveList
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _smartGuestLink = smartGuestLink;
            _archiveList = archiveList;
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

            if (string.IsNullOrWhiteSpace(body.LinkName))
            {
                return BadRequest(new
                {
                    success = false,
                    field = "linkName",
                    message = "Link name is required."
                });
            }

            var linkName = DigitalGuestLinkNaming.FormatLinkName(body.LinkName);
            if (linkName.Length > DigitalGuestLinkNaming.LinkNameMaxLength)
            {
                return BadRequest(new
                {
                    success = false,
                    field = "linkName",
                    message =
                        $"Link name must be at most {DigitalGuestLinkNaming.LinkNameMaxLength} characters."
                });
            }

            var internalDescription =
                DigitalGuestLinkNaming.FormatInternalDescription(
                    body.InternalDescription
                );
            if (
                internalDescription != null
                && internalDescription.Length
                    > DigitalGuestLinkNaming.InternalDescriptionMaxLength
            )
            {
                return BadRequest(new
                {
                    success = false,
                    field = "internalDescription",
                    message =
                        $"Internal description must be at most {DigitalGuestLinkNaming.InternalDescriptionMaxLength} characters."
                });
            }

            if (
                string.IsNullOrWhiteSpace(body.Channel)
                || !Enum.TryParse<DigitalGuestLinkChannel>(
                    body.Channel.Trim(),
                    ignoreCase: false,
                    out var channel
                )
            )
            {
                return BadRequest(new
                {
                    success = false,
                    field = "channel",
                    message = "Channel is required."
                });
            }

            var requestedStatus = QrCodeStatus.Active;
            if (!string.IsNullOrWhiteSpace(body.Status))
            {
                if (
                    !Enum.TryParse<QrCodeStatus>(
                        body.Status.Trim(),
                        ignoreCase: false,
                        out requestedStatus
                    )
                    || (requestedStatus != QrCodeStatus.Active
                        && requestedStatus != QrCodeStatus.Paused)
                )
                {
                    return BadRequest(new
                    {
                        success = false,
                        field = "status",
                        message = "Status must be Active or Paused."
                    });
                }
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
                .AsNoTracking()
                .FirstAsync(l => l.Id == locationId);

            var status =
                location.CaptureLocationStatus == CaptureLocationStatus.Paused
                    ? QrCodeStatus.Paused
                    : requestedStatus;

            var normalizedLinkName =
                DigitalGuestLinkNaming.NormalizeLinkName(linkName);

            var nameTaken = await _context.QrCodes.AnyAsync(q =>
                q.RestaurantLocationId == locationId
                && q.QrType == QrType.DigitalGuestLink
                && (q.Status == QrCodeStatus.Active
                    || q.Status == QrCodeStatus.Paused)
                && q.NormalizedLinkName == normalizedLinkName
            );

            if (nameTaken)
            {
                return Conflict(new
                {
                    success = false,
                    field = "linkName",
                    message =
                        "A digital guest link with this name already exists at this location."
                });
            }

            var actor = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var token = await _smartGuestLink.GenerateTokenAsync();
            var qrCode = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = QrType.DigitalGuestLink,
                Token = token,
                Status = status,
                LinkName = linkName,
                NormalizedLinkName = normalizedLinkName,
                Channel = channel,
                InternalDescription = internalDescription,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                CreatedByDisplayName = actor?.FullName,
            };

            _context.QrCodes.Add(qrCode);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    success = false,
                    field = "linkName",
                    message =
                        "A digital guest link with this name already exists at this location."
                });
            }

            return Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                qrType = qrCode.QrType.ToString(),
                status = qrCode.Status.ToString(),
                linkName = qrCode.LinkName,
                channel = qrCode.Channel?.ToString(),
                internalDescription = qrCode.InternalDescription,
                createdAt = qrCode.CreatedAt,
                createdByDisplayName = qrCode.CreatedByDisplayName,
                updatedAt = qrCode.UpdatedAt,
                updatedByDisplayName = qrCode.UpdatedByDisplayName,
                qrLinkUrl = _smartGuestLink.BuildGuestUrl(qrCode.Token),
            });
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

            if (
                qrCode.Status != QrCodeStatus.Active
                && qrCode.Status != QrCodeStatus.Paused
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Only Active or Paused QR codes can update their description."
                });
            }

            var internalDescription =
                DigitalGuestLinkNaming.FormatInternalDescription(
                    body.InternalDescription
                );
            if (
                internalDescription != null
                && internalDescription.Length
                    > DigitalGuestLinkNaming.InternalDescriptionMaxLength
            )
            {
                return BadRequest(new
                {
                    success = false,
                    field = "internalDescription",
                    message =
                        $"Internal description must be at most {DigitalGuestLinkNaming.InternalDescriptionMaxLength} characters."
                });
            }

            var actor = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            qrCode.InternalDescription = internalDescription;
            qrCode.UpdatedAt = DateTime.UtcNow;
            qrCode.UpdatedByUserId = userId;
            qrCode.UpdatedByDisplayName = actor?.FullName;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                internalDescription = qrCode.InternalDescription,
                updatedAt = qrCode.UpdatedAt,
                updatedByDisplayName = qrCode.UpdatedByDisplayName,
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

        [HttpPost("{qrCodeId:int}/rotate")]
        public async Task<IActionResult> RotatePlacement(
            int qrCodeId,
            [FromQuery] int locationId
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

            if (qrCode.QrType == QrType.DigitalGuestLink)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Digital guest links cannot be rotated."
                });
            }

            if (
                qrCode.Status != QrCodeStatus.Active
                && qrCode.Status != QrCodeStatus.Paused
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only Active or Paused QR codes can be rotated."
                });
            }

            qrCode.Token = await _smartGuestLink.GenerateTokenAsync();
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString(),
                qrLinkUrl = _smartGuestLink.BuildGuestUrl(qrCode.Token)
            });
        }

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
        public async Task<IActionResult> ArchivePlacement(
            int qrCodeId,
            [FromQuery] int locationId
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

            if (
                qrCode.Status != QrCodeStatus.Active
                && qrCode.Status != QrCodeStatus.Paused
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only Active or Paused QR codes can be archived."
                });
            }

            var actor = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            qrCode.Status = QrCodeStatus.Archived;
            qrCode.ArchivedAt = DateTime.UtcNow;
            qrCode.ArchivedByUserId = userId;
            qrCode.ArchivedByDisplayName = actor?.FullName;

            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == locationId);
            location.CaptureLocationPauseRestoreQrCodeIdsJson =
                CaptureLocationPauseRestore.Remove(
                    location.CaptureLocationPauseRestoreQrCodeIdsJson,
                    qrCodeId
                );

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString(),
                archivedAt = qrCode.ArchivedAt,
                archivedByDisplayName = qrCode.ArchivedByDisplayName
            });
        }

        [HttpPost("{qrCodeId:int}/restore")]
        public async Task<IActionResult> RestorePlacement(
            int qrCodeId,
            [FromQuery] int locationId
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

            if (qrCode.Status != QrCodeStatus.Archived)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only Archived QR codes can be restored."
                });
            }

            if (qrCode.QrType == QrType.DigitalGuestLink)
            {
                if (qrCode.NormalizedLinkName != null)
                {
                    var nameTaken = await _context.QrCodes.AnyAsync(q =>
                        q.RestaurantLocationId == locationId
                        && q.QrType == QrType.DigitalGuestLink
                        && (q.Status == QrCodeStatus.Active
                            || q.Status == QrCodeStatus.Paused)
                        && q.NormalizedLinkName == qrCode.NormalizedLinkName
                    );

                    if (nameTaken)
                    {
                        return Conflict(new
                        {
                            success = false,
                            reason = "link_name_occupied",
                            message =
                                "A digital guest link with this name already exists at this location."
                        });
                    }
                }
            }
            else
            {
                var slotTaken = await _context.QrCodes.AnyAsync(q =>
                    q.RestaurantLocationId == locationId
                    && q.QrType == qrCode.QrType
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                );

                if (slotTaken)
                {
                    return Conflict(new
                    {
                        success = false,
                        reason = "type_slot_occupied",
                        message =
                            "A QR code of this type already exists at this location."
                    });
                }
            }

            qrCode.Status = QrCodeStatus.Paused;
            qrCode.ArchivedAt = null;
            qrCode.ArchivedByUserId = null;
            qrCode.ArchivedByDisplayName = null;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    success = false,
                    reason = qrCode.QrType == QrType.DigitalGuestLink
                        ? "link_name_occupied"
                        : "type_slot_occupied",
                    message = qrCode.QrType == QrType.DigitalGuestLink
                        ? "A digital guest link with this name already exists at this location."
                        : "A QR code of this type already exists at this location."
                });
            }

            return Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString(),
                qrLinkUrl = _smartGuestLink.BuildGuestUrl(qrCode.Token)
            });
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

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == locationId);

            if (location.CaptureLocationStatus == CaptureLocationStatus.Paused)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Per-code Pause and Activate are unavailable while location capture is paused."
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
    }
}
