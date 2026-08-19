using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackDetailsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackDetailsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetFeedbackDetails_ReturnsOwnedFeedbackFields()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "feedback-details-owner-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                seeded.FeedbackId,
                body.GetProperty("id").GetInt32()
            );
            Assert.Equal(
                "Alex Guest",
                body.GetProperty("guestName").GetString()
            );
            Assert.Equal(
                "alex@example.com",
                body.GetProperty("guestContact").GetString()
            );
            Assert.Equal(
                "Email",
                body.GetProperty("contactType").GetString()
            );
            Assert.Equal(
                "Great food",
                body.GetProperty("comment").GetString()
            );
            Assert.Equal(
                "Main",
                body.GetProperty("locationName").GetString()
            );
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );
            Assert.Equal(
                "1 High Street",
                body.GetProperty("address").GetString()
            );
            Assert.True(body.TryGetProperty("createdAt", out _));
            Assert.Equal(
                "Pending",
                body.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("sentiment").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("detectedTags").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("locationGuestId").ValueKind
            );
            Assert.Equal(
                "not_recorded",
                body.GetProperty("marketingPreference").GetString()
            );
            Assert.Equal(
                "Smart Guest",
                body.GetProperty("qrSource").GetString()
            );
        }

        [Fact]
        public async Task GetFeedbackDetails_ReturnsQrTypeLabel_AsQrSource()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "feedback-details-qr-counter-token-12",
                qrType: QrType.CounterCard
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "Counter card",
                body.GetProperty("qrSource").GetString()
            );
        }

        [Fact]
        public async Task GetFeedbackDetails_ReturnsDigitalGuestLinkName_AsQrSource()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "feedback-details-qr-digital-token-1",
                qrType: QrType.DigitalGuestLink,
                linkName: "WhatsApp list"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "WhatsApp list",
                body.GetProperty("qrSource").GetString()
            );
        }

        [Fact]
        public async Task GetFeedbackDetails_ReturnsLocationGuestId_WhenLinked()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "feedback-details-linked-guest-token-12",
                linkLocationGuest: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                seeded.LocationGuestId,
                body.GetProperty("locationGuestId").GetInt32()
            );
            Assert.Equal(
                "allowed",
                body.GetProperty("marketingPreference").GetString()
            );
        }

        [Fact]
        public async Task GetFeedbackDetails_Returns403_ForNonOwnedFeedback()
        {
            var owner = await SeedOwnerWithFeedbackAsync(
                "feedback-details-owner-a-token-1234"
            );
            var other = await SeedOwnerWithFeedbackAsync(
                "feedback-details-owner-b-token-1234",
                email: "other-feedback-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{other.FeedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackDetails_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/feedback/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackDetails_Returns404_ForUnknownId()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "feedback-details-missing-token-123"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/feedback/999999"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId,
            int? LocationGuestId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            string email = "feedback-details-owner@example.com",
            bool linkLocationGuest = false,
            QrType? qrType = null,
            string? linkName = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Feedback Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Feedback Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            int? locationGuestId = null;
            if (linkLocationGuest)
            {
                var masterGuest = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = "alex@example.com",
                    NormalizedEmail = "alex@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(masterGuest);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = masterGuest.Id,
                    RestaurantLocationId = location.Id,
                    Name = "Alex Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();
                locationGuestId = locationGuest.Id;
            }

            var resolvedQrType = qrType ?? QrType.SmartGuest;
            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = resolvedQrType,
                Token = linkToken,
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            if (resolvedQrType == QrType.DigitalGuestLink)
            {
                qrCode.LinkName = linkName ?? "Digital link";
                qrCode.NormalizedLinkName =
                    (linkName ?? "Digital link").ToLowerInvariant();
                qrCode.Channel = DigitalGuestLinkChannel.WhatsApp;
            }

            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuestId,
                QrCodeId = qrCode.Id,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, feedback.Id, locationGuestId);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
