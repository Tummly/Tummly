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
    public class GuestIdentityPatchEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestIdentityPatchEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PatchAsJsonAsync(
                "/api/guests/1?locationId=1",
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = "amelia@example.com",
                    phone = (string?)null,
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns403_ForNonOwnedLocation()
        {
            var ownerJwt = await SeedOwnerAsync("identity-owner-a-token12xxxx");
            var other = await SeedOwnerWithGuestAsync(
                "identity-owner-b-token12xxxx",
                ownerEmail: "identity-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(other.LocationGuestId, other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerJwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Blocked",
                    lastName = "Name",
                    email = "blocked@example.com",
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns404_ForUnknownGuest()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-unknown-gst-tokenx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(999_999, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Ghost",
                    lastName = "Guest",
                    email = "ghost@example.com",
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task PatchGuestIdentity_UpdatesNameOnThisLocationGuestOnly()
        {
            var seeded = await SeedOwnerWithTwoLocationGuestsAsync(
                "identity-name-local-tokenxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.PrimaryLocationGuestId, seeded.PrimaryLocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = seeded.Email,
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var primary = await context.LocationGuests
                .AsNoTracking()
                .SingleAsync(lg => lg.Id == seeded.PrimaryLocationGuestId);
            var secondary = await context.LocationGuests
                .AsNoTracking()
                .SingleAsync(lg => lg.Id == seeded.SecondaryLocationGuestId);

            Assert.Equal("Amelia Hart", primary.Name);
            Assert.Equal("Other Venue Name", secondary.Name);
        }

        [Fact]
        public async Task PatchGuestIdentity_UpdatesMasterEmailAndPhoneRestaurantWide()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-master-update-tokx",
                email: "before@example.com",
                mobile: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Notes",
                    lastName = "Guest",
                    email = "After@Example.com",
                    phone = "07400111222",
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = await context.MasterGuests
                .AsNoTracking()
                .SingleAsync(m => m.Id == seeded.MasterGuestId);

            Assert.Equal("After@Example.com", master.Email);
            Assert.Equal("after@example.com", master.NormalizedEmail);
            Assert.Equal("07400111222", master.Mobile);
            Assert.Equal("07400111222", master.NormalizedPhone);
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns400_WhenBothContactsCleared()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-no-contact-tokenxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = "",
                    phone = "",
                }
            );

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "contact",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns400_ForInvalidEmail()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-bad-email-tokenxxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = "not-an-email",
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns400_ForInvalidUkPhone()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-bad-phone-tokenxxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = "amelia@example.com",
                    phone = "12345",
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns400_WhenJoinedNameEmpty()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-empty-name-tokenxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "  ",
                    lastName = "",
                    email = "amelia@example.com",
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns409_OnEmailCollision_WithoutMerge()
        {
            var seeded = await SeedOwnerWithGuestAndColliderAsync(
                "identity-email-collide-tokx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Target",
                    lastName = "Guest",
                    email = seeded.ColliderEmail,
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.False(body.GetProperty("success").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var target = await context.MasterGuests
                .AsNoTracking()
                .SingleAsync(m => m.Id == seeded.MasterGuestId);
            var collider = await context.MasterGuests
                .AsNoTracking()
                .SingleAsync(m => m.Id == seeded.ColliderMasterGuestId);

            Assert.Equal(seeded.OriginalEmail, target.Email);
            Assert.Equal(seeded.ColliderEmail, collider.Email);
            Assert.Equal(
                seeded.MasterGuestId,
                (
                    await context.LocationGuests
                        .AsNoTracking()
                        .SingleAsync(lg => lg.Id == seeded.LocationGuestId)
                ).MasterGuestId
            );
        }

        [Fact]
        public async Task PatchGuestIdentity_Returns409_OnPhoneCollision_WithoutMerge()
        {
            var seeded = await SeedOwnerWithPhoneColliderAsync(
                "identity-phone-collide-tokx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Target",
                    lastName = "Guest",
                    email = (string?)null,
                    phone = seeded.ColliderPhone,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var target = await context.MasterGuests
                .AsNoTracking()
                .SingleAsync(m => m.Id == seeded.MasterGuestId);

            Assert.Equal(seeded.OriginalPhone, target.Mobile);
        }

        [Fact]
        public async Task PatchGuestIdentity_ClearsOneContactChannel_WhenOtherRemains()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-clear-one-channelxx",
                email: "keep-or-clear@example.com",
                mobile: "07400999888"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Notes",
                    lastName = "Guest",
                    email = "keep-or-clear@example.com",
                    phone = "",
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = await context.MasterGuests
                .AsNoTracking()
                .SingleAsync(m => m.Id == seeded.MasterGuestId);

            Assert.Equal("keep-or-clear@example.com", master.Email);
            Assert.Null(master.Mobile);
            Assert.Null(master.NormalizedPhone);

            var events = await context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.Kind == LocationGuestActivityKinds.ProfileEdited
                )
                .ToListAsync();

            Assert.Single(events);
            var payload = LocationGuestActivityPayload.Deserialize(
                events[0].PayloadJson
            );
            Assert.Contains("phone", payload!.ChangedFields!);
        }

        [Fact]
        public async Task PatchGuestIdentity_EmitsProfileEdited_WhenFieldsChange()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-emit-change-tokenx",
                email: "before-emit@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = "after-emit@example.com",
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var events = await context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.Kind == LocationGuestActivityKinds.ProfileEdited
                )
                .ToListAsync();

            Assert.Single(events);
            var payload = LocationGuestActivityPayload.Deserialize(
                events[0].PayloadJson
            );
            Assert.NotNull(payload?.ChangedFields);
            Assert.Contains("name", payload!.ChangedFields!);
            Assert.Contains("email", payload.ChangedFields!);
            Assert.DoesNotContain("phone", payload.ChangedFields!);
        }

        [Fact]
        public async Task PatchGuestIdentity_NoopSave_DoesNotEmitProfileEdited()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "identity-noop-save-tokenxxx",
                guestName: "Amelia Hart",
                email: "noop@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                GuestUrl(seeded.LocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(
                new
                {
                    firstName = "Amelia",
                    lastName = "Hart",
                    email = "noop@example.com",
                    phone = (string?)null,
                }
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var count = await context.LocationGuestActivityEvents
                .AsNoTracking()
                .CountAsync(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.Kind == LocationGuestActivityKinds.ProfileEdited
                );

            Assert.Equal(0, count);
        }

        private static string GuestUrl(int guestId, int locationId)
            => $"/api/guests/{guestId}?locationId={locationId}";

        private async Task<string> SeedOwnerAsync(
            string linkToken,
            string ownerEmail = "identity-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Identity Owner",
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
                Name = "Identity Venue",
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

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private async Task<GuestSeed> SeedOwnerWithGuestAsync(
            string linkToken,
            string ownerEmail = "identity-guest-owner@example.com",
            string guestName = "Notes Guest",
            string? email = null,
            string? mobile = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Identity Owner",
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
                Name = "Identity Venue",
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

            var resolvedEmail = email ?? $"{linkToken}@example.com";
            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = resolvedEmail,
                NormalizedEmail = resolvedEmail.Trim().ToLowerInvariant(),
                Mobile = mobile,
                NormalizedPhone = mobile == null
                    ? null
                    : new string(mobile.Where(char.IsDigit).ToArray()),
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = guestName,
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

            return new GuestSeed(
                jwt,
                location.Id,
                locationGuest.Id,
                master.Id,
                restaurant.Id,
                resolvedEmail
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
                FullName = "Identity Owner",
                Email = "identity-two-loc@example.com",
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
                Name = "Identity Multi Venue",
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

            var email = $"{linkToken}@example.com";
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
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            var secondaryGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = secondary.Id,
                Name = "Other Venue Name",
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

            return new TwoLocationSeed(
                jwt,
                primary.Id,
                primaryGuest.Id,
                secondaryGuest.Id,
                email
            );
        }

        private async Task<CollisionSeed> SeedOwnerWithGuestAndColliderAsync(
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
                FullName = "Identity Owner",
                Email = "identity-collide@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900125",
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
                Name = "Identity Venue",
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

            var originalEmail = $"{linkToken}-target@example.com";
            var colliderEmail = $"{linkToken}-collider@example.com";

            var targetMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = originalEmail,
                NormalizedEmail = originalEmail,
                CreatedAt = DateTime.UtcNow,
            };
            var colliderMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = colliderEmail,
                NormalizedEmail = colliderEmail,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(targetMaster, colliderMaster);
            await context.SaveChangesAsync();

            var targetGuest = new LocationGuest
            {
                MasterGuestId = targetMaster.Id,
                RestaurantLocationId = location.Id,
                Name = "Target Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            var colliderGuest = new LocationGuest
            {
                MasterGuestId = colliderMaster.Id,
                RestaurantLocationId = location.Id,
                Name = "Collider Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.AddRange(targetGuest, colliderGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new CollisionSeed(
                jwt,
                location.Id,
                targetGuest.Id,
                targetMaster.Id,
                colliderMaster.Id,
                originalEmail,
                colliderEmail
            );
        }

        private async Task<PhoneCollisionSeed> SeedOwnerWithPhoneColliderAsync(
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
                FullName = "Identity Owner",
                Email = "identity-phone-collide@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900126",
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
                Name = "Identity Venue",
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

            const string originalPhone = "07400111000";
            const string colliderPhone = "07400222000";

            var targetMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = originalPhone,
                NormalizedPhone = originalPhone,
                CreatedAt = DateTime.UtcNow,
            };
            var colliderMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = colliderPhone,
                NormalizedPhone = colliderPhone,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(targetMaster, colliderMaster);
            await context.SaveChangesAsync();

            var targetGuest = new LocationGuest
            {
                MasterGuestId = targetMaster.Id,
                RestaurantLocationId = location.Id,
                Name = "Target Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            var colliderGuest = new LocationGuest
            {
                MasterGuestId = colliderMaster.Id,
                RestaurantLocationId = location.Id,
                Name = "Collider Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.AddRange(targetGuest, colliderGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new PhoneCollisionSeed(
                jwt,
                location.Id,
                targetGuest.Id,
                targetMaster.Id,
                originalPhone,
                colliderPhone
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

        private sealed record GuestSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId,
            int MasterGuestId,
            int RestaurantId,
            string Email
        );

        private sealed record TwoLocationSeed(
            string Jwt,
            int PrimaryLocationId,
            int PrimaryLocationGuestId,
            int SecondaryLocationGuestId,
            string Email
        );

        private sealed record CollisionSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId,
            int MasterGuestId,
            int ColliderMasterGuestId,
            string OriginalEmail,
            string ColliderEmail
        );

        private sealed record PhoneCollisionSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId,
            int MasterGuestId,
            string OriginalPhone,
            string ColliderPhone
        );
    }
}
