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
    public class GuestMarketingPreferencePatchEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestMarketingPreferencePatchEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PatchMarketingPreference_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PatchAsJsonAsync(
                "/api/guests/1/marketing-preference?locationId=1",
                new { preference = "opted_out" }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PatchMarketingPreference_Returns403_ForNonOwnedLocation()
        {
            var ownerJwt = await SeedOwnerJwtAsync(
                "mkt-owner-a-token12xxxxxxxx",
                ownerEmail: "mkt-owner-a@example.com"
            );
            var other = await SeedOwnerWithGuestAsync(
                "mkt-owner-b-token12xxxxxxxx",
                ownerEmail: "mkt-owner-b@example.com"
            );

            var response = await SendPatchAsync(
                other.LocationGuestId,
                other.LocationId,
                ownerJwt,
                new { preference = "opted_out" }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PatchMarketingPreference_Returns404_ForUnknownGuest()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-unknown-gst-tokenxxxxxx"
            );

            var response = await SendPatchAsync(
                999_999,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_PersistsAllowedToOptedOut()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-allowed-to-out-tokenxxx",
                preference: LocationGuestMarketingPreference.Allowed
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "opted_out",
                body.GetProperty("preference").GetString()
            );
            Assert.True(body.GetProperty("preferenceChanged").GetBoolean());
            Assert.False(body.GetProperty("noteCreated").GetBoolean());

            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_PersistsAllowedToNotRecorded()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-allowed-to-nr-tokenxxxx",
                preference: LocationGuestMarketingPreference.Allowed
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "not_recorded" }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                LocationGuestMarketingPreference.NotRecorded,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_PersistsOptedOutToNotRecorded()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-out-to-nr-tokenxxxxxxxx",
                preference: LocationGuestMarketingPreference.OptedOut
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "not_recorded" }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                LocationGuestMarketingPreference.NotRecorded,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_PersistsNotRecordedToOptedOut()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-nr-to-out-tokenxxxxxxxx",
                preference: LocationGuestMarketingPreference.NotRecorded
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_KeepsAllowed_AsNoOp()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-keep-allowed-tokenxxxxx",
                preference: LocationGuestMarketingPreference.Allowed
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "allowed" }
            );
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(body.GetProperty("preferenceChanged").GetBoolean());
            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
            Assert.Equal(
                0,
                await CountActivityAsync(
                    seeded.LocationGuestId,
                    LocationGuestActivityKinds.MarketingPreferenceChanged
                )
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_RejectsOptedOutToAllowed()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-reject-out-allowed-tokx",
                preference: LocationGuestMarketingPreference.OptedOut
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "allowed" }
            );
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "allowed",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_RejectsNotRecordedToAllowed()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-reject-nr-allowed-tokxx",
                preference: LocationGuestMarketingPreference.NotRecorded
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "allowed" }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                LocationGuestMarketingPreference.NotRecorded,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_WritesActivity_WhenPreferenceChanges()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-activity-change-tokenxx",
                preference: LocationGuestMarketingPreference.Allowed,
                ownerFullName: "Ada Operator"
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = await context.LocationGuestActivityEvents
                .AsNoTracking()
                .SingleAsync(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.Kind
                        == LocationGuestActivityKinds.MarketingPreferenceChanged
                );
            var payload = LocationGuestActivityPayload.Deserialize(row.PayloadJson);

            Assert.Equal("allowed", payload!.FromPreference);
            Assert.Equal("opted_out", payload.ToPreference);
            Assert.Equal("Ada Operator", payload.AuthorDisplayName);
            Assert.True(row.OccurredAt > DateTime.MinValue);
        }

        [Fact]
        public async Task PatchMarketingPreference_AppendsGuestMarketingUnsubscribed_LocationActivity()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-loc-act-unsub-tokenxxxx",
                preference: LocationGuestMarketingPreference.Allowed,
                ownerFullName: "Ada Operator"
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.LocationId);
            var activity = await context.LocationActivities
                .AsNoTracking()
                .SingleAsync(row =>
                    row.RestaurantId == location.RestaurantId
                    && row.Kind
                        == LocationActivityKinds.GuestMarketingUnsubscribed
                );

            Assert.Equal(seeded.LocationId, activity.LocationId);
            Assert.True(activity.ActorUserId > 0);
            Assert.Equal("Ada Operator", activity.ActorDisplayName);
        }

        [Fact]
        public async Task PatchMarketingPreference_CreatesNote_WhenNoteNonEmpty()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-create-note-tokenxxxxxx",
                preference: LocationGuestMarketingPreference.Allowed,
                ownerFullName: "Ada Operator"
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out", note = "  Guest asked to stop.  " }
            );
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.GetProperty("noteCreated").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var note = await context.LocationGuestNotes
                .AsNoTracking()
                .SingleAsync(n => n.LocationGuestId == seeded.LocationGuestId);

            Assert.Equal("Guest asked to stop.", note.Body);
            Assert.Equal("Ada Operator", note.AuthorDisplayName);
            Assert.Equal(
                1,
                await context.LocationGuestActivityEvents.CountAsync(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.Kind == LocationGuestActivityKinds.NoteAdded
                )
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_SkipsNote_WhenEmptyOrOmitted()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-skip-empty-note-tokenxx",
                preference: LocationGuestMarketingPreference.Allowed
            );

            var omitted = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );
            Assert.Equal(HttpStatusCode.OK, omitted.StatusCode);

            var empty = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "not_recorded", note = "   " }
            );
            var body = await ReadJsonAsync(empty);

            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            Assert.False(body.GetProperty("noteCreated").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.LocationGuestNotes.CountAsync(n =>
                    n.LocationGuestId == seeded.LocationGuestId
                )
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_NoteOnly_UsesNoteAddedActivityOnly()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "mkt-note-only-tokenxxxxxxxx",
                preference: LocationGuestMarketingPreference.OptedOut
            );

            var response = await SendPatchAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.Jwt,
                new { preference = "opted_out", note = "Followed up by phone." }
            );
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(body.GetProperty("preferenceChanged").GetBoolean());
            Assert.True(body.GetProperty("noteCreated").GetBoolean());
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                await ReadPreferenceAsync(seeded.LocationGuestId)
            );
            Assert.Equal(
                0,
                await CountActivityAsync(
                    seeded.LocationGuestId,
                    LocationGuestActivityKinds.MarketingPreferenceChanged
                )
            );
            Assert.Equal(
                1,
                await CountActivityAsync(
                    seeded.LocationGuestId,
                    LocationGuestActivityKinds.NoteAdded
                )
            );
        }

        [Fact]
        public async Task PatchMarketingPreference_DoesNotChangeOtherLocationGuest()
        {
            var seeded = await SeedOwnerWithTwoLocationGuestsAsync(
                "mkt-two-loc-tokenxxxxxxxxxx"
            );

            var response = await SendPatchAsync(
                seeded.PrimaryLocationGuestId,
                seeded.PrimaryLocationId,
                seeded.Jwt,
                new { preference = "opted_out" }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                await ReadPreferenceAsync(seeded.PrimaryLocationGuestId)
            );
            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                await ReadPreferenceAsync(seeded.SecondaryLocationGuestId)
            );
        }

        private async Task<HttpResponseMessage> SendPatchAsync(
            int guestId,
            int locationId,
            string jwt,
            object body
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/guests/{guestId}/marketing-preference?locationId={locationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(body);
            return await _client.SendAsync(request);
        }

        private async Task<LocationGuestMarketingPreference> ReadPreferenceAsync(
            int locationGuestId
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var guest = await context.LocationGuests
                .AsNoTracking()
                .SingleAsync(lg => lg.Id == locationGuestId);
            return guest.MarketingPreference;
        }

        private async Task<int> CountActivityAsync(int locationGuestId, string kind)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            return await context.LocationGuestActivityEvents
                .AsNoTracking()
                .CountAsync(e =>
                    e.LocationGuestId == locationGuestId && e.Kind == kind
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

        private async Task<string> SeedOwnerJwtAsync(
            string linkToken,
            string ownerEmail
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Marketing Owner",
                Email = ownerEmail,
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
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
                Name = "Marketing Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Camden Street",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private async Task<GuestSeed> SeedOwnerWithGuestAsync(
            string linkToken,
            string ownerEmail = "mkt-guest-owner@example.com",
            string ownerFullName = "Marketing Owner",
            LocationGuestMarketingPreference preference =
                LocationGuestMarketingPreference.Allowed
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = ownerFullName,
                Email = ownerEmail.Contains(linkToken, StringComparison.Ordinal)
                    ? ownerEmail
                    : $"{linkToken}@example.com",
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
                Name = "Marketing Venue",
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

            var email = $"{linkToken}-guest@example.com";
            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = email,
                NormalizedEmail = email,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Notes Guest",
                MarketingPreference = preference,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            return new GuestSeed(
                jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.Email,
                    user.Role
                ),
                location.Id,
                locationGuest.Id
            );
        }

        private async Task<TwoLocationSeed> SeedOwnerWithTwoLocationGuestsAsync(
            string linkToken
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Marketing Owner",
                Email = $"{linkToken}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900124",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Marketing Multi Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var primary = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var secondary = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho Street",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(primary, secondary);
            await context.SaveChangesAsync();

            var email = $"{linkToken}-guest@example.com";
            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = email,
                NormalizedEmail = email,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var primaryGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = primary.Id,
                Name = "Primary Name",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            var secondaryGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = secondary.Id,
                Name = "Other Venue Name",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.AddRange(primaryGuest, secondaryGuest);
            await context.SaveChangesAsync();

            return new TwoLocationSeed(
                jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.Email,
                    user.Role
                ),
                primary.Id,
                primaryGuest.Id,
                secondaryGuest.Id
            );
        }

        private sealed record GuestSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId
        );

        private sealed record TwoLocationSeed(
            string Jwt,
            int PrimaryLocationId,
            int PrimaryLocationGuestId,
            int SecondaryLocationGuestId
        );
    }
}
