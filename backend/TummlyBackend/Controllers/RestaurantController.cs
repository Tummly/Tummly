using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/restaurant")]
    [Authorize]
    public class RestaurantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;

        public RestaurantController(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
        }

        /*
         =========================================
         GET OPERATOR LOCATIONS
         =========================================
        */

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out var userId)
            )
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token."
                });
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r =>
                    r.OwnerUserId == userId
                );

            if (restaurant == null)
            {
                return Ok(new
                {
                    success = true,
                    restaurantName = "",
                    locations = Array.Empty<object>()
                });
            }

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.RestaurantId == restaurant.Id)
                .OrderBy(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.LocationName,
                    l.Address,
                    l.LocationPhone,
                    l.LocalContact,
                    l.CreatedAt
                })
                .ToListAsync();

            var locationIds = locations.Select(l => l.Id).ToList();

            var smartGuestTokensByLocationId = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    locationIds.Contains(q.RestaurantLocationId)
                    && q.QrType == QrType.SmartGuest
                    && q.Status == QrCodeStatus.Active
                )
                .ToDictionaryAsync(q => q.RestaurantLocationId, q => q.Token);

            var brandLogoPublicUrl =
                string.IsNullOrWhiteSpace(restaurant.BrandLogoObjectKey)
                    ? null
                    : BrandLogoRules.BuildPublicUrl(
                        restaurant.BrandLogoObjectKey
                    );

            return Ok(new
            {
                success = true,
                restaurantName = restaurant.Name,
                brandLogoPublicUrl,
                locations = locations.Select(l => new
                {
                    l.Id,
                    l.LocationName,
                    l.Address,
                    guestUrl = smartGuestTokensByLocationId
                        .TryGetValue(l.Id, out var smartGuestToken)
                        ? _smartGuestLink.BuildGuestUrl(smartGuestToken)
                        : "",
                    l.LocationPhone,
                    l.LocalContact,
                    l.CreatedAt
                })
            });
        }
    }
}
