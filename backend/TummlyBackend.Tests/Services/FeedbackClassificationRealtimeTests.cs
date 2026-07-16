using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class FeedbackClassificationRealtimeTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly ServiceProvider _services;
        private readonly ApplicationDbContext _context;
        private readonly RecordingFeedbackHomeRealtimePublisher _realtime =
            new();
        private readonly FakeFeedbackClassificationProvider _provider = new();
        private readonly IFeedbackClassificationWork _work;
        private readonly int _ownerUserId = 42;
        private readonly int _locationId;

        public FeedbackClassificationRealtimeTests()
        {
            var collection = new ServiceCollection();
            collection.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
            );
            collection.AddSingleton<IFeedbackClassificationProvider>(_provider);
            collection.AddSingleton<IFeedbackHomeRealtimePublisher>(_realtime);
            collection.AddSingleton<IOptions<FeedbackClassificationSettings>>(
                Options.Create(new FeedbackClassificationSettings())
            );
            collection.AddSingleton<IHostEnvironment>(
                new TestHostEnvironment()
            );
            collection.AddSingleton<ILogger<FeedbackClassificationWork>>(
                NullLogger<FeedbackClassificationWork>.Instance
            );
            collection.AddSingleton<
                IFeedbackClassificationWork,
                FeedbackClassificationWork
            >();

            _services = collection.BuildServiceProvider();
            _work = _services.GetRequiredService<IFeedbackClassificationWork>();
            _context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(_databaseName)
                    .Options
            );

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
        }

        [Fact]
        public async Task DrainAsync_PublishesThinSignal_WhenSucceeded()
        {
            _provider.SucceedWith(
                FeedbackSentiment.Negative,
                DetectedIssue.FoodQuality
            );
            var feedbackId = await SeedPendingFeedbackAsync();

            await _work.DrainAsync();

            Assert.Single(_realtime.Published);
            var signal = _realtime.Published[0];
            Assert.Equal(_ownerUserId, signal.UserId);
            Assert.Equal(feedbackId, signal.FeedbackId);
            Assert.Equal(_locationId, signal.LocationId);
        }

        [Fact]
        public async Task DrainAsync_PublishesThinSignal_WhenFailed()
        {
            _provider.Fail();
            var feedbackId = await SeedPendingFeedbackAsync();

            await _work.DrainAsync();

            Assert.Single(_realtime.Published);
            var signal = _realtime.Published[0];
            Assert.Equal(_ownerUserId, signal.UserId);
            Assert.Equal(feedbackId, signal.FeedbackId);
            Assert.Equal(_locationId, signal.LocationId);
        }

        [Fact]
        public async Task DrainAsync_DoesNotPublish_WhenNoPendingWork()
        {
            await _work.DrainAsync();

            Assert.Empty(_realtime.Published);
        }

        [Fact]
        public async Task DrainAsync_DoesNotPublish_WhenAlreadyTerminal()
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

            await _work.DrainAsync();

            Assert.Empty(_realtime.Published);
        }

        [Fact]
        public async Task DrainAsync_RecoversPending_WithoutWakeNotify()
        {
            _provider.SucceedWith(FeedbackSentiment.Positive);
            var feedbackId = await SeedPendingFeedbackAsync();

            await _work.DrainAsync();

            var row = await _context.Feedbacks.AsNoTracking()
                .FirstAsync(f => f.Id == feedbackId);
            Assert.Equal(
                ClassificationStatus.Succeeded,
                row.ClassificationStatus
            );
            Assert.Single(_realtime.Published);
        }

        [Fact]
        public async Task DrainAsync_MarksFailed_WhenClaimAttemptsExhausted()
        {
            var feedbackId = await SeedPendingFeedbackAsync(claimAttempts: 3);

            await _work.DrainAsync();

            var row = await _context.Feedbacks.AsNoTracking()
                .FirstAsync(f => f.Id == feedbackId);
            Assert.Equal(ClassificationStatus.Failed, row.ClassificationStatus);
            Assert.Null(row.Sentiment);
            Assert.Single(_realtime.Published);
        }

        [Fact]
        public async Task DrainAsync_MarksFailed_WhenProviderThrows()
        {
            _provider.ThrowOnClassify();
            var feedbackId = await SeedPendingFeedbackAsync();

            await _work.DrainAsync();

            var row = await _context.Feedbacks.AsNoTracking()
                .FirstAsync(f => f.Id == feedbackId);
            Assert.Equal(ClassificationStatus.Failed, row.ClassificationStatus);
            Assert.Null(row.Sentiment);
            Assert.Null(row.ClassificationClaimedAt);
            Assert.Single(_realtime.Published);
        }

        [Fact]
        public async Task DrainAsync_MarksFailed_WhenPostProviderMappingThrows()
        {
            _provider.SucceedWithNullIssues();
            var feedbackId = await SeedPendingFeedbackAsync();

            await _work.DrainAsync();

            var row = await _context.Feedbacks.AsNoTracking()
                .FirstAsync(f => f.Id == feedbackId);
            Assert.Equal(ClassificationStatus.Failed, row.ClassificationStatus);
            Assert.Null(row.Sentiment);
            Assert.Null(row.ClassificationClaimedAt);
            Assert.Single(_realtime.Published);
        }

        [Fact]
        public async Task DrainAsync_KeepsSucceeded_WhenPublishThrows()
        {
            _provider.SucceedWith(FeedbackSentiment.Positive);
            _realtime.ThrowOnPublish = true;
            var feedbackId = await SeedPendingFeedbackAsync();

            await _work.DrainAsync();

            var row = await _context.Feedbacks.AsNoTracking()
                .FirstAsync(f => f.Id == feedbackId);
            Assert.Equal(
                ClassificationStatus.Succeeded,
                row.ClassificationStatus
            );
            Assert.Equal(FeedbackSentiment.Positive, row.Sentiment);
            Assert.Null(row.ClassificationClaimedAt);
            Assert.Empty(_realtime.Published);
        }

        public void Dispose()
        {
            _context.Dispose();
            _services.Dispose();
        }

        private async Task<int> SeedPendingFeedbackAsync(
            int claimAttempts = 0
        )
        {
            var feedback = new Feedback
            {
                RestaurantLocationId = _locationId,
                GuestName = "Alex",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Slow service",
                ClassificationStatus = ClassificationStatus.Pending,
                ClassificationClaimAttempts = claimAttempts,
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

            public bool ThrowOnPublish { get; set; }

            public Task PublishClassificationTerminalAsync(
                int userId,
                int feedbackId,
                int locationId
            )
            {
                if (ThrowOnPublish)
                {
                    throw new InvalidOperationException("SignalR publish boom");
                }

                Published.Add((userId, feedbackId, locationId));
                return Task.CompletedTask;
            }
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Testing";

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = ".";

            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();
        }
    }
}
