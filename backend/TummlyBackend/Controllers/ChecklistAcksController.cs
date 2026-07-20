using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OperatorHome;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/operator-home/checklist-acks")]
    [Authorize]
    public class ChecklistAcksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public ChecklistAcksController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
        }

        [HttpGet]
        public async Task<IActionResult> GetChecklistAcks(
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

            var location = ownedLocation.Location!;

            return Ok(ToResponse(location));
        }

        [HttpPost]
        public async Task<IActionResult> UpsertChecklistAcks(
            [FromQuery] int locationId,
            [FromBody] UpdateChecklistAcksDto dto
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

            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == locationId);

            var now = DateTime.UtcNow;

            if (dto.GuestFormPreviewed == true
                && location.GuestFormPreviewedAt == null)
            {
                location.GuestFormPreviewedAt = now;
            }

            if (dto.QrPlacementGuideViewed == true
                && location.QrPlacementGuideViewedAt == null)
            {
                location.QrPlacementGuideViewedAt = now;
            }

            if (dto.LogoUploaded == true
                && location.LogoUploadedAt == null)
            {
                location.LogoUploadedAt = now;
            }

            await _context.SaveChangesAsync();

            return Ok(ToResponse(location));
        }

        private static object ToResponse(RestaurantLocation location)
        {
            return new
            {
                success = true,
                locationId = location.Id,
                guestFormPreviewed =
                    location.GuestFormPreviewedAt != null,
                qrPlacementGuideViewed =
                    location.QrPlacementGuideViewedAt != null,
                logoUploaded = location.LogoUploadedAt != null,
                guestFormPreviewedAt =
                    location.GuestFormPreviewedAt,
                qrPlacementGuideViewedAt =
                    location.QrPlacementGuideViewedAt,
                logoUploadedAt = location.LogoUploadedAt
            };
        }
    }
}
