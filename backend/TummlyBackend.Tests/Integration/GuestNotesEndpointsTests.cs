using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestNotesEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestNotesEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostGuestNote_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsJsonAsync(
                "/api/guests/1/notes?locationId=1",
                new { body = "Hello" }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestNotes_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/guests/1/notes?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostGuestNote_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("notes-owner-a-token12xxxx");
            var other = await SeedOwnerWithGuestAsync(
                "notes-owner-b-token12xxxx",
                ownerEmail: "notes-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                NotesUrl(other.LocationGuestId, other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new { body = "Blocked note" });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestNotes_Returns404_ForUnknownGuest()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "notes-unknown-gst-tokenxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                NotesUrl(999_999, seeded.LocationId)
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
        public async Task PostGuestNote_CreatesAndListsNewestFirst()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "notes-create-list-tokenxx"
            );

            using var firstRequest = new HttpRequestMessage(
                HttpMethod.Post,
                NotesUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            firstRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            firstRequest.Content = JsonContent.Create(
                new { body = "  Older note body  " }
            );

            var firstResponse = await _client.SendAsync(firstRequest);
            var firstBody = await ReadJsonAsync(firstResponse);

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.True(firstBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Older note body",
                firstBody.GetProperty("note").GetProperty("body").GetString()
            );
            Assert.Equal(
                "Notes Owner",
                firstBody.GetProperty("note")
                    .GetProperty("authorDisplayName")
                    .GetString()
            );
            Assert.True(
                firstBody.GetProperty("note").GetProperty("id").GetInt32() > 0
            );

            using var secondRequest = new HttpRequestMessage(
                HttpMethod.Post,
                NotesUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            secondRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            secondRequest.Content = JsonContent.Create(
                new { body = "Newer note body" }
            );

            var secondResponse = await _client.SendAsync(secondRequest);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                NotesUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var listResponse = await _client.SendAsync(listRequest);
            var listBody = await ReadJsonAsync(listResponse);

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.Equal(2, listBody.GetProperty("totalCount").GetInt32());

            var items = listBody.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal("Newer note body", items[0].GetProperty("body").GetString());
            Assert.Equal("Older note body", items[1].GetProperty("body").GetString());
            Assert.Equal(
                "Notes Owner",
                items[0].GetProperty("authorDisplayName").GetString()
            );
        }

        [Fact]
        public async Task PostGuestNote_RejectsWhitespaceOnlyBody()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "notes-whitespace-tokenxxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                NotesUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new { body = "   " });

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "Note body is required.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task PostGuestNote_EmitsNoteAddedOnActivityList()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "notes-emit-activity-tokenx"
            );

            using var createRequest = new HttpRequestMessage(
                HttpMethod.Post,
                NotesUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            createRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            createRequest.Content = JsonContent.Create(
                new { body = "Activity side-effect note" }
            );

            var createResponse = await _client.SendAsync(createRequest);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            using var activityRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests/{seeded.LocationGuestId}/activity?locationId={seeded.LocationId}&type=note"
            );
            activityRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var activityResponse = await _client.SendAsync(activityRequest);
            var activityBody = await ReadJsonAsync(activityResponse);

            Assert.Equal(HttpStatusCode.OK, activityResponse.StatusCode);
            Assert.Equal(1, activityBody.GetProperty("totalCount").GetInt32());

            var item = activityBody.GetProperty("items")[0];
            Assert.Equal(
                LocationGuestActivityKinds.NoteAdded,
                item.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Notes Owner",
                item.GetProperty("authorDisplayName").GetString()
            );
        }

        [Fact]
        public async Task GetGuestNotes_RespectsLimitCap()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "notes-limit-cap-tokenxxxx"
            );

            for (var i = 0; i < 4; i++)
            {
                using var createRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    NotesUrl(seeded.LocationGuestId, seeded.LocationId)
                );
                createRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                createRequest.Content = JsonContent.Create(
                    new { body = $"Note number {i}" }
                );
                var createResponse = await _client.SendAsync(createRequest);
                Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            }

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                NotesUrl(seeded.LocationGuestId, seeded.LocationId) + "&limit=2"
            );
            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var listResponse = await _client.SendAsync(listRequest);
            var listBody = await ReadJsonAsync(listResponse);

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.Equal(4, listBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, listBody.GetProperty("items").GetArrayLength());
            Assert.Equal(
                "Note number 3",
                listBody.GetProperty("items")[0].GetProperty("body").GetString()
            );
        }

        private static string NotesUrl(int guestId, int locationId)
        {
            return $"/api/guests/{guestId}/notes?locationId={locationId}";
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string linkToken,
            string email = "notes-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Notes Owner",
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
                Name = "Notes Venue",
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
            string ownerEmail = "notes-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Notes Owner",
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
                Name = "Notes Venue",
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
                Email = $"{linkToken}@example.com",
                NormalizedEmail = $"{linkToken}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Notes Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id, user.Id);
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
            int LocationGuestId,
            int UserId
        );
    }
}
