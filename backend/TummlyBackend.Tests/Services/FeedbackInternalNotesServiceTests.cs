using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IFeedbackInternalNotesService"/> — update/soft-delete,
    /// list excludes deleted, activity facts keep note-added and add note-deleted.
    /// </summary>
    public class FeedbackInternalNotesServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackInternalNotesService _notes;

        public FeedbackInternalNotesServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _notes = new FeedbackInternalNotesService(_context);
        }

        [Fact]
        public async Task UpdateAsync_OverwritesBody_WithoutActivityFactChange()
        {
            var seeded = await SeedFeedbackWithAuthorAsync();
            var created = await _notes.CreateAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                "Original"
            );
            Assert.NotNull(created);

            var updated = await _notes.UpdateAsync(
                seeded.FeedbackId,
                created!.Id,
                seeded.AuthorUserId,
                "  Fixed typo  "
            );

            Assert.NotNull(updated);
            Assert.Equal("Fixed typo", updated!.Body);
            Assert.NotNull(updated.UpdatedAt);

            var facts = await _notes.ListActivityFactsForFeedbackAsync(
                seeded.FeedbackId
            );
            Assert.Single(facts);
            Assert.Null(facts[0].DeletedAt);
        }

        [Fact]
        public async Task SoftDeleteAsync_ExcludesFromList_KeepsActivityFactWithDeletedAt()
        {
            var seeded = await SeedFeedbackWithAuthorAsync(
                authorFullName: "Ops Lead"
            );
            var created = await _notes.CreateAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                "Remove me"
            );
            Assert.NotNull(created);

            Assert.NotNull(
                await _notes.SoftDeleteAsync(
                    seeded.FeedbackId,
                    created!.Id,
                    seeded.AuthorUserId
                )
            );

            var visible = await _notes.ListForFeedbackAsync(seeded.FeedbackId);
            Assert.Empty(visible);

            var facts = await _notes.ListActivityFactsForFeedbackAsync(
                seeded.FeedbackId
            );
            Assert.Single(facts);
            Assert.NotNull(facts[0].DeletedAt);
            Assert.Equal("Ops Lead", facts[0].DeletedByDisplayName);

            var history = FeedbackActivityHistory.Derive(
                seeded.FeedbackCreatedAt,
                facts
            );
            Assert.Contains(history, e => e.Kind == "note_added");
            Assert.Contains(history, e => e.Kind == "note_deleted");
            Assert.DoesNotContain(history, e => e.Kind == "note_edited");
        }

        [Fact]
        public async Task SoftDeleteAsync_ReturnsFalse_WhenAlreadyDeleted()
        {
            var seeded = await SeedFeedbackWithAuthorAsync();
            var created = await _notes.CreateAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                "Once"
            );
            Assert.NotNull(created);
            Assert.NotNull(
                await _notes.SoftDeleteAsync(
                    seeded.FeedbackId,
                    created!.Id,
                    seeded.AuthorUserId
                )
            );

            Assert.Null(
                await _notes.SoftDeleteAsync(
                    seeded.FeedbackId,
                    created.Id,
                    seeded.AuthorUserId
                )
            );
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenSoftDeleted()
        {
            var seeded = await SeedFeedbackWithAuthorAsync();
            var created = await _notes.CreateAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                "Gone"
            );
            Assert.NotNull(created);
            Assert.NotNull(
                await _notes.SoftDeleteAsync(
                    seeded.FeedbackId,
                    created!.Id,
                    seeded.AuthorUserId
                )
            );

            var updated = await _notes.UpdateAsync(
                seeded.FeedbackId,
                created.Id,
                seeded.AuthorUserId,
                "Nope"
            );

            Assert.Null(updated);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<Seed> SeedFeedbackWithAuthorAsync(
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
                Name = "Feedback Notes Venue",
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

            var createdAt = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);
            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Pat Guest",
                GuestContact = "pat@example.com",
                ContactType = ContactType.Email,
                Comment = "Great meal",
                OffersOptOut = false,
                CreatedAt = createdAt,
                ClassificationStatus = ClassificationStatus.Pending,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return new Seed(user.Id, feedback.Id, createdAt);
        }

        private sealed record Seed(
            int AuthorUserId,
            int FeedbackId,
            DateTime FeedbackCreatedAt
        );
    }
}
