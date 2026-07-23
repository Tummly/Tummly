using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestDeleteEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestDeleteEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DeleteGuest_Returns401_WhenUnauthenticated()
        {
            var response = await _client.DeleteAsync(
                "/api/guests/1?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteGuest_Returns403_ForNonOwnedLocation()
        {
            var ownerJwt = await SeedOwnerJwtAsync(
                "delete-owner-a-token12xxxxx"
            );
            var other = await SeedGuestWithLinkedDataAsync(
                "delete-owner-b-token12xxxxx",
                ownerEmail: "delete-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(other.LocationGuestId, other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteGuest_Returns404_WhenGuestMissingOrWrongLocation()
        {
            var seeded = await SeedGuestWithLinkedDataAsync(
                "delete-missing-gst-tokenxx"
            );

            using var missingRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(999_999, seeded.LocationId)
            );
            missingRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var missingResponse = await _client.SendAsync(missingRequest);
            var missingBody = await ReadJsonAsync(missingResponse);

            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
            Assert.Equal(
                "Guest not found.",
                missingBody.GetProperty("message").GetString()
            );

            using var wrongLocationRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(seeded.LocationGuestId, seeded.OtherLocationId)
            );
            wrongLocationRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var wrongLocationResponse =
                await _client.SendAsync(wrongLocationRequest);
            var wrongLocationBody = await ReadJsonAsync(wrongLocationResponse);

            Assert.Equal(
                HttpStatusCode.NotFound,
                wrongLocationResponse.StatusCode
            );
            Assert.Equal(
                "Guest not found.",
                wrongLocationBody.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task DeleteGuest_HardDeletesWithCascadeSetNullAndOrphanMaster()
        {
            var seeded = await SeedGuestWithLinkedDataAsync(
                "delete-cascade-token12xxxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            Assert.False(
                await context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );
            Assert.False(
                await context.LocationGuestNotes.AnyAsync(
                    n => n.Id == seeded.NoteId
                )
            );
            Assert.False(
                await context.LocationGuestTags.AnyAsync(
                    m =>
                        m.LocationGuestId == seeded.LocationGuestId
                        && m.GuestTagId == seeded.TagId
                )
            );
            Assert.False(
                await context.LocationGuestActivityEvents.AnyAsync(
                    e => e.Id == seeded.ActivityEventId
                )
            );

            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Null(feedback.LocationGuestId);

            Assert.False(
                await context.MasterGuests.AnyAsync(
                    m => m.Id == seeded.MasterGuestId
                )
            );
        }

        [Fact]
        public async Task DeleteGuest_LeavesMasterWhenOtherLocationGuestsRemain()
        {
            var seeded = await SeedGuestSharedAcrossLocationsAsync(
                "delete-keep-master-tokenxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(seeded.PrimaryLocationGuestId, seeded.PrimaryLocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            Assert.False(
                await context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.PrimaryLocationGuestId
                )
            );
            Assert.True(
                await context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.SecondaryLocationGuestId
                )
            );
            Assert.True(
                await context.MasterGuests.AnyAsync(
                    m => m.Id == seeded.MasterGuestId
                )
            );
        }

        [Fact]
        public async Task DeleteGuest_RepeatDelete_Returns404()
        {
            var seeded = await SeedGuestWithLinkedDataAsync(
                "delete-repeat-token12xxxxx"
            );

            using var firstRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            firstRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var firstResponse = await _client.SendAsync(firstRequest);
            Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

            using var secondRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            secondRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var secondResponse = await _client.SendAsync(secondRequest);
            var body = await ReadJsonAsync(secondResponse);

            Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        private static string GuestUrl(int guestId, int locationId)
        {
            return $"/api/guests/{guestId}?locationId={locationId}";
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        private async Task<string> SeedOwnerJwtAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Delete Owner A",
                Email = "delete-owner-a@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900101",
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
                Name = "Delete Venue A",
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
                    LinkToken = linkToken,
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

        private async Task<LinkedGuestSeed> SeedGuestWithLinkedDataAsync(
            string linkToken,
            string ownerEmail = "delete-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Delete Owner",
                Email = ownerEmail,
                PasswordHash = "hash",
                PhoneNumber = "07700900102",
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
                Name = "Delete Venue",
                AccountType = "Multi",
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
                LocationName = "Soho Street",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(primary, secondary);
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
                RestaurantLocationId = primary.Id,
                Name = "Delete Target",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var note = new LocationGuestNote
            {
                LocationGuestId = locationGuest.Id,
                Body = "Private note to cascade",
                AuthorUserId = user.Id,
                AuthorDisplayName = user.FullName,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuestNotes.Add(note);

            var tag = new GuestTag
            {
                RestaurantId = restaurant.Id,
                DisplayName = "VIP",
                NormalizedName = "vip",
                AiSourced = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.GuestTags.Add(tag);
            await context.SaveChangesAsync();

            context.LocationGuestTags.Add(
                new LocationGuestTag
                {
                    LocationGuestId = locationGuest.Id,
                    GuestTagId = tag.Id,
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var feedback = new Feedback
            {
                RestaurantLocationId = primary.Id,
                LocationGuestId = locationGuest.Id,
                GuestName = "Delete Target",
                GuestContact = $"{linkToken}@example.com",
                ContactType = ContactType.Email,
                Comment = "Keep me, unlink me",
                CreatedAt = DateTime.UtcNow,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var activity = new LocationGuestActivityEvent
            {
                LocationGuestId = locationGuest.Id,
                Kind = LocationGuestActivityKinds.NoteAdded,
                PayloadJson = """{"noteId":1}""",
                OccurredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuestActivityEvents.Add(activity);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new LinkedGuestSeed(
                jwt,
                primary.Id,
                secondary.Id,
                locationGuest.Id,
                master.Id,
                note.Id,
                tag.Id,
                feedback.Id,
                activity.Id
            );
        }

        private async Task<SharedMasterSeed> SeedGuestSharedAcrossLocationsAsync(
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
                FullName = "Delete Multi Owner",
                Email = "delete-multi@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900103",
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
                Name = "Delete Multi Venue",
                AccountType = "Multi",
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
                LocationName = "Soho Street",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(primary, secondary);
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

            var primaryGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = primary.Id,
                Name = "Primary Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            var secondaryGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = secondary.Id,
                Name = "Secondary Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.AddRange(primaryGuest, secondaryGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new SharedMasterSeed(
                jwt,
                primary.Id,
                primaryGuest.Id,
                secondaryGuest.Id,
                master.Id
            );
        }

        private sealed record LinkedGuestSeed(
            string Jwt,
            int LocationId,
            int OtherLocationId,
            int LocationGuestId,
            int MasterGuestId,
            int NoteId,
            int TagId,
            int FeedbackId,
            int ActivityEventId
        );

        private sealed record SharedMasterSeed(
            string Jwt,
            int PrimaryLocationId,
            int PrimaryLocationGuestId,
            int SecondaryLocationGuestId,
            int MasterGuestId
        );
    }
}
