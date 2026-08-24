using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CampaignSendTestServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingGuestResponseEmailService _emailService;
        private readonly CampaignSendTestService _service;

        public CampaignSendTestServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingGuestResponseEmailService();

            _service = new CampaignSendTestService(
                _context,
                _emailService
            );
        }

        [Fact]
        public async Task SendAsync_SendsToNominatedEmail_CreatesNoCampaignFact()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Campaign Venue",
                locationName: "Main",
                address: "1 High Street"
            );
            var campaignCountBefore = await _context.Campaigns.CountAsync();

            var result = await _service.SendAsync(
                locationId,
                toEmail: "team@example.com",
                subject: "Thanks for visiting",
                body: "Hi guest, thanks for joining us."
            );

            Assert.True(result);
            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("team@example.com", _emailService.LastToEmail);
            Assert.Equal("Thanks for visiting", _emailService.LastSubject);
            Assert.Equal("Campaign Venue", _emailService.LastBrandTitle);
            Assert.Equal("Main", _emailService.LastBrandSubtitle);
            Assert.Equal("1 High Street", _emailService.LastLocationAddress);
            Assert.Equal(
                "Hi guest, thanks for joining us.",
                _emailService.LastMessage
            );
            Assert.Null(_emailService.LastOffer);
            Assert.Equal(
                campaignCountBefore,
                await _context.Campaigns.CountAsync()
            );
        }

        [Fact]
        public async Task SendAsync_WithOfferDraft_SendsSampleCode_CreatesNoCampaign()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Offer Venue",
                locationName: "Kitchen",
                address: "2 High Street"
            );
            var campaignCountBefore = await _context.Campaigns.CountAsync();

            var result = await _service.SendAsync(
                locationId,
                toEmail: "operator@example.com",
                subject: "Come back soon",
                body: "Here is an offer for you.",
                offer: new CampaignSendTestOfferDto
                {
                    Title = "15% off your next visit",
                    Description = "Show this code to the team.",
                    ExpiryLabel = "Expires: 31 July 2026",
                }
            );

            Assert.True(result);
            Assert.Equal(1, _emailService.CallCount);
            Assert.NotNull(_emailService.LastOffer);
            Assert.Equal(
                "15% off your next visit",
                _emailService.LastOffer!.Title
            );
            Assert.Equal(
                CampaignSendTestService.SampleRedemptionCode,
                _emailService.LastOffer.RedemptionCode
            );
            Assert.Equal(
                campaignCountBefore,
                await _context.Campaigns.CountAsync()
            );
        }

        [Fact]
        public async Task SendAsync_RejectsInvalidEmail()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Validation Venue",
                locationName: "Main",
                address: "3 High Street"
            );

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SendAsync(
                    locationId,
                    toEmail: "not-an-email",
                    subject: "Subject",
                    body: "Body text"
                )
            );

            Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _emailService.CallCount);
        }

        [Fact]
        public async Task SendAsync_RejectsEmptyBody()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Body Venue",
                locationName: "Main",
                address: "4 High Street"
            );

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SendAsync(
                    locationId,
                    toEmail: "ok@example.com",
                    subject: "Subject",
                    body: "   "
                )
            );

            Assert.Equal(0, _emailService.CallCount);
        }

        [Fact]
        public async Task SendAsync_ReturnsNull_WhenLocationMissing()
        {
            var result = await _service.SendAsync(
                locationId: 999_999,
                toEmail: "ok@example.com",
                subject: "Subject",
                body: "Body text"
            );

            Assert.Null(result);
            Assert.Equal(0, _emailService.CallCount);
        }

        [Fact]
        public async Task SendAsync_PropagatesEmailFailure_Synchronously()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Fail Venue",
                locationName: "Main",
                address: "5 High Street"
            );
            _emailService.ThrowOnSend = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendAsync(
                    locationId,
                    toEmail: "ok@example.com",
                    subject: "Subject",
                    body: "Body text"
                )
            );
        }

        [Fact]
        public async Task SendAsync_UsesDefaultCampaignSenderName_WhenSet()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Workspace Name",
                locationName: "Main",
                address: "1 High Street",
                defaultCampaignSenderName: "Harbour Kitchen"
            );

            var result = await _service.SendAsync(
                locationId,
                toEmail: "team@example.com",
                subject: "Thanks",
                body: "Hello"
            );

            Assert.True(result);
            Assert.Equal("Harbour Kitchen", _emailService.LastBrandTitle);
            Assert.Equal("Main", _emailService.LastBrandSubtitle);
        }

        [Fact]
        public async Task SendAsync_FallsBackToWorkspaceName_WhenSenderUnset()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "Workspace Name",
                locationName: "Main",
                address: "1 High Street"
            );

            var result = await _service.SendAsync(
                locationId,
                toEmail: "team@example.com",
                subject: "Thanks",
                body: "Hello"
            );

            Assert.True(result);
            Assert.Equal("Workspace Name", _emailService.LastBrandTitle);
            Assert.Equal("Main", _emailService.LastBrandSubtitle);
        }

        [Fact]
        public async Task SendAsync_FallsBackToLocationName_WhenWorkspaceNameEmpty()
        {
            var locationId = await SeedLocationAsync(
                restaurantName: "  ",
                locationName: "Main Street",
                address: "1 High Street"
            );

            var result = await _service.SendAsync(
                locationId,
                toEmail: "team@example.com",
                subject: "Thanks",
                body: "Hello"
            );

            Assert.True(result);
            Assert.Equal("Main Street", _emailService.LastBrandTitle);
            Assert.Null(_emailService.LastBrandSubtitle);
        }

        public void Dispose() => _context.Dispose();

        private async Task<int> SeedLocationAsync(
            string restaurantName,
            string locationName,
            string address,
            string? defaultCampaignSenderName = null
        )
        {
            var user = new User
            {
                FullName = "Campaign Operator",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
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
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = user.Id,
                DefaultCampaignSenderName = defaultCampaignSenderName,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = address,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            return location.Id;
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

            public GuestResponseEmailOfferBlock? LastOffer { get; private set; }

            public bool ThrowOnSend { get; set; }

            public override Task SendGuestResponseEmailAsync(
                string toEmail,
                string subject,
                string brandTitle,
                string? brandSubtitle,
                string? locationAddress,
                string message,
                string? brandLogoUrl = null,
                GuestResponseEmailOfferBlock? offer = null
            )
            {
                CallCount++;
                LastToEmail = toEmail;
                LastSubject = subject;
                LastBrandTitle = brandTitle;
                LastBrandSubtitle = brandSubtitle;
                LastLocationAddress = locationAddress;
                LastMessage = message;
                LastOffer = offer;

                if (ThrowOnSend)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }
    }
}
