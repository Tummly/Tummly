using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: Home recommendation click create-or-get for Campaign Drafts.
    /// One open Draft per Location and recommendation source; reuse keeps edits.
    /// </summary>
    public class CampaignRecommendationDraftTests : IDisposable
    {
        private const int UserId = 7;

        private readonly ApplicationDbContext _context;
        private readonly CampaignDraftService _drafts;

        public CampaignRecommendationDraftTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _drafts = new CampaignDraftService(
                _context,
                new CampaignTemplateCatalogueService(),
                new OffersCatalogService(_context)
            );
        }

        [Fact]
        public async Task CreateOrGet_RepeatClick_ReturnsSameDraftAndDoesNotCreateAnother()
        {
            var locationId = await SeedLocationAsync();
            var request = Recommendation(locationId);

            var first = await _drafts.CreateOrGetFromRecommendationAsync(request, UserId);
            var second = await _drafts.CreateOrGetFromRecommendationAsync(request, UserId);

            Assert.False(first.Reused);
            Assert.True(second.Reused);
            Assert.Equal(first.Campaign.Id, second.Campaign.Id);
            Assert.Equal(1, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task CreateOrGet_StoresRecommendationSourceGoalAndLocation()
        {
            var locationId = await SeedLocationAsync();

            var result = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);

            var row = await _context.Campaigns.SingleAsync(c => c.Id == result.Campaign.Id);
            Assert.Equal(locationId, row.RestaurantLocationId);
            Assert.Equal("thank-recent-guests", row.GoalId);
            Assert.Equal(CampaignDraftService.DraftStatus, row.Status);
            Assert.False(string.IsNullOrWhiteSpace(row.RecommendationSourceKey));
        }

        [Fact]
        public async Task CreateOrGet_ReusedDraftKeepsUserEdits()
        {
            var locationId = await SeedLocationAsync();
            var first = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);
            var row = await _context.Campaigns.SingleAsync(c => c.Id == first.Campaign.Id);
            row.Name = "Edited by operator";
            row.MessageBody = "Edited body";
            await _context.SaveChangesAsync();

            var second = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);

            Assert.Equal(first.Campaign.Id, second.Campaign.Id);
            Assert.Equal("Edited by operator", second.Campaign.Name);
            Assert.Equal("Edited body", second.Campaign.MessageBody);
        }

        [Theory]
        [InlineData("scheduled")]
        [InlineData("sending")]
        [InlineData("sent")]
        [InlineData("partially-sent")]
        [InlineData("failed")]
        [InlineData("cancelled")]
        public async Task CreateOrGet_AfterDraftLeftDraftStatus_CreatesNewDraft(string nextStatus)
        {
            var locationId = await SeedLocationAsync();
            var first = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);
            var row = await _context.Campaigns.SingleAsync(c => c.Id == first.Campaign.Id);
            row.Status = nextStatus;
            await _context.SaveChangesAsync();

            var second = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);

            Assert.False(second.Reused);
            Assert.NotEqual(first.Campaign.Id, second.Campaign.Id);
        }

        [Fact]
        public async Task CreateOrGet_MateriallyDifferentRecommendation_CreatesSeparateDraft()
        {
            var locationId = await SeedLocationAsync();
            var first = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);

            var other = await _drafts.CreateOrGetFromRecommendationAsync(
                Recommendation(locationId, goalId: "re-engage-inactive", audienceKey: "dormant-guests"),
                UserId
            );

            Assert.False(other.Reused);
            Assert.NotEqual(first.Campaign.Id, other.Campaign.Id);
            Assert.Equal(2, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task CreateOrGet_OtherLocationIsIsolated()
        {
            var locationA = await SeedLocationAsync();
            var locationB = await SeedLocationAsync();
            var first = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationA), UserId);

            var second = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationB), UserId);

            Assert.False(second.Reused);
            Assert.NotEqual(first.Campaign.Id, second.Campaign.Id);
            Assert.Equal(locationB, second.Campaign.LocationId);
        }

        [Fact]
        public async Task CreateOrGet_UnknownRecommendationType_IsRejected()
        {
            var locationId = await SeedLocationAsync();
            var request = Recommendation(locationId);
            request = new CreateRecommendationCampaignDraftRequest
            {
                RecommendationType = "review-open-feedback",
                Draft = request.Draft,
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _drafts.CreateOrGetFromRecommendationAsync(request, UserId));
        }

        [Fact]
        public async Task CreateAsync_ByHand_IsNeverReusedAsRecommendationDraft()
        {
            var locationId = await SeedLocationAsync();
            var handMade = Recommendation(locationId).Draft;

            var first = await _drafts.CreateAsync(handMade, UserId);
            var second = await _drafts.CreateAsync(handMade, UserId);

            Assert.NotEqual(first.Id, second.Id);
            var rows = await _context.Campaigns.ToListAsync();
            Assert.All(rows, row => Assert.Null(row.RecommendationSourceKey));
        }

        [Fact]
        public async Task CreateOrGet_RecentRedeemersAudience_StoresThirtyDayLookbackInLondon()
        {
            var locationId = await SeedLocationAsync();
            var request = Recommendation(locationId, audienceKey: "recent-redeemers");

            var result = await _drafts.CreateOrGetFromRecommendationAsync(request, UserId);

            Assert.Equal(30, result.Campaign.AudienceLookbackDays);
            Assert.Equal("Europe/London", result.Campaign.AudienceLookbackTimeZone);
        }

        [Fact]
        public async Task CreateOrGet_OtherAudience_StoresNoLookback()
        {
            var locationId = await SeedLocationAsync();

            var result = await _drafts.CreateOrGetFromRecommendationAsync(Recommendation(locationId), UserId);

            Assert.Null(result.Campaign.AudienceLookbackDays);
            Assert.Null(result.Campaign.AudienceLookbackTimeZone);
        }

        private static CreateRecommendationCampaignDraftRequest Recommendation(
            int locationId,
            string goalId = "thank-recent-guests",
            string audienceKey = "new-guests"
        )
        {
            return new CreateRecommendationCampaignDraftRequest
            {
                RecommendationType = "thank-recent-guests",
                Draft = new CreateCampaignDraftRequest
                {
                    LocationId = locationId,
                    Name = "Thank recent guests",
                    GoalId = goalId,
                    AudienceKey = audienceKey,
                    Channel = "email",
                    OfferStance = "no-offer",
                    MessageSubject = "Thanks",
                    MessageBody = "Thanks for visiting",
                },
            };
        }

        private async Task<int> SeedLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Venue",
                AccountType = "Single",
                OwnerUserId = UserId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
