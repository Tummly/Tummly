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
    [Route("api/restaurant")]
    [Authorize]
    public class RestaurantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IRestaurantPermissionHelper _permissions;

        public RestaurantController(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink,
            IRestaurantPermissionHelper permissions
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
            _permissions = permissions;
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeLocationSetAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.View
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == decision.RestaurantId);

            if (restaurant == null)
            {
                return Ok(new
                {
                    success = true,
                    restaurantName = "",
                    locations = Array.Empty<object>()
                });
            }

            var scopedIds = decision.LocationIds.ToHashSet();
            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurant.Id
                    && scopedIds.Contains(row.Id)
                )
                .OrderBy(row => row.CreatedAt)
                .Select(row => new
                {
                    row.Id,
                    row.LocationName,
                    row.Address,
                    row.LocationPhone,
                    row.LocalContact,
                    row.CreatedAt
                })
                .ToListAsync();

            var locationIds = locations.Select(row => row.Id).ToList();

            var smartGuestTokensByLocationId = await _context.QrCodes
                .AsNoTracking()
                .Where(qr =>
                    locationIds.Contains(qr.RestaurantLocationId)
                    && qr.QrType == QrType.SmartGuest
                    && qr.Status == QrCodeStatus.Active
                )
                .ToDictionaryAsync(qr => qr.RestaurantLocationId, qr => qr.Token);

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
                locations = locations.Select(row => new
                {
                    row.Id,
                    row.LocationName,
                    row.Address,
                    guestUrl = smartGuestTokensByLocationId
                        .TryGetValue(row.Id, out var smartGuestToken)
                        ? _smartGuestLink.BuildGuestUrl(smartGuestToken)
                        : "",
                    row.LocationPhone,
                    row.LocalContact,
                    row.CreatedAt
                })
            });
        }
    }
}
