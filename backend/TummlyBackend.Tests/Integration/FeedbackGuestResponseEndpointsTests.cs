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
    public class FeedbackGuestResponseEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackGuestResponseEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendGuestResponse_PersistsFact_KeepsInProgress_EmitsActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-send-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry about your visit",
                body = "Thank you for telling us.",
                intent = "respond_to_guest",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
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
                "guest_response_sent",
                activityEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "email",
                activityEvent.GetProperty("channel").GetString()
            );
            Assert.Equal(
                "a••••@example.com",
                activityEvent.GetProperty("maskedDestination").GetString()
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

            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                FeedbackGuestResponseChannel.Email,
                guestResponse.Channel
            );
            Assert.Equal("Sorry about your visit", guestResponse.Subject);
            Assert.Equal("Thank you for telling us.", guestResponse.Body);

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
                "guest_response_sent",
                activity[1].GetProperty("kind").GetString()
            );
        }

        [Fact]
        public async Task SendGuestResponse_Sms_OmitsSubject()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-tok",
                ContactType.Phone,
                "+447700900123",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "sms",
                body = "Thanks for your message.",
                intent = "respond_to_guest",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.Equal(
                "••••0123",
                postBody
                    .GetProperty("activityEvent")
                    .GetProperty("maskedDestination")
                    .GetString()
            );
        }

        [Fact]
        public async Task SendGuestResponse_Returns400_WhenEmailMissingSubject()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-no-subject-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "nosubject-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                body = "Hello",
                intent = "respond_to_guest",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.FeedbackGuestResponses.AnyAsync(
                    r => r.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task CompleteRecovery_Resolves_EmitsRecoveryCompleted_NotBareStatusChange()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-complete-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "complete-owner@example.com"
            );

            using var send = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            send.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            send.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry",
                body = "Thanks",
                intent = "respond_to_guest",
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(send)).StatusCode
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "respond_to_guest",
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
                "respond_to_guest",
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

            Assert.Contains("guest_response_sent", kinds);
            Assert.Contains("recovery_completed", kinds);
            Assert.DoesNotContain("workflow_status_changed", kinds);
            Assert.DoesNotContain("feedback_closed_out", kinds);
        }

        [Fact]
        public async Task CompleteRecovery_Returns400_WhenNoGuestResponse()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-complete-no-response-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "complete-noreq-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "respond_to_guest",
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
            string email = "guest-response-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Response Owner",
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
                Name = "Guest Response Venue",
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

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
