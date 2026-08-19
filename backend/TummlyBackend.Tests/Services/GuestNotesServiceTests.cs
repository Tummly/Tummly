using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
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

        [Fact]
        public async Task UpdateAsync_OverwritesBody_AndSetsUpdatedAt_WithoutActivity()
        {
            var seeded = await SeedGuestWithAuthorAsync(
                authorFullName: "Ada Operator"
            );
            var created = await _notes.CreateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.AuthorUserId,
                "Original body"
            );
            Assert.NotNull(created);
            var activityBefore = await _context.LocationGuestActivityEvents.CountAsync();

            var updated = await _notes.UpdateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                created!.Id,
                seeded.AuthorUserId,
                "  Corrected body  "
            );

            Assert.NotNull(updated);
            Assert.Equal("Corrected body", updated!.Body);
            Assert.Equal("Ada Operator", updated.AuthorDisplayName);
            Assert.NotNull(updated.UpdatedAt);
            Assert.Equal(
                activityBefore,
                await _context.LocationGuestActivityEvents.CountAsync()
            );

            var persisted = await _context.LocationGuestNotes.SingleAsync();
            Assert.Equal("Corrected body", persisted.Body);
            Assert.Equal(seeded.AuthorUserId, persisted.LastEditedByUserId);
            Assert.Equal("Ada Operator", persisted.LastEditedByDisplayName);
            Assert.Null(persisted.DeletedAt);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenNoteSoftDeleted()
        {
            var seeded = await SeedGuestWithAuthorAsync();
            var created = await _notes.CreateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.AuthorUserId,
                "Gone soon"
            );
            Assert.NotNull(created);
            var deleted = await _notes.SoftDeleteAsync(
                    seeded.LocationGuestId,
                    seeded.LocationId,
                    created!.Id,
                    seeded.AuthorUserId
                );
            Assert.NotNull(deleted);

            var updated = await _notes.UpdateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                created.Id,
                seeded.AuthorUserId,
                "Revive attempt"
            );

            Assert.Null(updated);
            var persisted = await _context.LocationGuestNotes.SingleAsync();
            Assert.Equal("Gone soon", persisted.Body);
            Assert.NotNull(persisted.DeletedAt);
        }

        [Fact]
        public async Task SoftDeleteAsync_HidesFromList_AndRecordsNoteDeletedActivity()
        {
            var seeded = await SeedGuestWithAuthorAsync(
                authorFullName: "Ada Operator"
            );
            var created = await _notes.CreateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.AuthorUserId,
                "Mistake"
            );
            Assert.NotNull(created);

            var deleted = await _notes.SoftDeleteAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                created!.Id,
                seeded.AuthorUserId
            );

            Assert.NotNull(deleted);
            Assert.Equal("Ada Operator", deleted!.DeletedByDisplayName);

            var list = await _notes.ListAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                limit: 50
            );
            Assert.NotNull(list);
            Assert.Empty(list!.Items);
            Assert.Equal(0, list.TotalCount);

            var persisted = await _context.LocationGuestNotes.SingleAsync();
            Assert.NotNull(persisted.DeletedAt);
            Assert.Equal(seeded.AuthorUserId, persisted.DeletedByUserId);
            Assert.Equal("Ada Operator", persisted.DeletedByDisplayName);

            var kinds = await _context.LocationGuestActivityEvents
                .OrderBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .Select(e => e.Kind)
                .ToListAsync();
            Assert.Equal(
                new[]
                {
                    LocationGuestActivityKinds.NoteAdded,
                    LocationGuestActivityKinds.NoteDeleted,
                },
                kinds
            );
        }

        [Fact]
        public async Task SoftDeleteAsync_ReturnsFalse_WhenAlreadyDeleted()
        {
            var seeded = await SeedGuestWithAuthorAsync();
            var created = await _notes.CreateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.AuthorUserId,
                "Once"
            );
            Assert.NotNull(created);
            Assert.NotNull(
                await _notes.SoftDeleteAsync(
                    seeded.LocationGuestId,
                    seeded.LocationId,
                    created!.Id,
                    seeded.AuthorUserId
                )
            );

            var again = await _notes.SoftDeleteAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                created.Id,
                seeded.AuthorUserId
            );

            Assert.Null(again);
            Assert.Equal(
                1,
                await _context.LocationGuestActivityEvents.CountAsync(e =>
                    e.Kind == LocationGuestActivityKinds.NoteDeleted
                )
            );
        }

        [Fact]
        public async Task UpdateAsync_ThrowsArgument_WhenBodyWhitespaceOnly()
        {
            var seeded = await SeedGuestWithAuthorAsync();
            var created = await _notes.CreateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.AuthorUserId,
                "Keep me"
            );
            Assert.NotNull(created);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _notes.UpdateAsync(
                    seeded.LocationGuestId,
                    seeded.LocationId,
                    created!.Id,
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
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
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
