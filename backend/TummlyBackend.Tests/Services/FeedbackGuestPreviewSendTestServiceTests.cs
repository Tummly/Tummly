using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class FeedbackGuestPreviewSendTestServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingGuestResponseEmailService _emailService;
        private readonly FeedbackGuestPreviewSendTestService _service;

        public FeedbackGuestPreviewSendTestServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingGuestResponseEmailService();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
                    }
                )
                .Build();

            _service = new FeedbackGuestPreviewSendTestService(
                _context,
                _emailService,
                new StubSmartGuestLinkService(),
                configuration
            );
        }

        [Fact]
        public async Task SendAsync_SendsToOperatorEmail_NotGuest_AndCreatesNoFact()
        {
            var seeded = await SeedAsync(
                operatorEmail: "operator@example.com",
                guestContact: "guest@example.com"
            );

            var result = await _service.SendAsync(
                seeded.FeedbackId,
                seeded.OperatorUserId,
                subject: "Thanks for visiting",
                body: "Hi guest, thanks for your feedback."
            );

            Assert.True(result);
            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("operator@example.com", _emailService.LastToEmail);
            Assert.Equal("Thanks for visiting", _emailService.LastSubject);
            Assert.Equal("Recovery Venue", _emailService.LastBrandTitle);
            Assert.Equal("Main", _emailService.LastBrandSubtitle);
            Assert.Equal("1 High Street", _emailService.LastLocationAddress);
            Assert.Equal(
                "Hi guest, thanks for your feedback.",
                _emailService.LastMessage
            );
            Assert.DoesNotContain(
                "guest@example.com",
                _emailService.LastToEmail
                    + _emailService.LastSubject
                    + _emailService.LastMessage
            );
            Assert.Equal(0, await _context.FeedbackGuestResponses.CountAsync());
        }

        [Fact]
        public async Task SendAsync_PropagatesEmailFailure_Synchronously()
        {
            var seeded = await SeedAsync(
                operatorEmail: "operator-fail@example.com",
                guestContact: "guest-fail@example.com"
            );
            _emailService.ThrowOnSend = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendAsync(
                    seeded.FeedbackId,
                    seeded.OperatorUserId,
                    subject: "Subject",
                    body: "Body text"
                )
            );

            Assert.Equal(0, await _context.FeedbackGuestResponses.CountAsync());
        }

        [Fact]
        public async Task SendAsync_ReturnsNull_WhenFeedbackMissing()
        {
            var user = new User
            {
                FullName = "Orphan Operator",
                Email = "orphan@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.SendAsync(
                feedbackId: 999_999,
                operatorUserId: user.Id,
                subject: "Subject",
                body: "Body text"
            );

            Assert.Null(result);
            Assert.Equal(0, _emailService.CallCount);
        }

        public void Dispose() => _context.Dispose();

        private async Task<(int FeedbackId, int OperatorUserId)> SeedAsync(
            string operatorEmail,
            string guestContact
        )
        {
            var user = new User
            {
                FullName = "Preview Operator",
                Email = operatorEmail,
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
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
                ContactType = ContactType.Email,
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
    }
}
