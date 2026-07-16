using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class FeedbackClassificationRealtimeTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RecordingFeedbackHomeRealtimePublisher _realtime =
            new();
        private readonly FakeFeedbackClassificationProvider _provider = new();
        private readonly FeedbackClassificationProcessor _processor;
        private readonly int _ownerUserId = 42;
        private readonly int _locationId;

        public FeedbackClassificationRealtimeTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var user = new User
            {
                Id = _ownerUserId,
                Email = "op@example.com",
                PasswordHash = "x",
                FullName = "Op",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);

            var restaurant = new Restaurant
            {
                Name = "Venue",
                AccountType = "Single",
                OwnerUserId = _ownerUserId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = "realtime-token-1234567",
                LocationName = "Main",
                Address = "1 High Road",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            _locationId = location.Id;

            _processor = new FeedbackClassificationProcessor(
                _context,
                _provider,
                NullLogger<FeedbackClassificationProcessor>.Instance,
                _realtime
            );
        }

        [Fact]
        public async Task ProcessAsync_PublishesThinSignal_WhenSucceeded()
        {
            _provider.SucceedWith(
                FeedbackSentiment.Negative,
                DetectedIssue.FoodQuality
            );
            var feedbackId = await SeedPendingFeedbackAsync();

            await _processor.ProcessAsync(feedbackId);

            Assert.Single(_realtime.Published);
            var signal = _realtime.Published[0];
            Assert.Equal(_ownerUserId, signal.UserId);
            Assert.Equal(feedbackId, signal.FeedbackId);
            Assert.Equal(_locationId, signal.LocationId);
        }

        [Fact]
        public async Task ProcessAsync_PublishesThinSignal_WhenFailed()
        {
            _provider.Fail();
            var feedbackId = await SeedPendingFeedbackAsync();

            await _processor.ProcessAsync(feedbackId);

            Assert.Single(_realtime.Published);
            var signal = _realtime.Published[0];
            Assert.Equal(_ownerUserId, signal.UserId);
            Assert.Equal(feedbackId, signal.FeedbackId);
            Assert.Equal(_locationId, signal.LocationId);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotPublish_WhenFeedbackMissing()
        {
            await _processor.ProcessAsync(999_999);

            Assert.Empty(_realtime.Published);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotPublish_WhenAlreadyTerminal()
        {
            var feedback = new Feedback
            {
                RestaurantLocationId = _locationId,
                GuestName = "Alex",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Already done",
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Positive,
                DetectedIssuesJson = "[]",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            await _processor.ProcessAsync(feedback.Id);

            Assert.Empty(_realtime.Published);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedPendingFeedbackAsync()
        {
            var feedback = new Feedback
            {
                RestaurantLocationId = _locationId,
                GuestName = "Alex",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Slow service",
                ClassificationStatus = ClassificationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return feedback.Id;
        }

        private sealed class RecordingFeedbackHomeRealtimePublisher
            : IFeedbackHomeRealtimePublisher
        {
            public List<(
                int UserId,
                int FeedbackId,
                int LocationId
            )> Published { get; } = [];

            public Task PublishClassificationTerminalAsync(
                int userId,
                int feedbackId,
                int locationId
            )
            {
                Published.Add((userId, feedbackId, locationId));
                return Task.CompletedTask;
            }
        }
    }
}
