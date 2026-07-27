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
    /// <summary>
    /// Seam: POST /api/feedback/{id}/notes and GET details notes/activity.
    /// </summary>
    public class FeedbackInternalNotesEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackInternalNotesEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostFeedbackNote_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsJsonAsync(
                "/api/feedback/1/notes",
                new { body = "Hello" }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostFeedbackNote_CreatesAndReturnsNote()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-create-token-123456"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/notes"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new { body = "  Called the kitchen  " }
            );

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Called the kitchen",
                body.GetProperty("note").GetProperty("body").GetString()
            );
            Assert.Equal(
                "Feedback Owner",
                body.GetProperty("note")
                    .GetProperty("authorDisplayName")
                    .GetString()
            );
            Assert.True(
                body.GetProperty("note").GetProperty("id").GetInt32() > 0
            );
            Assert.True(
                body.GetProperty("note").TryGetProperty("createdAt", out _)
            );
        }

        [Fact]
        public async Task PostFeedbackNote_Returns403_ForNonOwnedFeedback()
        {
            var owner = await SeedOwnerWithFeedbackAsync(
                "fb-note-owner-a-token-1234",
                email: "fb-note-owner-a@example.com"
            );
            var other = await SeedOwnerWithFeedbackAsync(
                "fb-note-owner-b-token-1234",
                email: "fb-note-owner-b@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{other.FeedbackId}/notes"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new { body = "Blocked" });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostFeedbackNote_Returns404_ForUnknownFeedback()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-missing-token-123"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/feedback/999999/notes"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new { body = "Orphan" });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostFeedbackNote_Returns400_WhenBodyWhitespaceOnly()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-blank-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/notes"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new { body = "   " });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackDetails_IncludesEmptyNotesAndReceivedActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-details-empty-tok"
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

            var notes = body.GetProperty("internalNotes");
            Assert.Equal(JsonValueKind.Array, notes.ValueKind);
            Assert.Equal(0, notes.GetArrayLength());

            var activity = body.GetProperty("activityHistory");
            Assert.Equal(1, activity.GetArrayLength());
            Assert.Equal(
                "feedback_received",
                activity[0].GetProperty("kind").GetString()
            );
        }

        [Fact]
        public async Task GetFeedbackDetails_IncludesNotesNewestFirstAndDerivedActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-details-full-tok1"
            );

            await PostNoteAsync(
                seeded.Jwt,
                seeded.FeedbackId,
                "Older note"
            );
            await PostNoteAsync(
                seeded.Jwt,
                seeded.FeedbackId,
                "Newer note"
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

            var notes = body.GetProperty("internalNotes");
            Assert.Equal(2, notes.GetArrayLength());
            Assert.Equal(
                "Newer note",
                notes[0].GetProperty("body").GetString()
            );
            Assert.Equal(
                "Older note",
                notes[1].GetProperty("body").GetString()
            );

            var activity = body.GetProperty("activityHistory");
            Assert.Equal(3, activity.GetArrayLength());
            Assert.Equal(
                "feedback_received",
                activity[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "note_added",
                activity[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Feedback Owner",
                activity[1].GetProperty("actorDisplayName").GetString()
            );
            Assert.Equal(
                "note_added",
                activity[2].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Feedback Owner",
                activity[2].GetProperty("actorDisplayName").GetString()
            );
        }

        [Fact]
        public async Task PutFeedbackNote_UpdatesBody_AndExposesUpdatedAt()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-put-update-tok123"
            );
            await PostNoteAsync(seeded.Jwt, seeded.FeedbackId, "Original");

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var details = await ReadJsonAsync(await _client.SendAsync(listRequest));
            var noteId = details.GetProperty("internalNotes")[0]
                .GetProperty("id")
                .GetInt32();

            using var putRequest = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/notes/{noteId}"
            );
            putRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            putRequest.Content = JsonContent.Create(new { body = "Corrected" });

            var putResponse = await _client.SendAsync(putRequest);
            var putBody = await ReadJsonAsync(putResponse);

            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
            Assert.Equal(
                "Corrected",
                putBody.GetProperty("note").GetProperty("body").GetString()
            );
            Assert.NotEqual(
                JsonValueKind.Null,
                putBody.GetProperty("note").GetProperty("updatedAt").ValueKind
            );

            using var detailsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            detailsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var after = await ReadJsonAsync(await _client.SendAsync(detailsRequest));
            var kinds = after.GetProperty("activityHistory")
                .EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToList();
            Assert.DoesNotContain("note_deleted", kinds);
            Assert.Equal(2, kinds.Count); // received + note_added (edit is silent)
        }

        [Fact]
        public async Task DeleteFeedbackNote_HidesFromNotes_AddsNoteDeletedActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "fb-note-delete-act-tok12"
            );
            await PostNoteAsync(seeded.Jwt, seeded.FeedbackId, "Mistake");

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var details = await ReadJsonAsync(await _client.SendAsync(listRequest));
            var noteId = details.GetProperty("internalNotes")[0]
                .GetProperty("id")
                .GetInt32();

            using var deleteRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/feedback/{seeded.FeedbackId}/notes/{noteId}"
            );
            deleteRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(deleteRequest)).StatusCode
            );

            using var afterRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            afterRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var after = await ReadJsonAsync(await _client.SendAsync(afterRequest));

            Assert.Equal(0, after.GetProperty("internalNotes").GetArrayLength());
            var kinds = after.GetProperty("activityHistory")
                .EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToList();
            Assert.Contains("note_added", kinds);
            Assert.Contains("note_deleted", kinds);
        }

        private async Task PostNoteAsync(
            string jwt,
            int feedbackId,
            string noteBody
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{feedbackId}/notes"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new { body = noteBody });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int FeedbackId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            string email = "fb-internal-notes-owner@example.com"
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
                Name = "Feedback Notes Venue",
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
                LocationGuestId = null,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, feedback.Id);
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
