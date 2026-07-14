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
                "1 High Street",
                body.GetProperty("address").GetString()
            );
            Assert.True(body.TryGetProperty("createdAt", out _));
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
            int FeedbackId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            string email = "feedback-details-owner@example.com"
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
                LinkToken = linkToken,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
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

            return (jwt, location.Id, feedback.Id);
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
