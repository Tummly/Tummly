using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Search;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: one entity path failing must not empty the whole Global Search response.
    /// </summary>
    public sealed class GlobalSearchServicePartialFailureTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public GlobalSearchServicePartialFailureTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SearchAsync_WhenOneEntityThrows_ReturnsOtherGroupsAndPartialFailures()
        {
            var restaurant = new Restaurant
            {
                Name = "Partial Fail Cafe",
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High St",
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "mo@example.com",
                NormalizedEmail = "mo@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            _context.LocationGuests.Add(
                new LocationGuest
                {
                    RestaurantLocationId = location.Id,
                    MasterGuestId = master.Id,
                    Name = "Mohamed",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            var service = new ThrowingCampaignsGlobalSearchService(_context);
            var response = await service.SearchAsync(
                new GlobalSearchQuery
                {
                    Q = "mo",
                    LocationId = location.Id,
                    LocationIds = [location.Id],
                    LocationNamesById = new Dictionary<int, string>
                    {
                        [location.Id] = location.LocationName,
                    },
                    Limit = 5,
                    IncludeGuests = true,
                    IncludeFeedback = false,
                    IncludeCampaigns = true,
                    IncludeOffers = false,
                    IncludeQrCodes = false,
                }
            );

            Assert.True(response.Success);
            Assert.Contains(response.PartialFailures, type => type == "campaigns");
            Assert.DoesNotContain(
                response.PartialFailures,
                type => type == "guests"
            );
            var guests = Assert.Single(
                response.Groups,
                group => group.Type == "guests"
            );
            Assert.NotEmpty(guests.Hits);
            Assert.DoesNotContain(response.Groups, group => group.Type == "campaigns");
        }

        private sealed class ThrowingCampaignsGlobalSearchService : GlobalSearchService
        {
            public ThrowingCampaignsGlobalSearchService(ApplicationDbContext context)
                : base(context)
            {
            }

            protected override Task<GlobalSearchGroupDto> SearchCampaignsCoreAsync(
                GlobalSearchQuery query,
                CancellationToken cancellationToken
            )
            {
                throw new InvalidOperationException("simulated campaigns failure");
            }
        }
    }
}
