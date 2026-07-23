using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IGuestNotesService"/> — create resolves author
    /// display name from Users; list/create scoped to location guest.
    /// </summary>
    public class GuestNotesServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestNotesService _notes;

        public GuestNotesServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var recorder = new LocationGuestActivityRecorder(_context);
            _notes = new GuestNotesService(_context, recorder);
        }

        [Fact]
        public async Task CreateAsync_ResolvesAuthorDisplayNameFromUser()
        {
            var seeded = await SeedGuestWithAuthorAsync(
                authorFullName: "Ada Operator"
            );

            var note = await _notes.CreateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.AuthorUserId,
                "  Hello note  "
            );

            Assert.NotNull(note);
            Assert.Equal("Hello note", note!.Body);
            Assert.Equal("Ada Operator", note.AuthorDisplayName);
            Assert.True(note.Id > 0);

            var persisted = await _context.LocationGuestNotes.SingleAsync();
            Assert.Equal(seeded.AuthorUserId, persisted.AuthorUserId);
            Assert.Equal("Ada Operator", persisted.AuthorDisplayName);
        }

        [Fact]
        public async Task CreateAsync_ThrowsInvalidOperation_WhenAuthorMissing()
        {
            var seeded = await SeedGuestWithAuthorAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _notes.CreateAsync(
                    seeded.LocationGuestId,
                    seeded.LocationId,
                    authorUserId: 999_999,
                    "Note body"
                )
            );

            Assert.Equal("User not found.", ex.Message);
            Assert.Empty(_context.LocationGuestNotes.ToList());
        }

        [Fact]
        public async Task CreateAsync_ReturnsNull_WhenGuestMissing()
        {
            var seeded = await SeedGuestWithAuthorAsync();

            var note = await _notes.CreateAsync(
                locationGuestId: 999_999,
                seeded.LocationId,
                seeded.AuthorUserId,
                "Orphan note"
            );

            Assert.Null(note);
            Assert.Empty(_context.LocationGuestNotes.ToList());
        }

        [Fact]
        public async Task CreateAsync_ThrowsArgument_WhenBodyWhitespaceOnly()
        {
            var seeded = await SeedGuestWithAuthorAsync();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _notes.CreateAsync(
                    seeded.LocationGuestId,
                    seeded.LocationId,
                    seeded.AuthorUserId,
                    "   "
                )
            );

            Assert.Equal("Note body is required.", ex.Message);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<Seed> SeedGuestWithAuthorAsync(
            string authorFullName = "Notes Author"
        )
        {
            var user = new User
            {
                FullName = authorFullName,
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Notes Test Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = $"notes-svc-{Guid.NewGuid():N}".Substring(0, 28),
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{Guid.NewGuid():N}@guest.example.com",
                NormalizedEmail = $"{Guid.NewGuid():N}@guest.example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Test Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(locationGuest);
            await _context.SaveChangesAsync();

            return new Seed(user.Id, location.Id, locationGuest.Id);
        }

        private sealed record Seed(
            int AuthorUserId,
            int LocationId,
            int LocationGuestId
        );
    }
}
