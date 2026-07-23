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
    public class GuestProfileDetailEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestProfileDetailEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGuestProfile_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/guests/1?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestProfile_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("guest-profile-owner-a-token12");
            var other = await SeedOwnerWithGuestAsync(
                "guest-profile-owner-b-token12",
                ownerEmail: "guest-profile-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(other.LocationGuestId, other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestProfile_Returns404_ForUnknownLocation()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "guest-profile-unknown-loc-tk"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.LocationGuestId, 999_999)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Location not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetGuestProfile_Returns404_ForUnknownGuest()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "guest-profile-unknown-gst-tk"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(999_999, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetGuestProfile_Returns404_WhenGuestBelongsToDifferentLocation()
        {
            var seeded = await SeedOwnerWithGuestOnSecondLocationAsync(
                "guest-profile-wrong-loc-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.OtherLocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetGuestProfile_ReturnsEnvelopeMatchingContract()
        {
            var guestSinceAt = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var olderFeedbackAt = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
            var latestFeedbackAt = new DateTime(2026, 7, 20, 14, 22, 0, DateTimeKind.Utc);

            var seeded = await SeedOwnerWithGuestAsync(
                "guest-profile-envelope-token1",
                name: "Amelia Hart",
                guestEmail: "amelia@example.com",
                mobile: null,
                offersOptOut: false,
                guestSinceAt: guestSinceAt,
                feedbackCreatedAts: [olderFeedbackAt, latestFeedbackAt]
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(seeded.LocationId, body.GetProperty("locationId").GetInt32());
            Assert.Equal(seeded.LocationGuestId, body.GetProperty("id").GetInt32());
            Assert.Equal("Amelia Hart", body.GetProperty("name").GetString());
            Assert.Equal(
                "Eligible — Email",
                body.GetProperty("marketingStatus").GetString()
            );
            Assert.False(body.GetProperty("offersOptOut").GetBoolean());
            Assert.Equal(
                guestSinceAt,
                body.GetProperty("guestSinceAt").GetDateTime()
            );
            Assert.Equal(
                latestFeedbackAt,
                body.GetProperty("lastActivityAt").GetDateTime()
            );
            Assert.Equal(
                "Feedback submitted",
                body.GetProperty("lastInteractionLabel").GetString()
            );

            var summary = body.GetProperty("profileSummary");
            Assert.Equal("amelia@example.com", summary.GetProperty("email").GetString());
            Assert.Equal(JsonValueKind.Null, summary.GetProperty("mobile").ValueKind);
            Assert.Equal(
                guestSinceAt,
                summary.GetProperty("firstCapturedAt").GetDateTime()
            );
            Assert.Equal("Camden Street", summary.GetProperty("locationName").GetString());
            Assert.Equal(2, summary.GetProperty("feedbackSubmissionCount").GetInt32());
            Assert.Equal(0, summary.GetProperty("offerClaimsAndRedemptions").GetInt32());
            Assert.Equal(
                latestFeedbackAt,
                summary.GetProperty("lastInteractionAt").GetDateTime()
            );
            Assert.Equal(
                "Feedback submitted",
                summary.GetProperty("lastInteractionLabel").GetString()
            );
            Assert.Equal(JsonValueKind.Array, summary.GetProperty("guestTags").ValueKind);
            Assert.Empty(summary.GetProperty("guestTags").EnumerateArray());

            var overview = body.GetProperty("overviewDetails");
            Assert.Equal(
                guestSinceAt,
                overview.GetProperty("guestSinceAt").GetDateTime()
            );
            Assert.Equal(2, overview.GetProperty("totalInteractions").GetInt32());
            Assert.Equal(2, overview.GetProperty("feedbackReceived").GetInt32());
            Assert.Equal(0, overview.GetProperty("offersClaimed").GetInt32());
            Assert.Equal(0, overview.GetProperty("campaignsSent").GetInt32());
            Assert.Equal(
                latestFeedbackAt,
                overview.GetProperty("lastActivityAt").GetDateTime()
            );

            var eligibility = body.GetProperty("contactEligibility")
                .EnumerateArray()
                .ToList();
            Assert.Equal(2, eligibility.Count);

            Assert.Equal("email", eligibility[0].GetProperty("channel").GetString());
            Assert.Equal("eligible", eligibility[0].GetProperty("status").GetString());
            Assert.Equal(
                "consent_captured",
                eligibility[0].GetProperty("detailKind").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                eligibility[0].GetProperty("detailAt").ValueKind
            );

            Assert.Equal("sms", eligibility[1].GetProperty("channel").GetString());
            Assert.Equal("not_provided", eligibility[1].GetProperty("status").GetString());
            Assert.Equal(
                JsonValueKind.Null,
                eligibility[1].GetProperty("detailKind").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                eligibility[1].GetProperty("detailAt").ValueKind
            );

            var latestFeedback = body.GetProperty("latestFeedback")
                .EnumerateArray()
                .ToList();
            Assert.Equal(2, latestFeedback.Count);
            Assert.Equal(
                latestFeedbackAt,
                latestFeedback[0].GetProperty("createdAt").GetDateTime()
            );
            Assert.Equal(
                olderFeedbackAt,
                latestFeedback[1].GetProperty("createdAt").GetDateTime()
            );
            Assert.Equal(
                "Camden Street",
                latestFeedback[0].GetProperty("locationName").GetString()
            );
            Assert.Equal(
                "Succeeded",
                latestFeedback[0].GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                "positive",
                latestFeedback[0].GetProperty("sentiment").GetString()
            );
            Assert.Equal(
                JsonValueKind.Array,
                latestFeedback[0].GetProperty("detectedTags").ValueKind
            );

            var recentNotes = body.GetProperty("recentNotes")
                .EnumerateArray()
                .ToList();
            Assert.Empty(recentNotes);
        }

        [Fact]
        public async Task GetGuestProfile_ReturnsLiveGuestTagsFromMemberships()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "guest-profile-live-tags-token1",
                name: "Tagged Guest",
                guestEmail: "tagged@example.com"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var tagging = scope.ServiceProvider
                    .GetRequiredService<IGuestTaggingService>();

                var restaurantId = await context.RestaurantLocations
                    .Where(l => l.Id == seeded.LocationId)
                    .Select(l => l.RestaurantId)
                    .SingleAsync();

                var vip = await tagging.CreateByNameAsync(restaurantId, "VIP Guest");
                var regular = await tagging.CreateByNameAsync(
                    restaurantId,
                    "Regular"
                );

                await tagging.ApplyAdditiveAsync(
                    restaurantId,
                    new[] { seeded.LocationId },
                    new[] { seeded.LocationGuestId },
                    new[] { vip.Id, regular.Id }
                );
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var guestTags = body.GetProperty("profileSummary")
                .GetProperty("guestTags")
                .EnumerateArray()
                .Select(t => (
                    Id: t.GetProperty("id").GetInt32(),
                    Name: t.GetProperty("name").GetString()
                ))
                .ToList();

            Assert.Equal(2, guestTags.Count);
            Assert.Equal(
                new[] { "Regular", "VIP Guest" },
                guestTags.Select(t => t.Name).ToArray()
            );
        }

        [Fact]
        public async Task GetGuestProfile_ReturnsLatestFeedbackCappedAtThreeNewest()
        {
            var guestSinceAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
            var t1 = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2026, 7, 15, 13, 0, 0, DateTimeKind.Utc);
            var t3 = new DateTime(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);
            var t4 = new DateTime(2026, 7, 22, 15, 0, 0, DateTimeKind.Utc);

            var seeded = await SeedOwnerWithGuestAndFeedbackRowsAsync(
                "guest-profile-latest-fb-token1",
                guestSinceAt,
                [
                    new FeedbackSeedRow(
                        t1,
                        "Oldest comment",
                        ClassificationStatus.Succeeded,
                        FeedbackSentiment.Positive,
                        """["FoodQuality"]"""
                    ),
                    new FeedbackSeedRow(
                        t2,
                        "Pending comment",
                        ClassificationStatus.Pending,
                        null,
                        null
                    ),
                    new FeedbackSeedRow(
                        t3,
                        "Negative comment",
                        ClassificationStatus.Succeeded,
                        FeedbackSentiment.Negative,
                        """["WaitTime"]"""
                    ),
                    new FeedbackSeedRow(
                        t4,
                        "Newest failed",
                        ClassificationStatus.Failed,
                        null,
                        null
                    ),
                ]
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var latestFeedback = body.GetProperty("latestFeedback")
                .EnumerateArray()
                .ToList();
            Assert.Equal(3, latestFeedback.Count);

            Assert.Equal(t4, latestFeedback[0].GetProperty("createdAt").GetDateTime());
            Assert.Equal("Newest failed", latestFeedback[0].GetProperty("comment").GetString());
            Assert.Equal("Failed", latestFeedback[0].GetProperty("classificationStatus").GetString());
            Assert.Equal(JsonValueKind.Null, latestFeedback[0].GetProperty("sentiment").ValueKind);
            Assert.Equal(JsonValueKind.Null, latestFeedback[0].GetProperty("detectedTags").ValueKind);

            Assert.Equal(t3, latestFeedback[1].GetProperty("createdAt").GetDateTime());
            Assert.Equal("negative", latestFeedback[1].GetProperty("sentiment").GetString());
            var tags = latestFeedback[1].GetProperty("detectedTags")
                .EnumerateArray()
                .Select(t => t.GetString())
                .ToList();
            Assert.Equal(["WaitTime"], tags);

            Assert.Equal(t2, latestFeedback[2].GetProperty("createdAt").GetDateTime());
            Assert.Equal("Pending", latestFeedback[2].GetProperty("classificationStatus").GetString());
            Assert.Equal(JsonValueKind.Null, latestFeedback[2].GetProperty("sentiment").ValueKind);
            Assert.Equal(JsonValueKind.Null, latestFeedback[2].GetProperty("detectedTags").ValueKind);

            Assert.Equal(4, body.GetProperty("overviewDetails")
                .GetProperty("feedbackReceived")
                .GetInt32());
        }

        [Fact]
        public async Task GetGuestProfile_ReturnsRecentNotesCappedAtThreeNewest()
        {
            var guestSinceAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
            var t1 = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2026, 7, 15, 13, 0, 0, DateTimeKind.Utc);
            var t3 = new DateTime(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);
            var t4 = new DateTime(2026, 7, 22, 15, 0, 0, DateTimeKind.Utc);

            var seeded = await SeedOwnerWithGuestAndNotesAsync(
                "guest-profile-recent-notes-tk",
                guestSinceAt,
                [
                    new NoteSeedRow(t1, "Oldest note", "Author One"),
                    new NoteSeedRow(t2, "Second note", "Author Two"),
                    new NoteSeedRow(t3, "Third note", "Author Three"),
                    new NoteSeedRow(t4, "Newest note", "Author Four"),
                ]
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var recentNotes = body.GetProperty("recentNotes")
                .EnumerateArray()
                .ToList();
            Assert.Equal(3, recentNotes.Count);

            Assert.Equal(t4, recentNotes[0].GetProperty("createdAt").GetDateTime());
            Assert.Equal("Newest note", recentNotes[0].GetProperty("body").GetString());
            Assert.Equal(
                "Author Four",
                recentNotes[0].GetProperty("authorDisplayName").GetString()
            );

            Assert.Equal(t3, recentNotes[1].GetProperty("createdAt").GetDateTime());
            Assert.Equal("Third note", recentNotes[1].GetProperty("body").GetString());

            Assert.Equal(t2, recentNotes[2].GetProperty("createdAt").GetDateTime());
            Assert.Equal("Second note", recentNotes[2].GetProperty("body").GetString());
        }

        [Fact]
        public async Task GetGuestProfile_MarksChannelsUnsubscribed_WhenOffersOptOut()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "guest-profile-optout-token123",
                name: "Opt Out Sam",
                guestEmail: "sam@example.com",
                mobile: "07700900456",
                offersOptOut: true,
                guestSinceAt: DateTime.UtcNow.AddDays(-10),
                feedbackCreatedAts: [DateTime.UtcNow.AddDays(-8)]
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Not eligible",
                body.GetProperty("marketingStatus").GetString()
            );
            Assert.True(body.GetProperty("offersOptOut").GetBoolean());

            var eligibility = body.GetProperty("contactEligibility")
                .EnumerateArray()
                .ToList();

            Assert.Equal("unsubscribed", eligibility[0].GetProperty("status").GetString());
            Assert.Equal(
                "unsubscribed",
                eligibility[0].GetProperty("detailKind").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                eligibility[0].GetProperty("detailAt").ValueKind
            );

            Assert.Equal("unsubscribed", eligibility[1].GetProperty("status").GetString());
            Assert.Equal(
                "unsubscribed",
                eligibility[1].GetProperty("detailKind").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                eligibility[1].GetProperty("detailAt").ValueKind
            );
        }

        private static string GuestUrl(int guestId, int locationId)
        {
            return $"/api/guests/{guestId}?locationId={locationId}";
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string linkToken,
            string email = "guest-profile-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Profile Owner",
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
                Name = "Guest Profile Venue",
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
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<GuestSeed> SeedOwnerWithGuestAsync(
            string linkToken,
            string ownerEmail = "guest-profile-owner@example.com",
            string name = "Amelia Hart",
            string? guestEmail = "amelia@example.com",
            string? mobile = null,
            bool offersOptOut = false,
            DateTime? guestSinceAt = null,
            IReadOnlyList<DateTime>? feedbackCreatedAts = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Profile Owner",
                Email = ownerEmail,
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
                Name = "Guest Profile Venue",
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
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var capturedAt = guestSinceAt ?? DateTime.UtcNow.AddDays(-5);

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = guestEmail,
                NormalizedEmail = guestEmail?.ToLowerInvariant(),
                Mobile = mobile,
                NormalizedPhone = mobile == null
                    ? null
                    : new string(mobile.Where(char.IsDigit).ToArray()),
                CreatedAt = capturedAt,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = name,
                OffersOptOut = offersOptOut,
                CreatedAt = capturedAt,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            foreach (var createdAt in feedbackCreatedAts ?? [])
            {
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = location.Id,
                        LocationGuestId = locationGuest.Id,
                        GuestName = name,
                        GuestContact = guestEmail ?? mobile ?? "unknown",
                        ContactType = guestEmail != null
                            ? ContactType.Email
                            : mobile != null
                                ? ContactType.Phone
                                : ContactType.Unknown,
                        Comment = "Visit note",
                        OffersOptOut = offersOptOut,
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Positive,
                        CreatedAt = createdAt,
                    }
                );
            }

            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id);
        }

        private async Task<GuestSeed> SeedOwnerWithGuestAndFeedbackRowsAsync(
            string linkToken,
            DateTime guestSinceAt,
            IReadOnlyList<FeedbackSeedRow> feedbackRows
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Profile Owner",
                Email = $"{linkToken}@example.com",
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
                Name = "Guest Profile Latest Feedback Venue",
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
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "latest-fb@example.com",
                NormalizedEmail = "latest-fb@example.com",
                CreatedAt = guestSinceAt,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Latest Feedback Guest",
                OffersOptOut = false,
                CreatedAt = guestSinceAt,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            foreach (var row in feedbackRows)
            {
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = location.Id,
                        LocationGuestId = locationGuest.Id,
                        GuestName = locationGuest.Name,
                        GuestContact = "latest-fb@example.com",
                        ContactType = ContactType.Email,
                        Comment = row.Comment,
                        ClassificationStatus = row.ClassificationStatus,
                        Sentiment = row.Sentiment,
                        DetectedTagsJson = row.DetectedTagsJson,
                        CreatedAt = row.CreatedAt,
                    }
                );
            }

            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id);
        }

        private async Task<GuestSeed> SeedOwnerWithGuestAndNotesAsync(
            string linkToken,
            DateTime guestSinceAt,
            IReadOnlyList<NoteSeedRow> noteRows
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Profile Owner",
                Email = $"{linkToken}@example.com",
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
                Name = "Guest Profile Recent Notes Venue",
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
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "recent-notes@example.com",
                NormalizedEmail = "recent-notes@example.com",
                CreatedAt = guestSinceAt,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Recent Notes Guest",
                OffersOptOut = false,
                CreatedAt = guestSinceAt,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            foreach (var row in noteRows)
            {
                context.LocationGuestNotes.Add(
                    new LocationGuestNote
                    {
                        LocationGuestId = locationGuest.Id,
                        Body = row.Body,
                        AuthorUserId = user.Id,
                        AuthorDisplayName = row.AuthorDisplayName,
                        CreatedAt = row.CreatedAt,
                    }
                );
            }

            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int OtherLocationGuestId
        )> SeedOwnerWithGuestOnSecondLocationAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Profile Owner",
                Email = "guest-profile-twoloc@example.com",
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
                Name = "Guest Profile Two Loc Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var primary = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = $"{linkToken}-a",
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var secondary = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = $"{linkToken}-b",
                LocationName = "Soho",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.AddRange(primary, secondary);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "elsewhere@example.com",
                NormalizedEmail = "elsewhere@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var otherGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = secondary.Id,
                Name = "Elsewhere Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(otherGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, primary.Id, otherGuest.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private sealed record GuestSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId
        );

        private sealed record FeedbackSeedRow(
            DateTime CreatedAt,
            string Comment,
            ClassificationStatus ClassificationStatus,
            FeedbackSentiment? Sentiment,
            string? DetectedTagsJson
        );

        private sealed record NoteSeedRow(
            DateTime CreatedAt,
            string Body,
            string AuthorDisplayName
        );
    }
}
