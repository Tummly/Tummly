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
    public class FeedbackWorkflowStatusEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackWorkflowStatusEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetFeedbackDetails_ExposesInitialNewAndNeedsAttention()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-details-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
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
                "new",
                body.GetProperty("workflowStatus").GetString()
            );
            Assert.True(body.GetProperty("needsAttention").GetBoolean());
        }

        [Fact]
        public async Task GetFeedbackDetails_NeedsAttentionFalse_WhenResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-resolved-needs-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                workflowStatus: FeedbackWorkflowStatus.Resolved
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "resolved",
                body.GetProperty("workflowStatus").GetString()
            );
            Assert.False(body.GetProperty("needsAttention").GetBoolean());
        }

        [Fact]
        public async Task GetFeedbackDetails_DoesNotAdvanceStatus()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-open-noop-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            for (var i = 0; i < 2; i++)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/api/feedback/{seeded.FeedbackId}"
                );
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);

                var response = await _client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(FeedbackWorkflowStatus.New, feedback.WorkflowStatus);
        }

        [Fact]
        public async Task SetWorkflowStatus_TransitionsNewToInProgress_AndRecordsActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-transition-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            using var put = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/workflow-status"
            );
            put.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            put.Content = JsonContent.Create(new
            {
                workflowStatus = "in_progress"
            });

            var putResponse = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var putBody = await ReadJsonAsync(putResponse);
            Assert.True(putBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "in_progress",
                putBody.GetProperty("workflowStatus").GetString()
            );
            Assert.True(putBody.GetProperty("needsAttention").GetBoolean());

            var activityEvent = putBody.GetProperty("activityEvent");
            Assert.Equal(
                "workflow_status_changed",
                activityEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Workflow Owner",
                activityEvent.GetProperty("actorDisplayName").GetString()
            );
            Assert.Equal(
                "new",
                activityEvent.GetProperty("fromWorkflowStatus").GetString()
            );
            Assert.Equal(
                "in_progress",
                activityEvent.GetProperty("toWorkflowStatus").GetString()
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
            Assert.Equal(
                "in_progress",
                getBody.GetProperty("workflowStatus").GetString()
            );
            Assert.True(getBody.GetProperty("needsAttention").GetBoolean());

            var activity = getBody.GetProperty("activityHistory");
            Assert.Equal(2, activity.GetArrayLength());
            Assert.Equal(
                "feedback_received",
                activity[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "workflow_status_changed",
                activity[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "new",
                activity[1].GetProperty("fromWorkflowStatus").GetString()
            );
            Assert.Equal(
                "in_progress",
                activity[1].GetProperty("toWorkflowStatus").GetString()
            );
        }

        [Fact]
        public async Task SetWorkflowStatus_Returns400_ForResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-reject-resolved-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/workflow-status"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                workflowStatus = "resolved"
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(FeedbackWorkflowStatus.New, feedback.WorkflowStatus);
            Assert.Empty(
                await context.FeedbackWorkflowStatusChanges
                    .AsNoTracking()
                    .Where(c => c.FeedbackId == seeded.FeedbackId)
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task SetWorkflowStatus_SameToSame_IsNoOpWithoutActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-same-noop-tok",
                ClassificationStatus.Pending,
                sentiment: null,
                workflowStatus: FeedbackWorkflowStatus.InProgress
            );

            using var put = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/workflow-status"
            );
            put.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            put.Content = JsonContent.Create(new
            {
                workflowStatus = "in_progress"
            });

            var putResponse = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var putBody = await ReadJsonAsync(putResponse);
            Assert.Equal(
                "in_progress",
                putBody.GetProperty("workflowStatus").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                putBody.GetProperty("activityEvent").ValueKind
            );

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getResponse = await _client.SendAsync(get);
            var getBody = await ReadJsonAsync(getResponse);
            var activity = getBody.GetProperty("activityHistory");
            Assert.Equal(1, activity.GetArrayLength());
            Assert.Equal(
                "feedback_received",
                activity[0].GetProperty("kind").GetString()
            );
        }

        [Fact]
        public async Task SetWorkflowStatus_Returns400_ForInvalidStatus()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-bad-tok",
                ClassificationStatus.Pending,
                sentiment: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/workflow-status"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                workflowStatus = "needs_attention"
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SetWorkflowStatus_Returns401_WhenUnauthenticated()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "workflow-status-unauth-tok",
                ClassificationStatus.Pending,
                sentiment: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/workflow-status"
            );
            request.Content = JsonContent.Create(new
            {
                workflowStatus = "in_progress"
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SetWorkflowStatus_Returns403_ForNonOwnedFeedback()
        {
            var owner = await SeedOwnerWithFeedbackAsync(
                "workflow-status-owner-a-tok",
                ClassificationStatus.Pending,
                sentiment: null
            );
            var other = await SeedOwnerWithFeedbackAsync(
                "workflow-status-owner-b-tok",
                ClassificationStatus.Pending,
                sentiment: null,
                email: "other-workflow-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{other.FeedbackId}/workflow-status"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                workflowStatus = "in_progress"
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
            FeedbackWorkflowStatus workflowStatus = FeedbackWorkflowStatus.New,
            string email = "workflow-status-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Workflow Owner",
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
                Name = "Workflow Venue",
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
