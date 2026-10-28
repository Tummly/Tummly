using Microsoft.EntityFrameworkCore;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.SqlServer
{
    /// <summary>
    /// Concurrent Home recommendation clicks for one Location and recommendation.
    /// The filtered unique index <c>IX_Campaigns_RestaurantLocationId_RecommendationSourceKey</c>
    /// admits one open Draft. Callers that lose the insert race get that Draft back.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public sealed class CampaignRecommendationDraftConcurrentSqlTests
    {
        private const int ConcurrentCallers = 4;

        private readonly SqlServerFixture _fixture;

        public CampaignRecommendationDraftConcurrentSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task ConcurrentCreateOrGet_ForOneRecommendation_StoresOneDraftAndReturnsItToAll()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var (userId, locationId) = await SeedLocationAsync();

            var results = await Task.WhenAll(
                Enumerable
                    .Range(0, ConcurrentCallers)
                    .Select(_ => CreateOrGetAsync(userId, locationId))
            );

            var ids = results.Select(result => result.Campaign.Id).Distinct().ToList();
            Assert.Single(ids);
            Assert.Equal(1, results.Count(result => !result.Reused));
            Assert.Equal(ConcurrentCallers - 1, results.Count(result => result.Reused));

            await using var verify = _fixture.CreateContext();
            Assert.Equal(
                1,
                await verify.Campaigns.CountAsync(row =>
                    row.RestaurantLocationId == locationId
                    && row.RecommendationSourceKey != null
                )
            );
        }

        private async Task<CampaignRecommendationDraftResult> CreateOrGetAsync(int userId, int locationId)
        {
            await using var context = _fixture.CreateContext();
            var service = new CampaignDraftService(
                context,
                new CampaignTemplateCatalogueService(),
                new OffersCatalogService(context)
            );
            return await service.CreateOrGetFromRecommendationAsync(
                new CreateRecommendationCampaignDraftRequest
                {
                    RecommendationType = "thank-recent-guests",
                    Draft = new CreateCampaignDraftRequest
                    {
                        LocationId = locationId,
                        Name = "Thank recent guests",
                        GoalId = "thank-recent-guests",
                        AudienceKey = "new-guests",
                        Channel = "email",
                        OfferStance = "no-offer",
                    },
                },
                createdByUserId: userId
            );
        }

        private async Task<(int UserId, int LocationId)> SeedLocationAsync()
        {
            await using var context = _fixture.CreateContext();
            var user = new User
            {
                FullName = "Race Owner",
                Email = $"race-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Race Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Race Site",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            return (user.Id, location.Id);
        }
    }
}
