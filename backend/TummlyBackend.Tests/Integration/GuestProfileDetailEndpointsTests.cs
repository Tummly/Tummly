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
            Assert.Equal(JsonValueKind.Null, summary.GetProperty("guestTags").ValueKind);

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
    }
}
