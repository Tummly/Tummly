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
    public class HomeLatestActivityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeLatestActivityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetHomeLatestActivity_ReturnsMergedItemsSortedByTime()
        {
            var olderJoin = new DateTime(
                2026,
                7,
                10,
                8,
                0,
                0,
                DateTimeKind.Utc
            );
            var feedbackAt = new DateTime(
                2026,
                7,
                12,
                12,
                0,
                0,
                DateTimeKind.Utc
            );
            var newerJoin = new DateTime(
                2026,
                7,
                14,
                9,
                0,
                0,
                DateTimeKind.Utc
            );

            var seeded = await SeedOwnerWithActivityAsync(
                "home-latest-activity-sort-token-1",
                feedbackCreatedAt: feedbackAt,
                extraLocationGuest: ("Pat Join", newerJoin),
                primaryLocationGuestCreatedAt: olderJoin
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LatestActivityUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var items = body.GetProperty("items");
            Assert.Equal(3, items.GetArrayLength());
            Assert.Equal(
                "guest-joined",
                items[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Pat Join",
                items[0].GetProperty("guestName").GetString()
            );
            Assert.Equal(
                "feedback",
                items[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Great food",
                items[1].GetProperty("comment").GetString()
            );
            Assert.Equal(
                seeded.LocationGuestId,
                items[1].GetProperty("locationGuestId").GetInt32()
            );
            Assert.Equal(
                "guest-joined",
                items[2].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Alex Guest",
                items[2].GetProperty("guestName").GetString()
            );
        }

        [Fact]
        public async Task GetHomeLatestActivity_FirstFeedbackYieldsBothJoinAndFeedbackRows()
        {
            var sameInstant = new DateTime(
                2026,
                7,
                12,
                10,
                0,
                0,
                DateTimeKind.Utc
            );

            var seeded = await SeedOwnerWithActivityAsync(
                "home-latest-activity-both-token-1",
                feedbackCreatedAt: sameInstant,
                primaryLocationGuestCreatedAt: sameInstant
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LatestActivityUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items");

            Assert.Equal(2, items.GetArrayLength());
            Assert.Contains(
                items.EnumerateArray(),
                item => item.GetProperty("kind").GetString() == "feedback"
            );
            Assert.Contains(
                items.EnumerateArray(),
                item => item.GetProperty("kind").GetString() == "guest-joined"
            );
        }

        [Fact]
        public async Task GetHomeLatestActivity_GuestJoinedItemsDoNotExposeFeedbackId()
        {
            var seeded = await SeedOwnerWithActivityAsync(
                "home-latest-activity-no-fbid-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LatestActivityUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            var joinItem = body
                .GetProperty("items")
                .EnumerateArray()
                .Single(item =>
                    item.GetProperty("kind").GetString() == "guest-joined"
                );

            Assert.False(joinItem.TryGetProperty("feedbackId", out _));
            Assert.False(joinItem.TryGetProperty("id", out _));
            Assert.True(
                joinItem.TryGetProperty("locationGuestId", out _)
            );
        }

        [Fact]
        public async Task GetFeedback_RemainsFeedbackOnly_NotMergedWithGuestJoins()
        {
            var seeded = await SeedOwnerWithActivityAsync(
                "home-latest-activity-feedback-only-token",
                extraLocationGuest: ("Extra Guest", DateTime.UtcNow)
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback?locationId={seeded.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, body.GetProperty("total").GetInt32());

            var recent = body.GetProperty("recent");
            Assert.Equal(1, recent.GetArrayLength());
            Assert.True(recent[0].TryGetProperty("comment", out _));
            Assert.False(recent[0].TryGetProperty("kind", out _));
        }

        [Fact]
        public async Task GetHomeLatestActivity_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(LatestActivityUrl(1));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetHomeLatestActivity_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithActivityAsync(
                "home-latest-activity-owner-a-token"
            );
            var other = await SeedOwnerWithActivityAsync(
                "home-latest-activity-owner-b-token",
                email: "home-latest-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LatestActivityUrl(other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private static string LatestActivityUrl(int locationId)
        {
            return $"/api/home/latest-activity?locationId={locationId}";
        }

        [Fact]
        public async Task GetHomeLatestActivity_GuestJoinedItemsExposeOffersOptOut()
        {
            var seeded = await SeedOwnerWithActivityAsync(
                "home-latest-activity-opt-out-token",
                primaryLocationGuestOffersOptOut: true,
                extraLocationGuest: ("Pat Join", DateTime.UtcNow)
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LatestActivityUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            var joinItems = body
                .GetProperty("items")
                .EnumerateArray()
                .Where(item =>
                    item.GetProperty("kind").GetString() == "guest-joined"
                )
                .ToList();

            Assert.Equal(2, joinItems.Count);

            var alexJoin = joinItems.Single(item =>
                item.GetProperty("guestName").GetString() == "Alex Guest"
            );
            Assert.Equal("opted_out", alexJoin.GetProperty("marketingPreference").GetString());

            var patJoin = joinItems.Single(item =>
                item.GetProperty("guestName").GetString() == "Pat Join"
            );
            Assert.Equal("allowed", patJoin.GetProperty("marketingPreference").GetString());
        }

        [Fact]
        public async Task GetHomeLatestActivity_IncludesGuestJoinOutsidePerStreamTopFive()
        {
            var seeded = await SeedBusyActivityScenarioAsync(
                "home-latest-activity-global-merge-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LatestActivityUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items");

            Assert.Equal(5, items.GetArrayLength());
            Assert.Contains(
                items.EnumerateArray(),
                item =>
                    item.GetProperty("kind").GetString() == "guest-joined"
                    && item.GetProperty("guestName").GetString() == "Global Fifth"
            );
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedBusyActivityScenarioAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Busy Activity Owner",
                Email = "busy-activity-owner@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900444",
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
                Name = "Busy Activity Venue",
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

            var feedbackTimes = new[]
            {
                new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc),
            };

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "busy@example.com",
                NormalizedEmail = "busy@example.com",
                CreatedAt = DateTime.UtcNow,
            };

            context.MasterGuests.Add(masterGuest);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                RestaurantLocationId = location.Id,
                Name = "Busy Guest",
                CreatedAt = feedbackTimes[4],
            };

            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            for (var index = 0; index < feedbackTimes.Length; index++)
            {
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = location.Id,
                        LocationGuestId = locationGuest.Id,
                        GuestName = $"Feedback Guest {index + 1}",
                        GuestContact = $"guest{index + 1}@example.com",
                        ContactType = ContactType.Email,
                        Comment = $"Feedback {index + 1}",
                        CreatedAt = feedbackTimes[index],
                    }
                );
            }

            var fillerGuestTimes = new[]
            {
                new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
            };

            for (var index = 0; index < fillerGuestTimes.Length; index++)
            {
                var fillerMaster = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = $"filler{index + 1}@example.com",
                    NormalizedEmail = $"filler{index + 1}@example.com",
                    CreatedAt = DateTime.UtcNow,
                };

                context.MasterGuests.Add(fillerMaster);
                await context.SaveChangesAsync();

                context.LocationGuests.Add(
                    new LocationGuest
                    {
                        MasterGuestId = fillerMaster.Id,
                        RestaurantLocationId = location.Id,
                        Name = $"Filler Guest {index + 1}",
                        CreatedAt = fillerGuestTimes[index],
                    }
                );
                await context.SaveChangesAsync();
            }

            var globalFifthMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "global-fifth@example.com",
                NormalizedEmail = "global-fifth@example.com",
                CreatedAt = DateTime.UtcNow,
            };

            context.MasterGuests.Add(globalFifthMaster);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(
                new LocationGuest
                {
                    MasterGuestId = globalFifthMaster.Id,
                    RestaurantLocationId = location.Id,
                    Name = "Global Fifth",
                    CreatedAt = new DateTime(
                        2026,
                        7,
                        16,
                        12,
                        30,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId,
            int LocationGuestId
        )> SeedOwnerWithActivityAsync(
            string linkToken,
            string email = "home-latest-owner@example.com",
            DateTime? feedbackCreatedAt = null,
            DateTime? primaryLocationGuestCreatedAt = null,
            bool primaryLocationGuestOffersOptOut = false,
            (string Name, DateTime CreatedAt)? extraLocationGuest = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Home Latest Owner",
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
                Name = "Home Latest Venue",
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

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "alex@example.com",
                NormalizedEmail = "alex@example.com",
                CreatedAt = DateTime.UtcNow,
            };

            context.MasterGuests.Add(masterGuest);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                RestaurantLocationId = location.Id,
                Name = "Alex Guest",
                MarketingPreference = LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(primaryLocationGuestOffersOptOut),

                CreatedAt = primaryLocationGuestCreatedAt ?? DateTime.UtcNow,
            };

            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            if (extraLocationGuest != null)
            {
                var extraMaster = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = "pat@example.com",
                    NormalizedEmail = "pat@example.com",
                    CreatedAt = DateTime.UtcNow,
                };

                context.MasterGuests.Add(extraMaster);
                await context.SaveChangesAsync();

                context.LocationGuests.Add(
                    new LocationGuest
                    {
                        MasterGuestId = extraMaster.Id,
                        RestaurantLocationId = location.Id,
                        Name = extraLocationGuest.Value.Name,
                        CreatedAt = extraLocationGuest.Value.CreatedAt,
                    }
                );
                await context.SaveChangesAsync();
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest.Id,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = feedbackCreatedAt ?? DateTime.UtcNow,
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, feedback.Id, locationGuest.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
