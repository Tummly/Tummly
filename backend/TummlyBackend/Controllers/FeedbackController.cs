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
        private readonly IGuestTaggingService _guestTagging;
        private readonly IFeedbackInternalNotesService _internalNotes;
        private readonly IFeedbackClassificationCorrectionsService _corrections;

        public FeedbackController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            IGuestTaggingService guestTagging,
            IFeedbackInternalNotesService internalNotes,
            IFeedbackClassificationCorrectionsService corrections
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _guestTagging = guestTagging;
            _internalNotes = internalNotes;
            _corrections = corrections;
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

            var internalNotes = await _internalNotes.ListForFeedbackAsync(
                feedback.Id
            );
            var noteActivityFacts =
                await _internalNotes.ListActivityFactsForFeedbackAsync(
                    feedback.Id
                );
            var corrections = await _corrections.ListForFeedbackAsync(
                feedback.Id
            );
            var activityHistory = FeedbackActivityHistory.Derive(
                feedback.CreatedAt,
                noteActivityFacts,
                corrections
            );

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
                detectedTags = classification.DetectedTags,
                locationGuestId = feedback.LocationGuestId,
                internalNotes,
                activityHistory,
            });
        }

        /*
         =========================================
         CREATE FEEDBACK INTERNAL NOTE (OWNED)
         =========================================
        */

        [HttpPost("{feedbackId:int}/notes")]
        public async Task<IActionResult> CreateFeedbackInternalNote(
            int feedbackId,
            [FromBody] CreateFeedbackInternalNoteRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
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

            try
            {
                var note = await _internalNotes.CreateAsync(
                    feedbackId,
                    userId,
                    request.Body
                );

                if (note == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    note,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
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

        /*
         =========================================
         UPDATE FEEDBACK INTERNAL NOTE (OWNED)
         =========================================
        */

        [HttpPut("{feedbackId:int}/notes/{noteId:int}")]
        public async Task<IActionResult> UpdateFeedbackInternalNote(
            int feedbackId,
            int noteId,
            [FromBody] UpdateFeedbackInternalNoteRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
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

            try
            {
                var note = await _internalNotes.UpdateAsync(
                    feedbackId,
                    noteId,
                    userId,
                    request.Body
                );

                if (note == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Note not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    note,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
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

        /*
         =========================================
         SOFT-DELETE FEEDBACK INTERNAL NOTE (OWNED)
         =========================================
        */

        [HttpDelete("{feedbackId:int}/notes/{noteId:int}")]
        public async Task<IActionResult> SoftDeleteFeedbackInternalNote(
            int feedbackId,
            int noteId
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
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

            try
            {
                var deleted = await _internalNotes.SoftDeleteAsync(
                    feedbackId,
                    noteId,
                    userId
                );

                if (deleted == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Note not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    deletedAt = deleted.DeletedAt,
                    deletedByDisplayName = deleted.DeletedByDisplayName,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
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

            if (feedback.Sentiment is not FeedbackSentiment fromSentiment)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Classification can only be corrected when it has succeeded."
                });
            }

            if (fromSentiment == sentiment)
            {
                var unchanged =
                    FeedbackClassificationMapping.ToApiFields(feedback);

                return Ok(new
                {
                    success = true,
                    id = feedback.Id,
                    classificationStatus =
                        unchanged.ClassificationStatus,
                    sentiment = unchanged.Sentiment,
                    detectedTags = unchanged.DetectedTags,
                    activityEvent = (FeedbackActivityEventDto?)null,
                });
            }

            feedback.Sentiment = sentiment;

            FeedbackClassificationCorrectionItemDto? recorded;
            try
            {
                recorded = await _corrections.RecordAsync(
                    feedback.Id,
                    userId,
                    fromSentiment,
                    sentiment
                );
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
            }

            if (recorded == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            await _guestTagging.UnionDetectedTagsFromFeedbackAsync(feedback);

            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                classificationStatus =
                    classification.ClassificationStatus,
                sentiment = classification.Sentiment,
                detectedTags = classification.DetectedTags,
                activityEvent = FeedbackActivityHistory.ToActivityEvent(
                    recorded
                ),
            });
        }
    }
}
