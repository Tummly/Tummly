using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackRecoveryDraftEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackRecoveryDraftEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PrepareDraft_ReturnsBodyAndSubject_WithoutRawContactInProviderInput()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-draft-prepare-tok",
                ContactType.Email,
                "secret-guest@example.com",
                FeedbackWorkflowStatus.InProgress
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeFeedbackRecoveryDraftProvider>();
            fake.SucceedWith(
                "Dear Alex, thank you for telling us.",
                "Regarding your recent visit",
                "email"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-draft"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                mode = "prepare",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Dear Alex, thank you for telling us.",
                body.GetProperty("body").GetString()
            );
            Assert.Equal(
                "Regarding your recent visit",
                body.GetProperty("subject").GetString()
            );
            Assert.Equal("email", body.GetProperty("channel").GetString());

            Assert.NotNull(fake.LastInput);
            var serialized = JsonSerializer.Serialize(fake.LastInput);
            Assert.DoesNotContain("secret-guest@example.com", serialized);
            Assert.Equal("Alex Guest", fake.LastInput!.GuestDisplayName);
            Assert.Equal("Great food", fake.LastInput.FeedbackComment);
            Assert.Equal("negative", fake.LastInput.Sentiment);
            Assert.Equal("Main", fake.LastInput.LocationName);
        }

        [Fact]
        public async Task RewriteDraft_PassesCurrentBodyAndSubject()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-draft-rewrite-tok",
                ContactType.Email,
                "rewrite@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "rewrite-owner@example.com"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeFeedbackRecoveryDraftProvider>();
            fake.SucceedWith(
                "Improved body",
                "Improved subject",
                "email"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-draft"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                mode = "rewrite",
                currentBody = "Prior body",
                currentSubject = "Prior subject",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(fake.LastInput);
            Assert.Equal("rewrite", fake.LastInput!.Mode);
            Assert.Equal("Prior body", fake.LastInput.CurrentBody);
            Assert.Equal("Prior subject", fake.LastInput.CurrentSubject);
        }

        [Fact]
        public async Task PrepareDraft_Failure_ReturnsRetryableFlag()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-draft-fail-tok",
                ContactType.Email,
                "fail@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "fail-owner@example.com"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeFeedbackRecoveryDraftProvider>();
            fake.Fail(retryable: false);

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-draft"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                mode = "prepare",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("retryable").GetBoolean());
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            ContactType contactType,
            string guestContact,
            FeedbackWorkflowStatus workflowStatus,
            string email = "recovery-draft-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Recovery Draft Owner",
                Email = email + "-" + linkToken,
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
                Name = "Recovery Draft Venue",
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
                WorkflowStatus = workflowStatus,
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
