using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

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

            var recentRows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recent = recentRows.Select(f =>
            {
                var classification =
                    FeedbackClassificationMapping.ToApiFields(f);

                return new
                {
                    id = f.Id,
                    guestName = f.GuestName,
                    guestContact = f.GuestContact,
                    contactType = f.ContactType.ToString(),
                    comment = f.Comment,
                    createdAt = f.CreatedAt,
                    classificationStatus =
                        classification.ClassificationStatus,
                    sentiment = classification.Sentiment,
                    detectedTags = classification.DetectedTags
                };
            });

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
                .Include(f => f.RestaurantLocation)
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

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

            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                guestName = feedback.GuestName,
                guestContact = feedback.GuestContact,
                contactType = feedback.ContactType.ToString(),
                comment = feedback.Comment,
                createdAt = feedback.CreatedAt,
                locationName = feedback.RestaurantLocation!.LocationName,
                address = feedback.RestaurantLocation.Address,
                classificationStatus =
                    classification.ClassificationStatus,
                sentiment = classification.Sentiment,
                detectedTags = classification.DetectedTags
            });
        }

        /*
         =========================================
         CORRECT CLASSIFICATION SENTIMENT (OWNED)
         =========================================
        */

        [HttpPut("{feedbackId:int}/classification")]
        public async Task<IActionResult> CorrectClassification(
            int feedbackId,
            [FromBody] CorrectFeedbackClassificationDto dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackClassificationMapping.TryParseWireSentiment(
                    dto.Sentiment,
                    out var sentiment
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Sentiment must be positive, neutral, or negative."
                });
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

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

            if (feedback.ClassificationStatus
                != ClassificationStatus.Succeeded)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Classification can only be corrected when it has succeeded."
                });
            }

            feedback.Sentiment = sentiment;
            await _context.SaveChangesAsync();

            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                classificationStatus =
                    classification.ClassificationStatus,
                sentiment = classification.Sentiment,
                detectedTags = classification.DetectedTags
            });
        }
    }
}
