using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public sealed class GuestResponseEmailDeliveryServiceTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly ServiceProvider _services;
        private readonly ApplicationDbContext _context;
        private readonly TrackingGuestResponseEmailService _emailService;
        private readonly IGuestResponseEmailDeliveryWork _deliveryWork;
        private readonly FeedbackGuestResponsesService _guestResponses;
        private readonly FeedbackRespondAndRecordService _respondAndRecord;

        public GuestResponseEmailDeliveryServiceTests()
        {
            _emailService = new TrackingGuestResponseEmailService();

            var collection = new ServiceCollection();
            collection.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
            );
            collection.AddSingleton<IEmailService>(_emailService);
            collection.AddSingleton<ISmartGuestLinkService>(
                new StubSmartGuestLinkService()
            );
            collection.AddSingleton<IConfiguration>(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://app.tummly.test",
                        }
                    )
                    .Build()
            );
            collection.AddSingleton<IOptions<GuestResponseEmailDeliverySettings>>(
                Options.Create(
                    new GuestResponseEmailDeliverySettings
                    {
                        RetryBackoffSeconds = 0,
                        ClaimLeaseMinutes = 10,
                        SweepIntervalSeconds = 30,
                    }
                )
            );
            collection.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
            collection.AddSingleton<ILogger<GuestResponseEmailDeliveryWork>>(
                NullLogger<GuestResponseEmailDeliveryWork>.Instance
            );
            collection.AddSingleton<
                IGuestResponseEmailDeliveryWork,
                GuestResponseEmailDeliveryWork
            >();

            _services = collection.BuildServiceProvider();
            _deliveryWork =
                _services.GetRequiredService<IGuestResponseEmailDeliveryWork>();
            _context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(_databaseName)
                    .Options
            );
            _guestResponses = new FeedbackGuestResponsesService(
                _context,
                _deliveryWork
            );
            _respondAndRecord = new FeedbackRespondAndRecordService(
                _context,
                _deliveryWork
            );
        }

        public void Dispose()
        {
            _context.Dispose();
            _services.Dispose();
        }

        [Fact]
        public async Task SendAsync_Email_PersistsFactThenDrainDeliversNoOfferMail()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "guest@example.com"
            );

            var result = await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks for visiting",
                body: "Hi guest, thanks for your feedback.",
                purpose: "thank_the_guest",
                tone: "warm_and_friendly",
                includeNotes: null
            );

            Assert.NotNull(result);
            Assert.Equal(0, _emailService.CallCount);

            var pending = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                pending.EmailDeliveryStatus
            );

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("guest@example.com", _emailService.LastToEmail);
            Assert.Equal("Thanks for visiting", _emailService.LastSubject);
            Assert.Equal("Recovery Venue", _emailService.LastBrandTitle);
            Assert.Equal("Main", _emailService.LastBrandSubtitle);
            Assert.Equal("1 High Street", _emailService.LastLocationAddress);
            Assert.Equal(
                "Hi guest, thanks for your feedback.",
                _emailService.LastMessage
            );

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.Id == pending.Id);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
            Assert.NotNull(accepted.EmailDeliveredAt);
            Assert.Null(accepted.EmailDeliveryClaimedAt);
            Assert.Null(accepted.EmailDeliveryRetryAfter);
        }

        [Fact]
        public async Task SendAsync_Email_ConfirmSucceedsWhileDeliveryPending_RetriesUntilAccepted()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "retry-guest@example.com"
            );
            _emailService.ThrowOnSend = true;

            var result = await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Subject",
                body: "Body text for retry",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            Assert.NotNull(result);
            Assert.Equal(1, await _context.FeedbackGuestResponses.CountAsync());

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            var afterFail = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                afterFail.EmailDeliveryStatus
            );
            Assert.Null(afterFail.EmailDeliveryClaimedAt);
            Assert.True(afterFail.EmailDeliveryAttemptCount >= 1);
            Assert.NotNull(afterFail.EmailDeliveryRetryAfter);

            // Make the row claimable again without waiting on backoff.
            // Clear the send-path tracker so we do not overwrite delivery updates.
            _context.ChangeTracker.Clear();
            var tracked = await _context.FeedbackGuestResponses.SingleAsync();
            tracked.EmailDeliveryRetryAfter = null;
            await _context.SaveChangesAsync();

            _emailService.ThrowOnSend = false;
            await _deliveryWork.DrainAsync();

            Assert.Equal(2, _emailService.CallCount);
            Assert.Equal("retry-guest@example.com", _emailService.LastToEmail);

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
        }

        [Fact]
        public async Task SendAsync_Sms_SavesFactOnly_NoEmailDelivery()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Phone,
                guestContact: "07700900111"
            );

            var result = await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Sms,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: null,
                body: "Thanks for your visit.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            Assert.NotNull(result);

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.NotApplicable,
                row.EmailDeliveryStatus
            );

            await _deliveryWork.DrainAsync();

            Assert.Equal(0, _emailService.CallCount);
        }

        [Fact]
        public async Task SendAndRecordAsync_Email_PersistsThenDrainDelivers()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "record-guest@example.com"
            );

            var result = await _respondAndRecord.SendAndRecordAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackInternalActionCategory.ProductQualityChecked,
                note: "Spoke with kitchen lead",
                subject: "We are on it",
                body: "Thanks — we are fixing this.",
                purpose: "apologise_and_explain",
                tone: "warm_and_apologetic",
                includeNotes: null
            );

            Assert.NotNull(result);
            Assert.Equal(0, _emailService.CallCount);

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("record-guest@example.com", _emailService.LastToEmail);
            Assert.Equal("We are on it", _emailService.LastSubject);

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
            Assert.Equal(1, await _context.FeedbackInternalActions.CountAsync());
        }

        private async Task<(int FeedbackId, int AuthorUserId)> SeedAsync(
            ContactType contactType,
            string guestContact
        )
        {
            var user = new User
            {
                FullName = "Delivery Operator",
                Email = $"op-{Guid.NewGuid()}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900999",
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
                Name = "Recovery Venue",
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

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[\"FoodQuality\"]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (feedback.Id, user.Id);
        }

        private sealed class TrackingGuestResponseEmailService : EmailServiceStubBase
        {
            public int CallCount { get; private set; }

            public string? LastToEmail { get; private set; }

            public string? LastSubject { get; private set; }

            public string? LastBrandTitle { get; private set; }

            public string? LastBrandSubtitle { get; private set; }

            public string? LastLocationAddress { get; private set; }

            public string? LastMessage { get; private set; }

            public bool ThrowOnSend { get; set; }

            public override Task SendGuestResponseEmailAsync(
                string toEmail,
                string subject,
                string brandTitle,
                string? brandSubtitle,
                string? locationAddress,
                string message,
                string giveFeedbackUrl,
                string? brandLogoUrl = null
            )
            {
                CallCount++;
                LastToEmail = toEmail;
                LastSubject = subject;
                LastBrandTitle = brandTitle;
                LastBrandSubtitle = brandSubtitle;
                LastLocationAddress = locationAddress;
                LastMessage = message;

                if (ThrowOnSend)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }

        private sealed class StubSmartGuestLinkService : ISmartGuestLinkService
        {
            public Task<string> GenerateTokenAsync() =>
                Task.FromResult("stub-token");

            public Task<DTOs.SmartGuestLink.GuestLinkLocationInfo?> ResolveForGuestAsync(
                string token
            ) => Task.FromResult<DTOs.SmartGuestLink.GuestLinkLocationInfo?>(null);

            public Task<DTOs.SmartGuestLink.QrLinkWriteResolution?> ResolveLocationForWriteAsync(
                string token
            ) => Task.FromResult<DTOs.SmartGuestLink.QrLinkWriteResolution?>(null);

            public string BuildGuestUrl(string token) =>
                $"https://app.tummly.test/scan/{token}";

            public Task<string?> GetActiveSmartGuestTokenAsync(
                int restaurantLocationId
            ) => Task.FromResult<string?>(null);
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
