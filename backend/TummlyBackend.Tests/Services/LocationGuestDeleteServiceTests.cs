using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ILocationGuestDeleteService"/> — owns Owned-location
    /// authz and Location Guest hard-delete policy.
    /// </summary>
    public class LocationGuestDeleteServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestDeleteService _delete;

        public LocationGuestDeleteServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var ownedLocation = new OwnedLocationService(_context);
            _delete = new LocationGuestDeleteService(_context, ownedLocation);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsForbidden_WhenUserDoesNotOwnLocation()
        {
            var seeded = await SeedOwnedGuestAsync();
            var otherUserId = seeded.OwnerUserId + 999;

            var outcome = await _delete.DeleteAsync(
                otherUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Forbidden, outcome.Status);
            Assert.Equal(
                "You do not have access to this location.",
                outcome.ErrorMessage
            );
            Assert.True(
                await _context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenLocationMissing()
        {
            var seeded = await SeedOwnedGuestAsync();

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                locationId: 999_999
            );

            Assert.Equal(LocationGuestDeleteStatus.NotFound, outcome.Status);
            Assert.Equal("Location not found.", outcome.ErrorMessage);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenGuestMissingOrWrongLocation()
        {
            var seeded = await SeedOwnedGuestAsync();

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                locationGuestId: 999_999,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.NotFound, outcome.Status);
            Assert.Equal("Guest not found.", outcome.ErrorMessage);
        }

        [Fact]
        public async Task DeleteAsync_Deletes_WhenOwned()
        {
            var seeded = await SeedOwnedGuestAsync();

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);
            Assert.False(
                await _context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<SeededGuest> SeedOwnedGuestAsync()
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
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

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "pat@example.com",
                NormalizedEmail = "pat@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Pat",
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            return new SeededGuest(user.Id, location.Id, guest.Id);
        }

        private sealed record SeededGuest(
            int OwnerUserId,
            int LocationId,
            int LocationGuestId
        );
    }
}
