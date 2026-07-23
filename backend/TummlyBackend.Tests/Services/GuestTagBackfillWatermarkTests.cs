using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class GuestTagBackfillWatermarkTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CountingGuestTagging _tagging;
        private readonly GuestTagBackfillService _sut;

        public GuestTagBackfillWatermarkTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _tagging = new CountingGuestTagging();
            _sut = new GuestTagBackfillService(_context, _tagging);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task BackfillAsync_WhenWatermarkSet_SkipsHeavyWork()
        {
            await SeedSucceededFeedbackAsync();
            _context.DataMigrationMarkers.Add(
                new DataMigrationMarker
                {
                    Id = DataMigrationMarkerIds.GuestTagBackfill,
                    CompletedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            await _sut.BackfillAsync();

            Assert.Equal(0, _tagging.UnionCallCount);
        }

        [Fact]
        public async Task BackfillAsync_WhenNotComplete_RunsThenSetsWatermark()
        {
            await SeedSucceededFeedbackAsync();

            await _sut.BackfillAsync();

            Assert.True(_tagging.UnionCallCount >= 1);
            Assert.True(
                await _context.DataMigrationMarkers.AnyAsync(
                    m => m.Id == DataMigrationMarkerIds.GuestTagBackfill
                )
            );
        }

        [Fact]
        public async Task BackfillAsync_SecondRun_IsIdempotentAndSkipsTagging()
        {
            await SeedSucceededFeedbackAsync();

            await _sut.BackfillAsync();
            var callsAfterFirst = _tagging.UnionCallCount;
            Assert.True(callsAfterFirst >= 1);

            await _sut.BackfillAsync();

            Assert.Equal(callsAfterFirst, _tagging.UnionCallCount);
            Assert.Equal(
                1,
                await _context.DataMigrationMarkers.CountAsync(
                    m => m.Id == DataMigrationMarkerIds.GuestTagBackfill
                )
            );
        }

        private async Task SeedSucceededFeedbackAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Tag Watermark Bistro",
                AccountType = "Single",
                OwnerUserId = 3,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = $"tag-wm-{Guid.NewGuid():N}"[..32],
                LocationName = "Main",
                Address = "3 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "tag-wm@example.com",
                NormalizedEmail = "tag-wm@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Tagged",
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    LocationGuestId = guest.Id,
                    GuestName = "Tagged",
                    GuestContact = "tag-wm@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Classified",
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Positive,
                    DetectedTagsJson =
                        FeedbackClassificationMapping.SerializeDetectedTags(
                            new[] { DetectedTag.Service }
                        ),
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
        }

        private sealed class CountingGuestTagging : IGuestTaggingService
        {
            public int UnionCallCount { get; private set; }

            public Task<GuestTag> CreateByNameAsync(
                int restaurantId,
                string name,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<IReadOnlyList<GuestTagPickerItem>> ListForLocationScopeAsync(
                int restaurantId,
                IReadOnlyList<int> locationIds,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task ApplyAdditiveAsync(
                int restaurantId,
                IReadOnlyList<int> locationIds,
                IReadOnlyList<int> locationGuestIds,
                IReadOnlyList<int> guestTagIds,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task SyncMembershipsAsync(
                int restaurantId,
                IReadOnlyList<int> locationIds,
                IReadOnlyList<int> locationGuestIds,
                IReadOnlyList<int> guestTagIds,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetMembershipsForGuestsAsync(
                int restaurantId,
                IReadOnlyList<int> locationIds,
                IReadOnlyList<int> locationGuestIds,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<GuestTag> EnsureFromDetectedTagAsync(
                int restaurantId,
                DetectedTag detectedTag,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task UnionDetectedTagsFromFeedbackAsync(
                Feedback feedback,
                CancellationToken cancellationToken = default
            )
            {
                UnionCallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
