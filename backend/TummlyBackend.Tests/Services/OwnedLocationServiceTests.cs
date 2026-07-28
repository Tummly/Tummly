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

        [Fact]
        public async Task ListOwnedLocationIdsAsync_ReturnsOwnedIdsForRestaurant()
        {
            var seeded = await SeedRestaurantWithLocationsAsync(
                ownerUserId: 7,
                locationCount: 2
            );
            var otherRestaurant = await SeedRestaurantWithLocationsAsync(
                ownerUserId: 7,
                locationCount: 1,
                nameSuffix: "Other"
            );

            var ids = await _service.ListOwnedLocationIdsAsync(
                seeded.RestaurantId,
                userId: 7
            );

            Assert.Equal(2, ids.Count);
            Assert.Contains(seeded.LocationIds[0], ids);
            Assert.Contains(seeded.LocationIds[1], ids);
            Assert.DoesNotContain(otherRestaurant.LocationIds[0], ids);
        }

        [Fact]
        public async Task ListOwnedLocationIdsAsync_ReturnsEmpty_WhenOwnerDoesNotMatch()
        {
            var seeded = await SeedRestaurantWithLocationsAsync(
                ownerUserId: 7,
                locationCount: 2
            );

            var ids = await _service.ListOwnedLocationIdsAsync(
                seeded.RestaurantId,
                userId: 99
            );

            Assert.Empty(ids);
        }

        [Fact]
        public async Task ListOwnedLocationIdsAsync_ReturnsEmpty_WhenRestaurantMissing()
        {
            var ids = await _service.ListOwnedLocationIdsAsync(
                restaurantId: 404,
                userId: 7
            );

            Assert.Empty(ids);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedLocationAsync(int ownerUserId)
        {
            var seeded = await SeedRestaurantWithLocationsAsync(
                ownerUserId,
                locationCount: 1
            );
            return seeded.LocationIds[0];
        }

        private async Task<(int RestaurantId, IReadOnlyList<int> LocationIds)>
            SeedRestaurantWithLocationsAsync(
                int ownerUserId,
                int locationCount,
                string nameSuffix = ""
            )
        {
            var restaurant = new Restaurant
            {
                Name = $"Test Restaurant{nameSuffix}",
                AccountType = "Single",
                OwnerUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var locationIds = new List<int>();
            for (var i = 0; i < locationCount; i++)
            {
                var location = new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = $"Location {i + 1}",
                    Address = $"{i + 1} High Street",
                    CreatedAt = DateTime.UtcNow
                };

                _context.RestaurantLocations.Add(location);
                await _context.SaveChangesAsync();
                locationIds.Add(location.Id);
            }

            return (restaurant.Id, locationIds);
        }
    }
}
