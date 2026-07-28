using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackClassificationCorrectionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackClassificationCorrectionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CorrectClassification_OverwritesSentiment_WhenSucceeded()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "correct-classification-ok-token-12345",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/classification"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                sentiment = "positive"
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Succeeded",
                body.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                "positive",
                body.GetProperty("sentiment").GetString()
            );
            Assert.Equal(
                JsonValueKind.Array,
                body.GetProperty("detectedTags").ValueKind
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(FeedbackSentiment.Positive, feedback.Sentiment);
            Assert.Equal(
                ClassificationStatus.Succeeded,
                feedback.ClassificationStatus
            );
        }

        [Fact]
        public async Task CorrectClassification_RecordsActivityWithActorTimeAndAction()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "correct-classification-activity-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            using var put = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/classification"
            );
            put.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            put.Content = JsonContent.Create(new { sentiment = "positive" });

            var putResponse = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var putBody = await ReadJsonAsync(putResponse);
            var activityEvent = putBody.GetProperty("activityEvent");
            Assert.Equal(
                "classification_corrected",
                activityEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Correct Owner",
                activityEvent.GetProperty("actorDisplayName").GetString()
            );
            Assert.Equal(
                "negative",
                activityEvent.GetProperty("fromSentiment").GetString()
            );
            Assert.Equal(
                "positive",
                activityEvent.GetProperty("toSentiment").GetString()
            );
            Assert.True(activityEvent.TryGetProperty("at", out _));

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getResponse = await _client.SendAsync(get);
            var getBody = await ReadJsonAsync(getResponse);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var activity = getBody.GetProperty("activityHistory");
            Assert.Equal(2, activity.GetArrayLength());
            Assert.Equal(
                "feedback_received",
                activity[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "classification_corrected",
                activity[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Correct Owner",
                activity[1].GetProperty("actorDisplayName").GetString()
            );
            Assert.Equal(
                "negative",
                activity[1].GetProperty("fromSentiment").GetString()
            );
            Assert.Equal(
                "positive",
                activity[1].GetProperty("toSentiment").GetString()
            );
        }

        [Fact]
        public async Task CorrectClassification_Returns409_WhenPending()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "correct-classification-pending-tok",
                ClassificationStatus.Pending,
                sentiment: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/classification"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                sentiment = "neutral"
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CorrectClassification_Returns409_WhenFailed()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "correct-classification-failed-tok",
                ClassificationStatus.Failed,
                sentiment: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/classification"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                sentiment = "positive"
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CorrectClassification_Returns400_ForInvalidSentiment()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "correct-classification-bad-sent-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/classification"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                sentiment = "mixed"
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CorrectClassification_Returns403_ForNonOwnedFeedback()
        {
            var owner = await SeedOwnerWithFeedbackAsync(
                "correct-classification-owner-a-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );
            var other = await SeedOwnerWithFeedbackAsync(
                "correct-classification-owner-b-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                email: "other-correct-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{other.FeedbackId}/classification"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                sentiment = "neutral"
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            ClassificationStatus classificationStatus,
            FeedbackSentiment? sentiment,
            string email = "correct-classification-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Correct Owner",
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
                Name = "Correct Venue",
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
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = classificationStatus,
                Sentiment = sentiment,
                DetectedTagsJson =
                    classificationStatus == ClassificationStatus.Succeeded
                        ? "[]"
                        : null,
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
