using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Exceptions;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class SmartGuestLinkServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly SmartGuestLinkService _service;

        public SmartGuestLinkServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example"
                })
                .Build();

            _service = new SmartGuestLinkService(_context, configuration);
        }

        [Fact]
        public async Task GenerateTokenAsync_Returns32CharAlphanumericToken()
        {
            var token = await _service.GenerateTokenAsync();

            Assert.Equal(32, token.Length);
            Assert.Matches("^[A-Za-z0-9]{32}$", token);
        }

        [Fact]
        public async Task GenerateTokenAsync_RetriesWhenTokenExistsInDatabase()
        {
            await SeedLocationAsync("existing-token-1234567890123456");

            var token = await _service.GenerateTokenAsync();

            Assert.Equal(32, token.Length);
            Assert.NotEqual("existing-token-1234567890123456", token);
        }

        [Fact]
        public async Task GenerateTokenAsync_AvoidsPendingChangeTrackerTokens()
        {
            _context.RestaurantLocations.Add(new RestaurantLocation
            {
                RestaurantId = 1,
                LinkToken = "pending-token-123456789012345678",
                LocationName = "Pending",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow
            });

            var token = await _service.GenerateTokenAsync();

            Assert.NotEqual("pending-token-123456789012345678", token);
        }

        [Fact]
        public async Task GenerateTokenAsync_ThrowsAfterRetryCap()
        {
            const string collidingToken =
                "collision-token-123456789012345678";

            await SeedLocationAsync(collidingToken);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example"
                })
                .Build();

            var alwaysCollidingService = new FixedTokenSmartGuestLinkService(
                _context,
                configuration,
                collidingToken
            );

            await Assert.ThrowsAsync<LinkTokenGenerationException>(() =>
                alwaysCollidingService.GenerateTokenAsync()
            );
        }

        [Fact]
        public async Task ResolveForGuestAsync_ReturnsMetadata_ForValidToken()
        {
            await SeedLocationAsync(
                "guest-token-123456789012345678",
                restaurantName: "The Golden Fork",
                locationName: "Main"
            );

            var result = await _service.ResolveForGuestAsync(
                "guest-token-123456789012345678"
            );

            Assert.NotNull(result);
            Assert.Equal("The Golden Fork", result!.RestaurantName);
            Assert.Equal("Main", result.LocationName);
        }

        [Fact]
        public async Task ResolveForGuestAsync_TrimsWhitespace()
        {
            await SeedLocationAsync("trim-token-12345678901234567890");

            var result = await _service.ResolveForGuestAsync(
                "  trim-token-12345678901234567890  "
            );

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ResolveForGuestAsync_ReturnsNull_ForUnknownToken()
        {
            var result = await _service.ResolveForGuestAsync("missing-token");

            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveLocationForWriteAsync_ReturnsTrackedEntity()
        {
            await SeedLocationAsync("write-token-1234567890123456789");

            var location = await _service.ResolveLocationForWriteAsync(
                "write-token-1234567890123456789"
            );

            Assert.NotNull(location);
            Assert.Equal(
                EntityState.Unchanged,
                _context.Entry(location!).State
            );
        }

        [Fact]
        public void BuildGuestUrl_ReturnsCanonicalShape()
        {
            var url = _service.BuildGuestUrl("abc123");

            Assert.Equal("https://tummly.example/scan/abc123", url);
        }

        [Fact]
        public void BuildGuestUrl_ThrowsWhenConfigMissing()
        {
            var configuration = new ConfigurationBuilder().Build();
            var service = new SmartGuestLinkService(_context, configuration);

            Assert.Throws<InvalidOperationException>(() =>
                service.BuildGuestUrl("abc123")
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task SeedLocationAsync(
            string linkToken,
            string restaurantName = "Test Restaurant",
            string locationName = "Main"
        )
        {
            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.RestaurantLocations.Add(new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = linkToken,
                LocationName = locationName,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private sealed class FixedTokenSmartGuestLinkService
            : SmartGuestLinkService
        {
            private readonly string _fixedToken;

            public FixedTokenSmartGuestLinkService(
                ApplicationDbContext context,
                IConfiguration configuration,
                string fixedToken
            ) : base(context, configuration)
            {
                _fixedToken = fixedToken;
            }

            protected override string CreateRandomToken() => _fixedToken;
        }
    }
}
