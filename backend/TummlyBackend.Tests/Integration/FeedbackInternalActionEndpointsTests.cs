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
    public class FeedbackInternalActionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackInternalActionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RecordInternalAction_PersistsFact_KeepsInProgress_EmitsActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "internal-action-record-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.InProgress
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/internal-actions"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                category = "team_briefed",
                note = "Briefed the floor team.",
                intent = "record_internal_action_only",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "in_progress",
                postBody.GetProperty("workflowStatus").GetString()
            );

            var activityEvent = postBody.GetProperty("activityEvent");
            Assert.Equal(
                "internal_action_recorded",
                activityEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "team_briefed",
                activityEvent.GetProperty("category").GetString()
            );
            Assert.Equal(
                "Team briefed",
                activityEvent.GetProperty("categoryLabel").GetString()
            );
            Assert.Equal(
                "Briefed the floor team.",
                activityEvent.GetProperty("note").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(
                FeedbackWorkflowStatus.InProgress,
                feedback.WorkflowStatus
            );

            var action = await context.FeedbackInternalActions
                .AsNoTracking()
                .SingleAsync(a => a.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                FeedbackInternalActionCategory.TeamBriefed,
                action.Category
            );
            Assert.Equal("Briefed the floor team.", action.Note);

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getResponse = await _client.SendAsync(get);
            var getBody = await ReadJsonAsync(getResponse);
            var activity = getBody.GetProperty("activityHistory");
            Assert.Equal(2, activity.GetArrayLength());
            Assert.Equal(
                "internal_action_recorded",
                activity[1].GetProperty("kind").GetString()
            );
        }

        [Fact]
        public async Task RecordInternalAction_Returns400_WhenNoteMissing()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "internal-action-no-note-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "nonote-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/internal-actions"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                category = "other_action",
                note = "   ",
                intent = "record_internal_action_only",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.FeedbackInternalActions.AnyAsync(
                    a => a.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task RecordInternalAction_Returns409_WhenAlreadyResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "internal-action-resolved-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.Resolved,
                email: "resolved-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/internal-actions"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                category = "team_briefed",
                note = "Briefed the floor team.",
                intent = "record_internal_action_only",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task RecordInternalAction_Returns403_ForNonOwner()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "internal-action-cross-tenant-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.InProgress,
                email: "cross-tenant-owner@example.com"
            );
            var otherJwt = await SeedOtherOwnerJwtAsync(
                "internal-action-cross-tenant-other-tok"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/internal-actions"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", otherJwt);
            post.Content = JsonContent.Create(new
            {
                category = "team_briefed",
                note = "Briefed the floor team.",
                intent = "record_internal_action_only",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CompleteRecovery_WithInternalAction_Resolves()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "internal-action-complete-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.InProgress,
                email: "complete-internal-owner@example.com"
            );

            using var record = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/internal-actions"
            );
            record.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            record.Content = JsonContent.Create(new
            {
                category = "product_quality_checked",
                note = "Checked the kitchen batch.",
                intent = "record_internal_action_only",
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(record)).StatusCode
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "record_internal_action_only",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.Equal(
                "resolved",
                postBody.GetProperty("workflowStatus").GetString()
            );
            Assert.Equal(
                "recovery_completed",
                postBody.GetProperty("activityEvent").GetProperty("kind").GetString()
            );
            Assert.Equal(
                "record_internal_action_only",
                postBody
                    .GetProperty("activityEvent")
                    .GetProperty("recoveryIntent")
                    .GetString()
            );

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getBody = await ReadJsonAsync(await _client.SendAsync(get));
            var kinds = getBody
                .GetProperty("activityHistory")
                .EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToList();

            Assert.Contains("internal_action_recorded", kinds);
            Assert.Contains("recovery_completed", kinds);
            Assert.DoesNotContain("workflow_status_changed", kinds);
            Assert.DoesNotContain("guest_response_sent", kinds);
        }

        [Fact]
        public async Task CompleteRecovery_RecordIntent_Returns400_WhenNoInternalAction()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "internal-action-complete-missing-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "complete-missing-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "record_internal_action_only",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            string email = "internal-action-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Internal Action Owner",
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
                Name = "Internal Action Venue",
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
                DetectedTagsJson = "[]",
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

        private async Task<string> SeedOtherOwnerJwtAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Other Internal Action Owner",
                Email = $"other-internal-action-owner-{linkToken}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900456",
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
                Name = "Other Internal Action Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }
    }
}
