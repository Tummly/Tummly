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
    public class FeedbackCloseOutEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackCloseOutEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CloseOut_MarkResolved_PersistsFactAndResolves()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "close-out-mark-resolved-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/close-out"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "mark_resolved",
                reason = "duplicate_submission",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "resolved",
                postBody.GetProperty("workflowStatus").GetString()
            );
            Assert.False(postBody.GetProperty("needsAttention").GetBoolean());

            var closeOutEvent = postBody.GetProperty("activityEvent");
            Assert.Equal(
                "feedback_closed_out",
                closeOutEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "mark_resolved",
                closeOutEvent.GetProperty("closeOutIntent").GetString()
            );
            Assert.Equal(
                "duplicate_submission",
                closeOutEvent.GetProperty("closeOutReason").GetString()
            );
            Assert.Equal(
                "new",
                closeOutEvent.GetProperty("fromWorkflowStatus").GetString()
            );
            Assert.Equal(
                "resolved",
                closeOutEvent.GetProperty("toWorkflowStatus").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                postBody.GetProperty("noteActivityEvent").ValueKind
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(FeedbackWorkflowStatus.Resolved, feedback.WorkflowStatus);

            var statusChange = await context.FeedbackWorkflowStatusChanges
                .AsNoTracking()
                .SingleAsync(c => c.FeedbackId == seeded.FeedbackId);
            Assert.Equal(FeedbackWorkflowStatus.New, statusChange.FromStatus);
            Assert.Equal(FeedbackWorkflowStatus.Resolved, statusChange.ToStatus);

            var closeOut = await context.FeedbackCloseOuts
                .AsNoTracking()
                .SingleAsync(c => c.FeedbackId == seeded.FeedbackId);
            Assert.Equal(FeedbackCloseOutIntent.MarkResolved, closeOut.Intent);
            Assert.Equal(
                FeedbackCloseOutReason.DuplicateSubmission,
                closeOut.Reason
            );
            Assert.Equal(statusChange.Id, closeOut.WorkflowStatusChangeId);
            Assert.Null(closeOut.InternalNoteId);

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
                "feedback_received",
                activity[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "feedback_closed_out",
                activity[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "mark_resolved",
                activity[1].GetProperty("closeOutIntent").GetString()
            );
        }

        [Fact]
        public async Task CloseOut_Other_CreatesNoteAndEmitsNoteAdded()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "close-out-other-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                workflowStatus: FeedbackWorkflowStatus.InProgress
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/close-out"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "mark_no_action_needed",
                reason = "other",
                noteBody = "Closed after phone call with guest.",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.Equal(
                "feedback_closed_out",
                postBody.GetProperty("activityEvent").GetProperty("kind").GetString()
            );
            Assert.Equal(
                "mark_no_action_needed",
                postBody.GetProperty("activityEvent")
                    .GetProperty("closeOutIntent")
                    .GetString()
            );
            var noteEvent = postBody.GetProperty("noteActivityEvent");
            Assert.Equal(
                "note_added",
                noteEvent.GetProperty("kind").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var note = await context.FeedbackInternalNotes
                .AsNoTracking()
                .SingleAsync(n => n.FeedbackId == seeded.FeedbackId);
            Assert.Equal("Closed after phone call with guest.", note.Body);

            var closeOut = await context.FeedbackCloseOuts
                .AsNoTracking()
                .SingleAsync(c => c.FeedbackId == seeded.FeedbackId);
            Assert.Equal(note.Id, closeOut.InternalNoteId);

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getResponse = await _client.SendAsync(get);
            var getBody = await ReadJsonAsync(getResponse);
            var activity = getBody.GetProperty("activityHistory");
            Assert.Equal(3, activity.GetArrayLength());
            var kinds = activity.EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToArray();
            Assert.Contains("feedback_received", kinds);
            Assert.Contains("note_added", kinds);
            Assert.Contains("feedback_closed_out", kinds);
            Assert.DoesNotContain("workflow_status_changed", kinds);
        }

        [Fact]
        public async Task CloseOut_Returns409_WhenAlreadyResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "close-out-already-resolved-tok",
                ClassificationStatus.Pending,
                sentiment: null,
                workflowStatus: FeedbackWorkflowStatus.Resolved
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/close-out"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "mark_resolved",
                reason = "test_or_invalid",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CloseOut_Returns400_WhenOtherMissingNote()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "close-out-other-missing-note-tok",
                ClassificationStatus.Pending,
                sentiment: null
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/close-out"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "mark_resolved",
                reason = "other",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CloseOut_Returns400_WhenNonOtherHasNoteBody()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "close-out-non-other-note-tok",
                ClassificationStatus.Pending,
                sentiment: null
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/close-out"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "mark_resolved",
                reason = "duplicate_submission",
                noteBody = "should not be sent",
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
            ClassificationStatus classificationStatus,
            FeedbackSentiment? sentiment,
            FeedbackWorkflowStatus workflowStatus = FeedbackWorkflowStatus.New,
            string email = "close-out-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Close-out Owner",
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
                Name = "Close-out Venue",
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
