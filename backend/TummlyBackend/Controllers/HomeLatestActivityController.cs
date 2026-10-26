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
        private readonly IRestaurantPermissionHelper _permissions;

        public HomeLatestActivityController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions
        )
        {
            _context = context;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestActivity(
            [FromQuery] int locationId
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedbackDecision =
                await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Feedback,
                    PermissionLevel.View,
                    locationId
                );
            var guestsDecision =
                await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Guests,
                    PermissionLevel.View,
                    locationId
                );

            var canViewFeedback =
                feedbackDecision.Status == RestaurantPermissionStatus.Allowed;
            var canViewGuests =
                guestsDecision.Status == RestaurantPermissionStatus.Allowed;

            if (!canViewFeedback && !canViewGuests)
            {
                // Area denials and Location denials share one decision shape.
                // The Locations area is the Location gate: every default role
                // can view it, so only a missing or unassigned Location fails.
                var locationDecision =
                    await _permissions.AuthorizeLocationAsync(
                        User,
                        OperatorAreaIds.Locations,
                        PermissionLevel.View,
                        locationId
                    );

                var locationDenied = locationDecision.ToHttpResult();

                if (locationDenied != null)
                {
                    return locationDenied;
                }
            }

            var feedbackCandidates = canViewFeedback
                ? await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => f.RestaurantLocationId == locationId)
                    .Select(f => new ActivityCandidate(
                        f.CreatedAt,
                        ActivityKind.Feedback,
                        f.Id
                    ))
                    .ToListAsync()
                : new List<ActivityCandidate>();

            var guestCandidates = canViewGuests
                ? await _context.LocationGuests
                    .AsNoTracking()
                    .Where(lg => lg.RestaurantLocationId == locationId)
                    .Select(lg => new ActivityCandidate(
                        lg.CreatedAt,
                        ActivityKind.GuestJoined,
                        lg.Id
                    ))
                    .ToListAsync()
                : new List<ActivityCandidate>();

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
                            guestContact = GuestContactMask.MaskOrEmpty(
                                feedback.ContactType,
                                feedback.GuestContact
                            ),
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
