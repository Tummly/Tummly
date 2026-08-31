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
    public class PrivacyConsentSaveEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PrivacyConsentSaveEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Save_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PutAsJsonAsync(
                "/api/privacy-consent",
                new
                {
                    smsConsentWording = "SMS copy",
                    emailConsentWording = "Email copy",
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Save_Returns403_WhenLocationsManageButPrivacyConsentIsNoAccess()
        {
            var seeded = await SeedAdminWithPrivacyNoAccessAsync();

            using var request = AuthorizedPut(
                "/api/privacy-consent",
                seeded.AdminJwt,
                new
                {
                    smsConsentWording = "SMS copy",
                    emailConsentWording = "Email copy",
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Save_SetsReady_PersistsWording_EmitsActivities_AndClearsPrivacyAttention()
        {
            var seeded = await SeedOwnerWithIncompletePrivacyAsync();

            using var beforeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            beforeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var beforeResponse = await _client.SendAsync(beforeRequest);
            Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
            var beforeBody = await ReadJsonAsync(beforeResponse);
            var beforeAttention = beforeBody.GetProperty("attentionItems");
            Assert.Contains(
                beforeAttention.EnumerateArray(),
                item =>
                    item.GetProperty("id").GetString() == "privacy-review"
            );
            var privacyItem = beforeAttention
                .EnumerateArray()
                .Single(item =>
                    item.GetProperty("id").GetString() == "privacy-review"
                );
            Assert.Contains(
                seeded.ActiveLocationId,
                privacyItem
                    .GetProperty("locationIds")
                    .EnumerateArray()
                    .Select(x => x.GetInt32())
            );

            using var saveRequest = AuthorizedPut(
                "/api/privacy-consent",
                seeded.OwnerJwt,
                new
                {
                    smsConsentWording = "We may text you offers.",
                    emailConsentWording = "We may email you offers.",
                }
            );
            var saveResponse = await _client.SendAsync(saveRequest);
            Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
            var saveBody = await ReadJsonAsync(saveResponse);
            Assert.True(saveBody.GetProperty("success").GetBoolean());
            Assert.True(saveBody.GetProperty("privacyReady").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.RestaurantId);
            Assert.NotNull(restaurant.PrivacyConsentReadyAt);
            Assert.Equal(
                "We may text you offers.",
                restaurant.SmsConsentWording
            );
            Assert.Equal(
                "We may email you offers.",
                restaurant.EmailConsentWording
            );

            var activities = await context.LocationActivities
                .AsNoTracking()
                .Where(row => row.RestaurantId == seeded.RestaurantId)
                .OrderBy(row => row.Kind)
                .ToListAsync();
            Assert.Contains(
                activities,
                row =>
                    row.Kind == LocationActivityKinds.ConsentCopyChanged
                    && row.LocationId == null
            );
            Assert.Contains(
                activities,
                row =>
                    row.Kind == LocationActivityKinds.PrivacyReviewCompleted
                    && row.LocationId == null
            );

            using var afterRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            afterRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var afterResponse = await _client.SendAsync(afterRequest);
            var afterBody = await ReadJsonAsync(afterResponse);
            var afterAttention = afterBody.GetProperty("attentionItems");
            Assert.DoesNotContain(
                afterAttention.EnumerateArray(),
                item =>
                    item.GetProperty("id").GetString() == "privacy-review"
            );
        }

        private async Task<(
            string AdminJwt,
            int RestaurantId
        )> SeedAdminWithPrivacyNoAccessAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Privacy Gate Owner",
                Email = $"priv-gate-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var admin = new User
            {
                FullName = "Privacy Gate Admin",
                Email = $"priv-gate-admin-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900112",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Privacy Gate Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;
            admin.SelectedRestaurantId = restaurant.Id;

            context.RestaurantMemberships.AddRange(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                },
                new RestaurantMembership
                {
                    UserId = admin.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Admin,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            // Locations stays Manage (Admin default); privacy-consent blocked.
            context.RestaurantAdminPermissionCells.Add(
                new RestaurantAdminPermissionCell
                {
                    RestaurantId = restaurant.Id,
                    AreaId = OperatorAreaIds.PrivacyConsent,
                    Level = PermissionLevel.NoAccess,
                }
            );
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateToken(
                    admin.Id.ToString(),
                    admin.Email,
                    admin.Role
                ),
                restaurant.Id
            );
        }

        private async Task<(
            string OwnerJwt,
            int RestaurantId,
            int ActiveLocationId
        )> SeedOwnerWithIncompletePrivacyAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "James",
                Email = $"priv-save-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Privacy Save Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                // New restaurants stay incomplete until save.
                PrivacyConsentReadyAt = null,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            var active = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Active Camden",
                Address = "1 High Street",
                City = "Camden",
                Postcode = "NW1 1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Paused Soho",
                Address = "2 High Street",
                City = "Soho",
                Postcode = "W1D 1AA",
                LifecycleStatus = LocationLifecycleStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(active, paused);
            await context.SaveChangesAsync();

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = active.Id,
                    QrType = QrType.CounterCard,
                    Token = Guid.NewGuid().ToString("N")[..16],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = paused.Id,
                    QrType = QrType.CounterCard,
                    Token = Guid.NewGuid().ToString("N")[..16],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                restaurant.Id,
                active.Id
            );
        }

        private static HttpRequestMessage AuthorizedPut(
            string path,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
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
