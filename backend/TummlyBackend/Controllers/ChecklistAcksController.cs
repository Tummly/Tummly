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
        private readonly IRestaurantPermissionHelper _permissions;

        public ChecklistAcksController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions
        )
        {
            _context = context;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetChecklistAcks(
            [FromQuery] int locationId
        )
        {
            var access = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.View,
                locationId
            );
            var denied = access.ToHttpResult();

            if (denied != null)
            {
                return denied;
            }

            var location = access.Location!;
            var hasBrandLogo = await RestaurantHasBrandLogoAsync(
                location.RestaurantId
            );

            return Ok(ToResponse(location, hasBrandLogo));
        }

        [HttpPost]
        public async Task<IActionResult> UpsertChecklistAcks(
            [FromQuery] int locationId,
            [FromBody] UpdateChecklistAcksDto dto
        )
        {
            var access = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.Manage,
                locationId
            );
            var denied = access.ToHttpResult();

            if (denied != null)
            {
                return denied;
            }

            // Tracked entity: the permission lookup is AsNoTracking.
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

            var hasBrandLogo = await RestaurantHasBrandLogoAsync(
                location.RestaurantId
            );

            return Ok(ToResponse(location, hasBrandLogo));
        }

        private async Task<bool> RestaurantHasBrandLogoAsync(int restaurantId)
        {
            var objectKey = await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.Id == restaurantId)
                .Select(r => r.BrandLogoObjectKey)
                .FirstOrDefaultAsync();

            return !string.IsNullOrWhiteSpace(objectKey);
        }

        private static object ToResponse(
            RestaurantLocation location,
            bool hasBrandLogo
        )
        {
            return new
            {
                success = true,
                locationId = location.Id,
                guestFormPreviewed =
                    location.GuestFormPreviewedAt != null,
                qrPlacementGuideViewed =
                    location.QrPlacementGuideViewedAt != null,
                logoUploaded =
                    location.LogoUploadedAt != null || hasBrandLogo,
                guestFormPreviewedAt =
                    location.GuestFormPreviewedAt,
                qrPlacementGuideViewedAt =
                    location.QrPlacementGuideViewedAt,
                logoUploadedAt = location.LogoUploadedAt
            };
        }
    }
}
