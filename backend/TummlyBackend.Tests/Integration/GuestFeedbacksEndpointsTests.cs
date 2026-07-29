using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestFeedbacksEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestFeedbacksEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGuestFeedbacks_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/guests/1/feedbacks?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestFeedbacks_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("fb-list-owner-a-token12xx");
            var other = await SeedOwnerWithGuestAsync(
                "fb-list-owner-b-token12xx",
                ownerEmail: "fb-list-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(other.LocationGuestId, other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestFeedbacks_Returns404_ForUnknownGuest()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "fb-list-unknown-gst-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(999_999, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetGuestFeedbacks_ListsFiltersSortsAndPaginates()
        {
            var seeded = await SeedOwnerWithGuestAsync("fb-list-filter-tokenxx");

            var older = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
            var mid = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                context.Feedbacks.AddRange(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = seeded.LocationGuestId,
                        GuestName = "Amelia",
                        GuestContact = "amelia@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Love the pasta",
                        OffersOptOut = false,
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Positive,
                        DetectedTagsJson =
                            FeedbackClassificationMapping.SerializeDetectedTags(
                                [DetectedTag.FoodQuality]
                            ),
                        CreatedAt = older,
                    },
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = seeded.LocationGuestId,
                        GuestName = "Amelia",
                        GuestContact = "amelia@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Slow service tonight",
                        OffersOptOut = false,
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        DetectedTagsJson =
                            FeedbackClassificationMapping.SerializeDetectedTags(
                                [DetectedTag.Service, DetectedTag.WaitTime]
                            ),
                        CreatedAt = mid,
                    },
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = seeded.LocationGuestId,
                        GuestName = "Amelia",
                        GuestContact = "amelia@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Still pending note",
                        OffersOptOut = false,
                        ClassificationStatus = ClassificationStatus.Pending,
                        CreatedAt = newer,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var allRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&sort=recent-activity"
            );
            allRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var allResponse = await _client.SendAsync(allRequest);
            var allBody = await ReadJsonAsync(allResponse);

            Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            Assert.Equal(3, allBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, allBody.GetProperty("page").GetInt32());
            Assert.Equal(25, allBody.GetProperty("pageSize").GetInt32());
            Assert.Equal(
                "Still pending note",
                allBody.GetProperty("items")[0]
                    .GetProperty("comment")
                    .GetString()
            );
            Assert.Equal(
                "Camden Street",
                allBody.GetProperty("items")[0]
                    .GetProperty("locationName")
                    .GetString()
            );
            Assert.Equal(
                "Pending",
                allBody.GetProperty("items")[0]
                    .GetProperty("classificationStatus")
                    .GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                allBody.GetProperty("items")[0]
                    .GetProperty("sentiment")
                    .ValueKind
            );

            using var searchRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&q=pasta&sort=oldest-first"
            );
            searchRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var searchResponse = await _client.SendAsync(searchRequest);
            var searchBody = await ReadJsonAsync(searchResponse);

            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
            Assert.Equal(1, searchBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Love the pasta",
                searchBody.GetProperty("items")[0]
                    .GetProperty("comment")
                    .GetString()
            );

            using var sentimentRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&sentiment=negative&sort=oldest-first"
            );
            sentimentRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var sentimentResponse = await _client.SendAsync(sentimentRequest);
            var sentimentBody = await ReadJsonAsync(sentimentResponse);

            Assert.Equal(HttpStatusCode.OK, sentimentResponse.StatusCode);
            Assert.Equal(1, sentimentBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Slow service tonight",
                sentimentBody.GetProperty("items")[0]
                    .GetProperty("comment")
                    .GetString()
            );

            using var tagsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&detectedTags=WaitTime&detectedTags=FoodQuality&sort=oldest-first"
            );
            tagsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var tagsResponse = await _client.SendAsync(tagsRequest);
            var tagsBody = await ReadJsonAsync(tagsResponse);

            Assert.Equal(HttpStatusCode.OK, tagsResponse.StatusCode);
            Assert.Equal(2, tagsBody.GetProperty("totalCount").GetInt32());
            var tagComments = tagsBody.GetProperty("items")
                .EnumerateArray()
                .Select(i => i.GetProperty("comment").GetString())
                .ToList();
            Assert.Equal(
                ["Love the pasta", "Slow service tonight"],
                tagComments
            );

            using var dateRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + $"&dateFrom={Uri.EscapeDataString(mid.ToString("O"))}"
                    + $"&dateTo={Uri.EscapeDataString(newer.AddSeconds(1).ToString("O"))}"
                    + "&sort=oldest-first"
            );
            dateRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var dateResponse = await _client.SendAsync(dateRequest);
            var dateBody = await ReadJsonAsync(dateResponse);

            Assert.Equal(HttpStatusCode.OK, dateResponse.StatusCode);
            Assert.Equal(2, dateBody.GetProperty("totalCount").GetInt32());

            using var page2Request = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&sort=oldest-first&page=1&pageSize=25"
            );
            page2Request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var page2Response = await _client.SendAsync(page2Request);
            var page2Body = await ReadJsonAsync(page2Response);

            Assert.Equal(HttpStatusCode.OK, page2Response.StatusCode);
            Assert.Equal(3, page2Body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, page2Body.GetProperty("page").GetInt32());
            Assert.Equal(25, page2Body.GetProperty("pageSize").GetInt32());
            Assert.Equal(
                "Love the pasta",
                page2Body.GetProperty("items")[0]
                    .GetProperty("comment")
                    .GetString()
            );
        }

        [Fact]
        public async Task GetGuestFeedbacks_RejectsInvalidPageSizeAndSort()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "fb-list-invalid-params-tk"
            );

            using var pageSizeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&pageSize=10"
            );
            pageSizeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var pageSizeResponse = await _client.SendAsync(pageSizeRequest);
            Assert.Equal(HttpStatusCode.BadRequest, pageSizeResponse.StatusCode);

            using var sortRequest = new HttpRequestMessage(
                HttpMethod.Get,
                FeedbacksUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&sort=guest-name-az"
            );
            sortRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var sortResponse = await _client.SendAsync(sortRequest);
            Assert.Equal(HttpStatusCode.BadRequest, sortResponse.StatusCode);
        }

        private static string FeedbacksUrl(int guestId, int locationId)
            => $"/api/guests/{guestId}/feedbacks?locationId={locationId}";

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string linkToken,
            string email = "fb-list-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Feedbacks List Owner",
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
                Name = "Feedbacks List Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
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
            string ownerEmail = "fb-list-guest-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Feedbacks List Owner",
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
                Name = "Feedbacks List Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "amelia@example.com",
                NormalizedEmail = "amelia@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Amelia Hart",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private sealed record GuestSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId
        );
    }
}
