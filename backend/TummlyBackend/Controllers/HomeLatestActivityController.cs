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
    [Route("api/home/latest-activity")]
    [Authorize]
    public class HomeLatestActivityController : ControllerBase
    {
        private const int RecentLimit = 5;

        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public HomeLatestActivityController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestActivity(
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

            var feedbackCandidates = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => f.RestaurantLocationId == locationId)
                .Select(f => new ActivityCandidate(
                    f.CreatedAt,
                    ActivityKind.Feedback,
                    f.Id
                ))
                .ToListAsync();

            var guestCandidates = await _context.LocationGuests
                .AsNoTracking()
                .Where(lg => lg.RestaurantLocationId == locationId)
                .Select(lg => new ActivityCandidate(
                    lg.CreatedAt,
                    ActivityKind.GuestJoined,
                    lg.Id
                ))
                .ToListAsync();

            var topCandidates = feedbackCandidates
                .Concat(guestCandidates)
                .OrderByDescending(candidate => candidate.CreatedAt)
                .Take(RecentLimit)
                .ToList();

            var feedbackIds = topCandidates
                .Where(candidate => candidate.Kind == ActivityKind.Feedback)
                .Select(candidate => candidate.Id)
                .ToList();

            var guestIds = topCandidates
                .Where(candidate => candidate.Kind == ActivityKind.GuestJoined)
                .Select(candidate => candidate.Id)
                .ToList();

            var feedbackRows = feedbackIds.Count == 0
                ? new Dictionary<int, Feedback>()
                : await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => feedbackIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id);

            var guestRows = guestIds.Count == 0
                ? new Dictionary<int, LocationGuest>()
                : await _context.LocationGuests
                    .AsNoTracking()
                    .Where(lg => guestIds.Contains(lg.Id))
                    .ToDictionaryAsync(lg => lg.Id);

            var items = topCandidates
                .Select(candidate =>
                {
                    if (candidate.Kind == ActivityKind.Feedback)
                    {
                        var feedback = feedbackRows[candidate.Id];
                        var classification =
                            FeedbackClassificationMapping.ToApiFields(feedback);

                        return (object)new
                        {
                            kind = "feedback",
                            id = feedback.Id,
                            guestName = feedback.GuestName,
                            guestContact = feedback.GuestContact,
                            contactType = feedback.ContactType.ToString(),
                            comment = feedback.Comment,
                            createdAt = feedback.CreatedAt,
                            classificationStatus =
                                classification.ClassificationStatus,
                            sentiment = classification.Sentiment,
                            detectedTags = classification.DetectedTags,
                            locationGuestId = feedback.LocationGuestId
                        };
                    }

                    var guest = guestRows[candidate.Id];
                    return (object)new
                    {
                        kind = "guest-joined",
                        locationGuestId = guest.Id,
                        guestName = guest.Name,
                        marketingPreference = guest.MarketingPreference.ToWireString(),
                        createdAt = guest.CreatedAt
                    };
                })
                .ToList();

            return Ok(new
            {
                success = true,
                items
            });
        }

        private enum ActivityKind
        {
            Feedback,
            GuestJoined
        }

        private sealed record ActivityCandidate(
            DateTime CreatedAt,
            ActivityKind Kind,
            int Id
        );
    }
}
