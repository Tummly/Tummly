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
    public class GuestsListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestsListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGuests_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/guests?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("guests-owner-a-token-123456");
            var other = await SeedOwnerAsync(
                "guests-owner-b-token-123456",
                email: "guests-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns404_ForUnknownLocation()
        {
            var owner = await SeedOwnerAsync("guests-unknown-loc-token-12");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(999_999)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenPageSizeNot25()
        {
            var owner = await SeedOwnerAsync("guests-pagesize-token-123456");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&pageSize=50"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenInvalidSmartGroup()
        {
            var owner = await SeedOwnerAsync("guests-bad-group-token-12345");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&smartGroup=not-a-group"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenInvalidSort()
        {
            var owner = await SeedOwnerAsync("guests-bad-sort-token-123456");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&sort=not-a-sort"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenPageLessThanOne()
        {
            var owner = await SeedOwnerAsync("guests-bad-page-token-123456");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&page=0"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_ReturnsEnvelopeOverviewAndRows()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-envelope-token-123456789"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(seeded.LocationId, body.GetProperty("locationId").GetInt32());
            Assert.Equal("all-guests", body.GetProperty("smartGroup").GetString());
            Assert.Equal("", body.GetProperty("q").GetString());
            Assert.Equal("recent-activity", body.GetProperty("sort").GetString());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(5, body.GetProperty("totalFilteredCount").GetInt32());

            var overview = body.GetProperty("overview");
            Assert.Equal(5, overview.GetProperty("totalGuests").GetInt32());
            Assert.Equal(3, overview.GetProperty("newThisMonth").GetInt32());
            Assert.Equal(3, overview.GetProperty("marketingEligible").GetInt32());
            Assert.Equal(0, overview.GetProperty("needsRecovery").GetInt32());

            var counts = body.GetProperty("smartGroupCounts");
            Assert.Equal(5, counts.GetProperty("all-guests").GetInt32());
            Assert.Equal(3, counts.GetProperty("new-guests").GetInt32());
            Assert.Equal(0, counts.GetProperty("needs-recovery").GetInt32());
            Assert.Equal(1, counts.GetProperty("positive-feedback").GetInt32());
            Assert.Equal(0, counts.GetProperty("offer-not-redeemed").GetInt32());
            Assert.Equal(0, counts.GetProperty("recent-redeemers").GetInt32());
            Assert.Equal(1, counts.GetProperty("dormant-guests").GetInt32());

            var rows = body.GetProperty("rows").EnumerateArray().ToList();
            Assert.Equal(5, rows.Count);

            var jane = rows.Single(r =>
                r.GetProperty("name").GetString() == "Jane Doe"
            );
            Assert.Equal(seeded.JaneLocationGuestId.ToString(), jane.GetProperty("id").GetString());
            Assert.Equal("jane@example.com", jane.GetProperty("email").GetString());
            Assert.True(jane.GetProperty("mobile").ValueKind == JsonValueKind.Null);
            Assert.Equal("Eligible — Email", jane.GetProperty("marketingStatus").GetString());
            Assert.Equal("Camden Street", jane.GetProperty("locationName").GetString());
            Assert.Equal("positive", jane.GetProperty("latestFeedbackSentiment").GetString());
            Assert.Equal(2, jane.GetProperty("feedbackSubmissionCount").GetInt32());
            Assert.Equal("Feedback submitted", jane.GetProperty("lastInteractionLabel").GetString());
            Assert.NotNull(jane.GetProperty("lastInteractionAt").GetString());
            Assert.NotNull(jane.GetProperty("capturedAt").GetString());
        }

        [Fact]
        public async Task GetGuests_OverviewAndCountsStayLocationWide_WhenFiltered()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-wide-counts-token-123456"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=new-guests&q=jane"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(5, body.GetProperty("overview").GetProperty("totalGuests").GetInt32());
            Assert.Equal(
                3,
                body.GetProperty("smartGroupCounts").GetProperty("new-guests").GetInt32()
            );
            Assert.Single(body.GetProperty("rows").EnumerateArray());
        }

        [Fact]
        public async Task GetGuests_FiltersNewGuestsSmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-new-group-token-1234567"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=new-guests"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(3, body.GetProperty("totalFilteredCount").GetInt32());
            var names = body.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .OrderBy(n => n)
                .ToList();
            Assert.Equal(
                new[] { "Jane Doe", "No Feedback", "Opt Out Sam" },
                names
            );
        }

        [Fact]
        public async Task GetGuests_FiltersPositiveFeedbackSmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-positive-token-123456789"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=positive-feedback"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Jane Doe",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GetGuests_FiltersDormantGuestsSmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-dormant-token-1234567890"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=dormant-guests"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Old Pat",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Theory]
        [InlineData("needs-recovery")]
        [InlineData("offer-not-redeemed")]
        [InlineData("recent-redeemers")]
        public async Task GetGuests_DeferredSmartGroups_ReturnZeroRows(
            string smartGroup
        )
        {
            var seeded = await SeedGuestsScenarioAsync(
                $"guests-deferred-{smartGroup}-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup={smartGroup}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(0, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Empty(body.GetProperty("rows").EnumerateArray());
            Assert.Equal(
                0,
                body.GetProperty("smartGroupCounts").GetProperty(smartGroup).GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_SearchMatchesNameEmailOrMobile()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-search-token-1234567890"
            );

            using var emailRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&q=07700900456"
            );
            emailRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var emailResponse = await _client.SendAsync(emailRequest);
            var emailBody = await ReadJsonAsync(emailResponse);
            Assert.Equal(1, emailBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Bob Mobile",
                emailBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );

            using var nameRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&q=opt"
            );
            nameRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var nameResponse = await _client.SendAsync(nameRequest);
            var nameBody = await ReadJsonAsync(nameResponse);
            Assert.Equal(1, nameBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Opt Out Sam",
                nameBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GetGuests_SortsAndPaginates()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-sort-page-token-123456789"
            );

            using var sortRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=guest-name-az"
            );
            sortRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var sortResponse = await _client.SendAsync(sortRequest);
            var sortBody = await ReadJsonAsync(sortResponse);
            var sortedNames = sortBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .ToList();
            Assert.Equal(
                new[]
                {
                    "Bob Mobile",
                    "Jane Doe",
                    "No Feedback",
                    "Old Pat",
                    "Opt Out Sam",
                },
                sortedNames
            );

            using var pageRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=guest-name-az&page=2&pageSize=25"
            );
            pageRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var pageResponse = await _client.SendAsync(pageRequest);
            var pageBody = await ReadJsonAsync(pageResponse);
            Assert.Equal(5, pageBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Empty(pageBody.GetProperty("rows").EnumerateArray());
        }

        [Fact]
        public async Task GetGuests_MostRecentRedemptionSort_FallsBackToRecentActivity()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-redemption-sort-token-123"
            );

            using var recentRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=recent-activity"
            );
            recentRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            using var redemptionRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=most-recent-redemption"
            );
            redemptionRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var recentBody = await ReadJsonAsync(
                await _client.SendAsync(recentRequest)
            );
            var redemptionBody = await ReadJsonAsync(
                await _client.SendAsync(redemptionRequest)
            );

            var recentIds = recentBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("id").GetString())
                .ToList();
            var redemptionIds = redemptionBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("id").GetString())
                .ToList();

            Assert.Equal(recentIds, redemptionIds);
        }

        [Fact]
        public async Task GetGuests_DerivesMarketingStatusAndSentimentFromLatestSucceeded()
        {
            var seeded = await SeedOwnerAsync("guests-derive-token-123456789");

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = seeded.RestaurantId,
                Email = "mixed@example.com",
                NormalizedEmail = "mixed@example.com",
                Mobile = "07700900999",
                NormalizedPhone = "07700900999",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = seeded.LocationId,
                Name = "Mixed Channels",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    LocationGuestId = locationGuest.Id,
                    GuestName = "Mixed Channels",
                    GuestContact = "mixed@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Older positive",
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Positive,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                },
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    LocationGuestId = locationGuest.Id,
                    GuestName = "Mixed Channels",
                    GuestContact = "mixed@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Latest pending",
                    ClassificationStatus = ClassificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                }
            );
            await context.SaveChangesAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            var row = body.GetProperty("rows")
                .EnumerateArray()
                .Single();

            Assert.Equal("Eligible — Email", row.GetProperty("marketingStatus").GetString());
            Assert.Equal("positive", row.GetProperty("latestFeedbackSentiment").GetString());
        }

        private static string GuestsUrl(int locationId)
        {
            return $"/api/guests?locationId={locationId}";
        }

        private async Task<(string Jwt, int LocationId, int RestaurantId)> SeedOwnerAsync(
            string linkToken,
            string email = "guests-owner@example.com",
            string locationName = "Camden Street"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guests Owner",
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
                Name = "Guests Venue",
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
                LocationName = locationName,
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

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<GuestsScenarioSeed> SeedGuestsScenarioAsync(
            string linkToken
        )
        {
            var owner = await SeedOwnerAsync(linkToken, locationName: "Camden Street");
            var now = DateTime.UtcNow;

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            async Task<(int LocationGuestId, MasterGuest Master)> AddGuestAsync(
                string name,
                string? email,
                string? normalizedEmail,
                string? mobile,
                string? normalizedPhone,
                bool offersOptOut,
                DateTime capturedAt,
                IEnumerable<(DateTime CreatedAt, ClassificationStatus Status, FeedbackSentiment? Sentiment)> feedbacks
            )
            {
                var master = new MasterGuest
                {
                    RestaurantId = owner.RestaurantId,
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    Mobile = mobile,
                    NormalizedPhone = normalizedPhone,
                    CreatedAt = capturedAt,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = owner.LocationId,
                    Name = name,
                    OffersOptOut = offersOptOut,
                    CreatedAt = capturedAt,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();

                foreach (var feedback in feedbacks)
                {
                    context.Feedbacks.Add(
                        new Feedback
                        {
                            RestaurantLocationId = owner.LocationId,
                            LocationGuestId = locationGuest.Id,
                            GuestName = name,
                            GuestContact = email ?? mobile ?? "unknown",
                            ContactType = email != null
                                ? ContactType.Email
                                : mobile != null
                                    ? ContactType.Phone
                                    : ContactType.Unknown,
                            Comment = "Visit note",
                            OffersOptOut = offersOptOut,
                            ClassificationStatus = feedback.Status,
                            Sentiment = feedback.Sentiment,
                            CreatedAt = feedback.CreatedAt,
                        }
                    );
                }

                await context.SaveChangesAsync();
                return (locationGuest.Id, master);
            }

            var jane = await AddGuestAsync(
                "Jane Doe",
                "jane@example.com",
                "jane@example.com",
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-5),
                feedbacks:
                [
                    (now.AddDays(-6), ClassificationStatus.Succeeded, FeedbackSentiment.Neutral),
                    (now.AddDays(-2), ClassificationStatus.Succeeded, FeedbackSentiment.Positive),
                ]
            );

            await AddGuestAsync(
                "Bob Mobile",
                null,
                null,
                "07700 900456",
                "07700900456",
                offersOptOut: false,
                capturedAt: now.AddDays(-60),
                feedbacks:
                [
                    (now.AddDays(-10), ClassificationStatus.Pending, null),
                ]
            );

            await AddGuestAsync(
                "Old Pat",
                "pat@example.com",
                "pat@example.com",
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-200),
                feedbacks:
                [
                    (now.AddDays(-100), ClassificationStatus.Succeeded, FeedbackSentiment.Negative),
                ]
            );

            await AddGuestAsync(
                "Opt Out Sam",
                "sam@example.com",
                "sam@example.com",
                null,
                null,
                offersOptOut: true,
                capturedAt: now.AddDays(-10),
                feedbacks:
                [
                    (now.AddDays(-8), ClassificationStatus.Succeeded, FeedbackSentiment.Neutral),
                ]
            );

            await AddGuestAsync(
                "No Feedback",
                null,
                null,
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-2),
                feedbacks: Array.Empty<(DateTime, ClassificationStatus, FeedbackSentiment?)>()
            );

            return new GuestsScenarioSeed(
                owner.Jwt,
                owner.LocationId,
                jane.LocationGuestId
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private sealed record GuestsScenarioSeed(
            string Jwt,
            int LocationId,
            int JaneLocationGuestId
        );
    }
}
