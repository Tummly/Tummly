using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ResendWebhookServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly byte[] _secretBytes;
        private readonly string _signingSecret;
        private readonly DateTime _now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        private readonly ResendWebhookService _service;

        public ResendWebhookServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _secretBytes = new byte[32];
            RandomNumberGenerator.Fill(_secretBytes);
            _signingSecret = "whsec_" + Convert.ToBase64String(_secretBytes);

            _service = new ResendWebhookService(
                _context,
                Options.Create(
                    new EmailSettings { WebhookSigningSecret = _signingSecret }
                ),
                NullLogger<ResendWebhookService>.Instance,
                () => _now
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public void IsSignatureValid_AcceptsMatchingSvixSignature()
        {
            var body = """{"type":"email.opened","data":{"email_id":"abc"}}""";
            var id = "msg_test";
            var timestamp = new DateTimeOffset(_now).ToUnixTimeSeconds().ToString();
            var signature = Sign(id, timestamp, body);

            Assert.True(
                ResendWebhookService.IsSignatureValid(
                    body,
                    id,
                    timestamp,
                    signature,
                    _signingSecret,
                    _now
                )
            );
        }

        [Fact]
        public async Task HandleAsync_EmailOpened_SetsOpenedAtUtcOnce()
        {
            var delivery = await SeedDeliveryAsync(providerMessageId: "email-1");
            var body =
                """{"type":"email.opened","created_at":"2026-10-06T11:55:00.000Z","data":{"email_id":"email-1"}}""";
            var id = "msg_open_1";
            var timestamp = new DateTimeOffset(_now).ToUnixTimeSeconds().ToString();
            var signature = Sign(id, timestamp, body);

            var first = await _service.HandleAsync(
                body,
                id,
                timestamp,
                signature
            );
            Assert.Equal(ResendWebhookHandleStatus.Accepted, first);

            var reloaded = await _context.CampaignRecipientDeliveries.SingleAsync();
            Assert.NotNull(reloaded.OpenedAtUtc);
            var firstOpened = reloaded.OpenedAtUtc;

            var second = await _service.HandleAsync(
                body,
                id,
                timestamp,
                signature
            );
            Assert.Equal(ResendWebhookHandleStatus.Accepted, second);
            Assert.Equal(
                firstOpened,
                (await _context.CampaignRecipientDeliveries.SingleAsync()).OpenedAtUtc
            );
            Assert.Equal(delivery.Id, reloaded.Id);
        }

        [Fact]
        public async Task HandleAsync_EmailOpened_FallsBackToTags()
        {
            var delivery = await SeedDeliveryAsync(providerMessageId: null);
            var body =
                "{\"type\":\"email.opened\",\"created_at\":\"2026-10-06T11:55:00.000Z\","
                + "\"data\":{\"email_id\":\"missing\",\"tags\":{"
                + $"\"campaign_id\":\"{delivery.CampaignId}\","
                + $"\"location_guest_id\":\"{delivery.LocationGuestId}\""
                + "}}}";
            var id = "msg_open_tags";
            var timestamp = new DateTimeOffset(_now).ToUnixTimeSeconds().ToString();
            var signature = Sign(id, timestamp, body);

            var status = await _service.HandleAsync(
                body,
                id,
                timestamp,
                signature
            );
            Assert.Equal(ResendWebhookHandleStatus.Accepted, status);
            Assert.NotNull(
                (await _context.CampaignRecipientDeliveries.SingleAsync()).OpenedAtUtc
            );
        }

        [Fact]
        public async Task HandleAsync_RejectsBadSignature()
        {
            var body = """{"type":"email.opened","data":{"email_id":"x"}}""";
            var status = await _service.HandleAsync(
                body,
                "msg_bad",
                new DateTimeOffset(_now).ToUnixTimeSeconds().ToString(),
                "v1,not-a-real-signature"
            );
            Assert.Equal(ResendWebhookHandleStatus.BadSignature, status);
        }

        private async Task<CampaignRecipientDelivery> SeedDeliveryAsync(
            string? providerMessageId
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Venue",
                AccountType = "Single",
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = "sent",
                Name = "Open test",
                Channel = "email",
                MessageBody = "Hi",
                MessageSubject = "Hi",
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "guest@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                Name = "Guest",
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            var delivery = new CampaignRecipientDelivery
            {
                CampaignId = campaign.Id,
                LocationGuestId = guest.Id,
                Channel = "email",
                Outcome = CampaignFireService.AcceptedOutcome,
                AcceptedAtUtc = _now.AddMinutes(-10),
                ProviderMessageId = providerMessageId,
                UpdatedAtUtc = _now.AddMinutes(-10),
            };
            _context.CampaignRecipientDeliveries.Add(delivery);
            await _context.SaveChangesAsync();
            return delivery;
        }

        private string Sign(string svixId, string timestamp, string body)
        {
            var signedContent = $"{svixId}.{timestamp}.{body}";
            var hash = HMACSHA256.HashData(
                _secretBytes,
                Encoding.UTF8.GetBytes(signedContent)
            );
            return "v1," + Convert.ToBase64String(hash);
        }
    }
}
