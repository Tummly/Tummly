using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ILocationGuestActivityRecorder"/> — Record* appends
    /// Location Guest activity events (kind + payload) onto the current
    /// DbContext; caller owns SaveChanges.
    /// </summary>
    public class LocationGuestActivityRecorderTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestActivityRecorder _recorder;

        public LocationGuestActivityRecorderTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _recorder = new LocationGuestActivityRecorder(_context);
        }

        [Fact]
        public async Task RecordGuestJoined_AppendsGuestJoinedKind_WithoutSave()
        {
            var guest = await SeedLocationGuestAsync();
            var occurredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

            _recorder.RecordGuestJoined(guest, occurredAt);

            Assert.Empty(_context.LocationGuestActivityEvents.ToList());

            var pending = _context.ChangeTracker
                .Entries<LocationGuestActivityEvent>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            Assert.Single(pending);
            Assert.Equal(LocationGuestActivityKinds.GuestJoined, pending[0].Kind);
            Assert.Same(guest, pending[0].LocationGuest);
            Assert.Equal(occurredAt, pending[0].OccurredAt);
            Assert.Null(pending[0].PayloadJson);
        }

        [Fact]
        public async Task RecordNoteAdded_SerializesAuthorDisplayName()
        {
            var guest = await SeedLocationGuestAsync();
            var occurredAt = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);

            _recorder.RecordNoteAdded(guest.Id, "Operator A", occurredAt);
            await _context.SaveChangesAsync();

            var row = await _context.LocationGuestActivityEvents.SingleAsync();
            Assert.Equal(LocationGuestActivityKinds.NoteAdded, row.Kind);
            var payload = LocationGuestActivityPayload.Deserialize(row.PayloadJson);
            Assert.NotNull(payload);
            Assert.Equal("Operator A", payload!.AuthorDisplayName);
        }

        [Fact]
        public async Task RecordNoteDeleted_SerializesActorDisplayName()
        {
            var guest = await SeedLocationGuestAsync();
            var occurredAt = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

            _recorder.RecordNoteDeleted(guest.Id, "Operator B", occurredAt);
            await _context.SaveChangesAsync();

            var row = await _context.LocationGuestActivityEvents.SingleAsync();
            Assert.Equal(LocationGuestActivityKinds.NoteDeleted, row.Kind);
            var payload = LocationGuestActivityPayload.Deserialize(row.PayloadJson);
            Assert.NotNull(payload);
            Assert.Equal("Operator B", payload!.AuthorDisplayName);
        }

        [Fact]
        public async Task RecordClassificationTerminal_SkipsWhenPending()
        {
            var guest = await SeedLocationGuestAsync();
            var feedback = new Feedback
            {
                RestaurantLocationId = guest.RestaurantLocationId,
                LocationGuestId = guest.Id,
                GuestName = "Pat",
                GuestContact = "pat@example.com",
                ContactType = ContactType.Email,
                Comment = "ok",
                ClassificationStatus = ClassificationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            _recorder.RecordClassificationTerminal(feedback, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            Assert.Empty(await _context.LocationGuestActivityEvents.ToListAsync());
        }

        [Fact]
        public async Task RecordClassificationTerminal_Succeeded_WritesSentimentPayload()
        {
            var guest = await SeedLocationGuestAsync();
            var feedback = new Feedback
            {
                RestaurantLocationId = guest.RestaurantLocationId,
                LocationGuestId = guest.Id,
                GuestName = "Pat",
                GuestContact = "pat@example.com",
                ContactType = ContactType.Email,
                Comment = "great",
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Positive,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            var occurredAt = new DateTime(2026, 7, 3, 15, 0, 0, DateTimeKind.Utc);
            _recorder.RecordClassificationTerminal(feedback, occurredAt);
            await _context.SaveChangesAsync();

            var row = await _context.LocationGuestActivityEvents.SingleAsync();
            Assert.Equal(
                LocationGuestActivityKinds.ClassificationSucceeded,
                row.Kind
            );
            Assert.Equal(feedback.Id, row.FeedbackId);
            var payload = LocationGuestActivityPayload.Deserialize(row.PayloadJson);
            Assert.NotNull(payload);
            Assert.Equal("positive", payload!.Sentiment);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<LocationGuest> SeedLocationGuestAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                LinkToken = Guid.NewGuid().ToString("N")[..16],
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
            return guest;
        }
    }
}
