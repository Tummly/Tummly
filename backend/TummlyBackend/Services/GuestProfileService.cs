using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class GuestProfileService : IGuestProfileService
    {
        private const string LastInteractionLabel = "Feedback submitted";
        private const int LatestFeedbackPreviewLimit = 3;
        private const int RecentNotesPreviewLimit = 3;

        private readonly ApplicationDbContext _context;

        public GuestProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object?> GetDetailAsync(
            int guestId,
            int locationId,
            string locationName
        )
        {
            var locationGuest = await _context.LocationGuests
                .AsNoTracking()
                .Include(lg => lg.MasterGuest)
                .FirstOrDefaultAsync(lg =>
                    lg.Id == guestId
                    && lg.RestaurantLocationId == locationId
                );

            if (locationGuest == null)
            {
                return null;
            }

            var masterGuest = locationGuest.MasterGuest
                ?? throw new InvalidOperationException(
                    "Location guest is missing master guest."
                );

            var feedbackFacts = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.LocationGuestId == guestId
                    && f.RestaurantLocationId == locationId
                )
                .Select(f => new LocationGuestFeedbackFact(
                    f.CreatedAt,
                    f.ClassificationStatus,
                    f.Sentiment
                ))
                .ToListAsync();

            var feedbackStats = LocationGuestProjections.BuildFeedbackStats(
                feedbackFacts
            );

            var marketingStatus = LocationGuestProjections.DeriveMarketingStatus(
                locationGuest.OffersOptOut,
                masterGuest.Email,
                masterGuest.Mobile
            );

            var contactEligibility =
                LocationGuestProjections.BuildContactEligibility(
                    locationGuest.OffersOptOut,
                    masterGuest.Email,
                    masterGuest.Mobile
                );

            var feedbackCount = feedbackStats.FeedbackSubmissionCount;
            var lastActivityAt = feedbackStats.LastInteractionAt;

            var latestFeedbackRows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.LocationGuestId == guestId
                    && f.RestaurantLocationId == locationId
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(LatestFeedbackPreviewLimit)
                .ToListAsync();

            var latestFeedback = latestFeedbackRows
                .Select(f =>
                {
                    var classification =
                        FeedbackClassificationMapping.ToApiFields(f);

                    return new
                    {
                        id = f.Id,
                        createdAt = f.CreatedAt,
                        comment = f.Comment,
                        locationName,
                        classificationStatus =
                            classification.ClassificationStatus,
                        sentiment = classification.Sentiment,
                        detectedTags = classification.DetectedTags,
                    };
                })
                .ToList();

            var recentNotes = await _context.LocationGuestNotes
                .AsNoTracking()
                .Where(n => n.LocationGuestId == guestId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(RecentNotesPreviewLimit)
                .Select(n => new
                {
                    id = n.Id,
                    body = n.Body,
                    authorDisplayName = n.AuthorDisplayName,
                    createdAt = n.CreatedAt,
                })
                .ToListAsync();

            var guestTags = await _context.LocationGuestTags
                .AsNoTracking()
                .Where(m => m.LocationGuestId == guestId)
                .OrderBy(m => m.GuestTag!.DisplayName)
                .Select(m => new
                {
                    id = m.GuestTagId,
                    name = m.GuestTag!.DisplayName,
                })
                .ToListAsync();

            return new
            {
                success = true,
                locationId,
                id = locationGuest.Id,
                name = locationGuest.Name,
                marketingStatus,
                offersOptOut = locationGuest.OffersOptOut,
                guestSinceAt = locationGuest.CreatedAt,
                lastActivityAt,
                lastInteractionLabel = LastInteractionLabel,
                profileSummary = new
                {
                    email = masterGuest.Email,
                    mobile = masterGuest.Mobile,
                    firstCapturedAt = locationGuest.CreatedAt,
                    locationName,
                    feedbackSubmissionCount = feedbackCount,
                    offerClaimsAndRedemptions = 0,
                    lastInteractionAt = lastActivityAt,
                    lastInteractionLabel = LastInteractionLabel,
                    guestTags,
                },
                overviewDetails = new
                {
                    guestSinceAt = locationGuest.CreatedAt,
                    totalInteractions = feedbackCount,
                    feedbackReceived = feedbackCount,
                    offersClaimed = 0,
                    campaignsSent = 0,
                    lastActivityAt,
                },
                contactEligibility,
                latestFeedback,
                recentNotes,
            };
        }
    }
}
