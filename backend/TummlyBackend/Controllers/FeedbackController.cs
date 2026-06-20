using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TummlyBackend.Data;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        /*
         =========================================
         GET FEEDBACK STATS FOR A LOCATION
         =========================================
        */

        [HttpGet]
        public async Task<IActionResult> GetFeedback(
            [FromQuery] int locationId
        )
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

            /*
             =========================================
             OWNERSHIP CHECK
             =========================================
            */

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(l => l.Restaurant)
                .FirstOrDefaultAsync(l =>
                    l.Id == locationId
                );

            if (location == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Location not found."
                });
            }

            if (
                location.Restaurant == null
                || location.Restaurant.OwnerUserId != userId
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message = "You do not have access to this location."
                    }
                );
            }

            /*
             =========================================
             FEEDBACK STATS
             =========================================
            */

            var total = await _context.Feedbacks
                .CountAsync(f =>
                    f.RestaurantLocationId == locationId
                );

            var recent = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .Select(f => new
                {
                    f.Id,
                    f.GuestName,
                    f.GuestContact,
                    contactType = f.ContactType.ToString(),
                    f.Comment,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                total,
                recent
            });
        }
    }
}
