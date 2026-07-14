using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public FeedbackController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
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

        /*
         =========================================
         GET ONE FEEDBACK (OWNED LOCATION)
         =========================================
        */

        [HttpGet("{feedbackId:int}")]
        public async Task<IActionResult> GetFeedbackDetails(int feedbackId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => f.Id == feedbackId)
                .Select(f => new
                {
                    f.Id,
                    f.RestaurantLocationId,
                    f.GuestName,
                    f.GuestContact,
                    ContactType = f.ContactType.ToString(),
                    f.Comment,
                    f.CreatedAt,
                    LocationName = f.RestaurantLocation!.LocationName,
                    Address = f.RestaurantLocation.Address
                })
                .FirstOrDefaultAsync();

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await _ownedLocation.ResolveAsync(
                userId,
                feedback.RestaurantLocationId
            );

            var denied = OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                guestName = feedback.GuestName,
                guestContact = feedback.GuestContact,
                contactType = feedback.ContactType,
                comment = feedback.Comment,
                createdAt = feedback.CreatedAt,
                locationName = feedback.LocationName,
                address = feedback.Address
            });
        }
    }
}
