using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackDetectedTagsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackDetectedTagsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UpdateDetectedTags_ReplacesTags_WhenSucceeded()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-replace-tok-12345",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                detectedTagsJson: """["Service"]"""
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "FoodQuality", "WaitTime" },
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
                "negative",
                body.GetProperty("sentiment").GetString()
            );
            var tags = body.GetProperty("detectedTags")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToArray();
            Assert.Equal(new[] { "FoodQuality", "WaitTime" }, tags);
            Assert.Equal(
                "detected_tags_updated",
                body.GetProperty("activityEvent").GetProperty("kind").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(
                """["FoodQuality","WaitTime"]""",
                feedback.DetectedTagsJson
            );
            Assert.Equal(1, await context.FeedbackDetectedTagsChanges.CountAsync(
                c => c.FeedbackId == seeded.FeedbackId
            ));
        }

        [Fact]
        public async Task UpdateDetectedTags_AllowsEmptySet_WhenSucceeded()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-empty-tok-1234567",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                detectedTagsJson: """["Service"]"""
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = Array.Empty<string>(),
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("detectedTags").GetArrayLength());
        }

        [Fact]
        public async Task UpdateDetectedTags_AcceptsOtherAlone()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-other-alone-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                detectedTagsJson: "[]"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Other" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns400_WhenOtherCombined()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-other-combo-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                detectedTagsJson: "[]"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Other", "Service" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns400_WhenDuplicateKeys()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-dup-keys-tok-12",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                detectedTagsJson: "[]"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service", "Service" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns400_WhenUnknownKey()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-unknown-key-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                detectedTagsJson: "[]"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "NotARealTag" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Idempotent_NoFact_WhenUnchanged()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-noop-tok-123456",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                detectedTagsJson: """["Service","FoodQuality"]"""
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "FoodQuality", "Service" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("activityEvent").ValueKind
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await context.FeedbackDetectedTagsChanges.CountAsync(
                c => c.FeedbackId == seeded.FeedbackId
            ));
        }

        [Fact]
        public async Task UpdateDetectedTags_PromotesFailed_WithSentiment()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-failed-promote-tok",
                ClassificationStatus.Failed,
                sentiment: null,
                detectedTagsJson: null,
                classificationRetryable: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service" },
                sentiment = "negative",
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "Succeeded",
                body.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                "negative",
                body.GetProperty("sentiment").GetString()
            );
            Assert.True(body.GetProperty("needsAttention").GetBoolean());
            Assert.True(body.TryGetProperty("classifiedAt", out var classifiedAt));
            Assert.NotEqual(JsonValueKind.Null, classifiedAt.ValueKind);

            var activity = body.GetProperty("activityEvent");
            Assert.Equal(
                "detected_tags_updated",
                activity.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "negative",
                activity.GetProperty("toSentiment").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(ClassificationStatus.Succeeded, feedback.ClassificationStatus);
            Assert.Equal(FeedbackSentiment.Negative, feedback.Sentiment);
            Assert.NotNull(feedback.ClassifiedAt);
            Assert.False(feedback.ClassificationRetryable);
            Assert.Null(feedback.ClassificationRetryAfter);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns400_WhenFailedMissingSentiment()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-failed-no-sent-tok",
                ClassificationStatus.Failed,
                sentiment: null,
                detectedTagsJson: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns400_WhenSucceededSuppliesSentiment()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-succeeded-sent-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                detectedTagsJson: "[]"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service" },
                sentiment = "negative",
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns409_WhenPending()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-pending-tok-1234",
                ClassificationStatus.Pending,
                sentiment: null,
                detectedTagsJson: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_Returns403_ForNonOwnedFeedback()
        {
            var owner = await SeedOwnerWithFeedbackAsync(
                "detected-tags-owner-a-tok-123",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                detectedTagsJson: "[]"
            );
            var other = await SeedOwnerWithFeedbackAsync(
                "detected-tags-owner-b-tok-123",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                detectedTagsJson: "[]",
                email: "other-detected-tags-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{other.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDetectedTags_UnionsGuestTags_DoesNotRemoveOnShrink()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-guest-union-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                detectedTagsJson: """["Service"]""",
                withLocationGuest: true
            );

            using var addRequest = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            addRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            addRequest.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service", "FoodQuality" },
            });
            var addResponse = await _client.SendAsync(addRequest);
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            using var scope1 = _factory.Services.CreateScope();
            var context1 = scope1.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var membershipCountAfterAdd = await context1.LocationGuestTags
                .CountAsync(m => m.LocationGuestId == seeded.LocationGuestId);
            Assert.Equal(2, membershipCountAfterAdd);

            using var shrinkRequest = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            shrinkRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            shrinkRequest.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Service" },
            });
            var shrinkResponse = await _client.SendAsync(shrinkRequest);
            Assert.Equal(HttpStatusCode.OK, shrinkResponse.StatusCode);

            using var scope2 = _factory.Services.CreateScope();
            var context2 = scope2.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var membershipCountAfterShrink = await context2.LocationGuestTags
                .CountAsync(m => m.LocationGuestId == seeded.LocationGuestId);
            Assert.Equal(2, membershipCountAfterShrink);
        }

        [Fact]
        public async Task UpdateDetectedTags_Succeeds_WithoutLocationGuest()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-no-guest-tok-12",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                detectedTagsJson: "[]",
                withLocationGuest: false
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Billing" },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackDetails_IncludesDetectedTagsUpdatedActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "detected-tags-details-hist-tok",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                detectedTagsJson: "[]"
            );

            using var put = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/detected-tags"
            );
            put.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            put.Content = JsonContent.Create(new
            {
                detectedTags = new[] { "Atmosphere" },
            });
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(put)).StatusCode);

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var getResponse = await _client.SendAsync(get);
            var getBody = await ReadJsonAsync(getResponse);

            var activity = getBody.GetProperty("activityHistory");
            Assert.Contains(
                activity.EnumerateArray(),
                e => e.GetProperty("kind").GetString() == "detected_tags_updated"
            );
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId,
            int? LocationGuestId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            ClassificationStatus classificationStatus,
            FeedbackSentiment? sentiment,
            string? detectedTagsJson,
            string email = "detected-tags-owner@example.com",
            bool withLocationGuest = false,
            bool classificationRetryable = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Detected Tags Owner",
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
                Name = "Detected Tags Venue",
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
            if (withLocationGuest)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = "alex@example.com",
                    NormalizedEmail = "alex@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = location.Id,
                    Name = "Alex Guest",
                    CreatedAt = DateTime.UtcNow,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();
                locationGuestId = locationGuest.Id;
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuestId,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = classificationStatus,
                Sentiment = sentiment,
                ClassifiedAt =
                    classificationStatus == ClassificationStatus.Succeeded
                        ? DateTime.UtcNow
                        : null,
                DetectedTagsJson = detectedTagsJson,
                ClassificationRetryable = classificationRetryable,
                ClassificationRetryAfter = classificationRetryable
                    ? DateTime.UtcNow.AddHours(1)
                    : null,
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
