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
    public class FeedbackRespondAndRecordEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackRespondAndRecordEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendAndRecord_DualWrites_KeepsInProgress_EmitsBothActivities()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Regarding your visit",
                body = "Thank you — we briefed the team.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "in_progress",
                postBody.GetProperty("workflowStatus").GetString()
            );

            var guestEvent = postBody.GetProperty("guestResponseActivityEvent");
            Assert.Equal(
                "guest_response_sent",
                guestEvent.GetProperty("kind").GetString()
            );

            var internalEvent =
                postBody.GetProperty("internalActionActivityEvent");
            Assert.Equal(
                "internal_action_recorded",
                internalEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "team_briefed",
                internalEvent.GetProperty("category").GetString()
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

            Assert.Equal(
                1,
                await context.FeedbackGuestResponses.CountAsync(
                    r => r.FeedbackId == seeded.FeedbackId
                )
            );
            Assert.Equal(
                1,
                await context.FeedbackInternalActions.CountAsync(
                    a => a.FeedbackId == seeded.FeedbackId
                )
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
            var kinds = activity.EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToList();
            Assert.Contains("guest_response_sent", kinds);
            Assert.Contains("internal_action_recorded", kinds);
            Assert.DoesNotContain("recovery_completed", kinds);
        }

        [Fact]
        public async Task CompleteRecovery_RequiresBothFacts_ThenResolves()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-complete-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-complete@example.com"
            );

            using var completeBefore = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            completeBefore.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            completeBefore.Content = JsonContent.Create(new
            {
                intent = "respond_and_record_internal_action",
            });

            var beforeResponse = await _client.SendAsync(completeBefore);
            Assert.Equal(HttpStatusCode.BadRequest, beforeResponse.StatusCode);

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry",
                body = "We followed up internally.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(post)).StatusCode
            );

            using var complete = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            complete.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            complete.Content = JsonContent.Create(new
            {
                intent = "respond_and_record_internal_action",
            });

            var completeResponse = await _client.SendAsync(complete);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
            var completeBody = await ReadJsonAsync(completeResponse);
            Assert.Equal(
                "resolved",
                completeBody.GetProperty("workflowStatus").GetString()
            );
            Assert.Equal(
                "respond_and_record_internal_action",
                completeBody
                    .GetProperty("activityEvent")
                    .GetProperty("recoveryIntent")
                    .GetString()
            );
        }

        [Fact]
        public async Task PrepareDraft_AcceptsConfirmedInternalActionContext()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-draft-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-draft@example.com"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<TummlyBackend.Services.FakeFeedbackRecoveryDraftProvider>();

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
                confirmedInternalActionCategory = "team_briefed",
                confirmedInternalActionNote = "Briefed the floor team.",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(fake.LastInput);
            Assert.Equal(
                "team_briefed",
                fake.LastInput!.ConfirmedInternalActionCategory
            );
            Assert.Equal(
                "Briefed the floor team.",
                fake.LastInput.ConfirmedInternalActionNote
            );
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
            string email = "respond-and-record-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Respond And Record Owner",
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
                Name = "Respond And Record Venue",
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
                Comment = "Cold food",
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

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }
    }
}
