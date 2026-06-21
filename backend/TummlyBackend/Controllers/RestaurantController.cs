using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

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
                    l.LinkToken,
                    l.LocationPhone,
                    l.LocalContact,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                locations = locations.Select(l => new
                {
                    l.Id,
                    l.LocationName,
                    l.Address,
                    guestUrl = _smartGuestLink.BuildGuestUrl(l.LinkToken),
                    l.LocationPhone,
                    l.LocalContact,
                    l.CreatedAt
                })
            });
        }
    }
}
