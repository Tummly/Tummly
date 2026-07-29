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
            await SeedQrCodeAsync("existing-token-1234567890123456");

            var token = await _service.GenerateTokenAsync();

            Assert.Equal(32, token.Length);
            Assert.NotEqual("existing-token-1234567890123456", token);
        }

        [Fact]
        public async Task GenerateTokenAsync_AvoidsPendingChangeTrackerTokens()
        {
            var location = await CreateLocationAsync();

            _context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = "pending-token-123456789012345678",
                Status = QrCodeStatus.Active,
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

            await SeedQrCodeAsync(collidingToken);

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
            await SeedQrCodeAsync(
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
            Assert.Equal(QrType.SmartGuest, result.QrType);
            Assert.True(result.QrCodeId > 0);
        }

        [Fact]
        public async Task ResolveForGuestAsync_TrimsWhitespace()
        {
            await SeedQrCodeAsync("trim-token-12345678901234567890");

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

        [Theory]
        [InlineData(QrCodeStatus.Paused)]
        [InlineData(QrCodeStatus.Archived)]
        public async Task ResolveForGuestAsync_ReturnsNull_ForInactiveQrCode(
            QrCodeStatus status
        )
        {
            await SeedQrCodeAsync("inactive-token-1234567890123456", status: status);

            var result = await _service.ResolveForGuestAsync(
                "inactive-token-1234567890123456"
            );

            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveLocationForWriteAsync_ReturnsTrackedLocationAndQrCode()
        {
            var location = await CreateLocationAsync();
            var qrCode = await AddQrCodeAsync(
                location,
                "write-token-1234567890123456789",
                QrType.CounterCard
            );

            var resolution = await _service.ResolveLocationForWriteAsync(
                "write-token-1234567890123456789"
            );

            Assert.NotNull(resolution);
            Assert.Equal(location.Id, resolution!.Location.Id);
            Assert.Equal(qrCode.Id, resolution.QrCodeId);
            Assert.Equal(QrType.CounterCard, resolution.QrType);
            Assert.Equal(
                EntityState.Unchanged,
                _context.Entry(resolution.Location).State
            );
        }

        [Theory]
        [InlineData(QrCodeStatus.Paused)]
        [InlineData(QrCodeStatus.Archived)]
        public async Task ResolveLocationForWriteAsync_ReturnsNull_ForInactiveQrCode(
            QrCodeStatus status
        )
        {
            await SeedQrCodeAsync(
                "inactive-write-token-123456789012",
                status: status
            );

            var resolution = await _service.ResolveLocationForWriteAsync(
                "inactive-write-token-123456789012"
            );

            Assert.Null(resolution);
        }

        [Fact]
        public async Task GetActiveSmartGuestTokenAsync_ReturnsToken_WhenActive()
        {
            var location = await CreateLocationAsync();
            await AddQrCodeAsync(location, "active-smart-guest-token12345", QrType.SmartGuest);

            var token = await _service.GetActiveSmartGuestTokenAsync(location.Id);

            Assert.Equal("active-smart-guest-token12345", token);
        }

        [Fact]
        public async Task GetActiveSmartGuestTokenAsync_ReturnsNull_WhenNoneActive()
        {
            var location = await CreateLocationAsync();
            await AddQrCodeAsync(
                location,
                "paused-smart-guest-token12345",
                QrType.SmartGuest,
                QrCodeStatus.Paused
            );

            var token = await _service.GetActiveSmartGuestTokenAsync(location.Id);

            Assert.Null(token);
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

        private async Task<RestaurantLocation> CreateLocationAsync(
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

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow
            };

            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            return location;
        }

        private async Task<QrCode> AddQrCodeAsync(
            RestaurantLocation location,
            string token,
            QrType qrType = QrType.SmartGuest,
            QrCodeStatus status = QrCodeStatus.Active
        )
        {
            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = qrType,
                Token = token,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.QrCodes.Add(qrCode);
            await _context.SaveChangesAsync();

            return qrCode;
        }

        private async Task SeedQrCodeAsync(
            string token,
            string restaurantName = "Test Restaurant",
            string locationName = "Main",
            QrCodeStatus status = QrCodeStatus.Active
        )
        {
            var location = await CreateLocationAsync(restaurantName, locationName);
            await AddQrCodeAsync(location, token, QrType.SmartGuest, status);
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
