using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class OwnedLocationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OwnedLocationService _service;

        public OwnedLocationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new OwnedLocationService(_context);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsFound_WhenOwnerMatches()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7);

            var result = await _service.ResolveAsync(7, locationId);

            Assert.Equal(OwnedLocationResolveStatus.Found, result.Status);
            Assert.NotNull(result.Location);
            Assert.Equal(locationId, result.Location.Id);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsForbidden_WhenOwnerDiffers()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7);

            var result = await _service.ResolveAsync(99, locationId);

            Assert.Equal(OwnedLocationResolveStatus.Forbidden, result.Status);
            Assert.Null(result.Location);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsNotFound_WhenLocationMissing()
        {
            var result = await _service.ResolveAsync(7, 404);

            Assert.Equal(OwnedLocationResolveStatus.NotFound, result.Status);
            Assert.Null(result.Location);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsForbidden_WhenUserIdDoesNotMatchAnyOwner()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7);

            var result = await _service.ResolveAsync(0, locationId);

            Assert.Equal(OwnedLocationResolveStatus.Forbidden, result.Status);
            Assert.Null(result.Location);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedLocationAsync(int ownerUserId)
        {
            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = "Single",
                OwnerUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = "test-location-token1234567890",
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow
            };

            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            return location.Id;
        }
    }
}
