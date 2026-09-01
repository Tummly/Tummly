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
    public class PrivacyConsentReadWriteEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PrivacyConsentReadWriteEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/privacy-consent");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_Returns403_WhenPrivacyConsentIsNoAccess()
        {
            var seeded = await SeedAdminWithPrivacyNoAccessAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsSetupRows_Toggles_Wording_AndReady()
        {
            var seeded = await SeedOwnerWithPrivacyStateAsync(
                emailEnabled: true,
                smsEnabled: false,
                feedbackEnabled: true,
                smsWording: null,
                emailWording: "We may email you.",
                readyAt: null
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("privacyReady").GetBoolean());
            Assert.True(
                body.TryGetProperty("privacyConsentReadyAt", out var readyAt)
                && readyAt.ValueKind == JsonValueKind.Null
            );
            Assert.Equal(
                "We may email you.",
                body.GetProperty("emailConsentWording").GetString()
            );
            Assert.Equal(
                string.Empty,
                body.GetProperty("smsConsentWording").GetString()
            );
            Assert.True(
                body.GetProperty("emailMarketingPermissionEnabled").GetBoolean()
            );
            Assert.False(
                body.GetProperty("smsMarketingPermissionEnabled").GetBoolean()
            );
            Assert.True(
                body.GetProperty("feedbackFollowUpPermissionEnabled")
                    .GetBoolean()
            );

            var rows = body.GetProperty("privacySetupRows").EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("id").GetString()!,
                    row => row.GetProperty("status").GetString()
                );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusConfigured,
                rows["privacy-notice"]
            );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusConfigured,
                rows["guest-permission-wording"]
            );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusEnabled,
                rows["email-marketing"]
            );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusNotUsed,
                rows["sms-marketing"]
            );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusEnabled,
                rows["feedback-follow-up"]
            );
        }

        [Fact]
        public async Task Get_DerivesGuestPermissionWordingNotConfigured_WhenEnabledChannelMissingCopy()
        {
            var seeded = await SeedOwnerWithPrivacyStateAsync(
                emailEnabled: true,
                smsEnabled: true,
                feedbackEnabled: true,
                smsWording: null,
                emailWording: null,
                readyAt: null
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            var rows = body.GetProperty("privacySetupRows").EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("id").GetString()!,
                    row => row.GetProperty("status").GetString()
                );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusNotUsed,
                rows["guest-permission-wording"]
            );
        }

        [Fact]
        public async Task Save_DoesNotSetReady_WhenEnabledChannelsLackWording()
        {
            var seeded = await SeedOwnerWithIncompletePrivacyAsync();

            using var saveRequest = AuthorizedJson(
                HttpMethod.Put,
                "/api/privacy-consent",
                seeded.OwnerJwt,
                new
                {
                    smsConsentWording = "",
                    emailConsentWording = "",
                }
            );
            var saveResponse = await _client.SendAsync(saveRequest);
            Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
            var saveBody = await ReadJsonAsync(saveResponse);
            Assert.False(saveBody.GetProperty("privacyReady").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.RestaurantId);
            Assert.Null(restaurant.PrivacyConsentReadyAt);
        }

        [Fact]
        public async Task Patch_Returns403_WhenPrivacyConsentIsNoAccess()
        {
            var seeded = await SeedAdminWithPrivacyNoAccessAsync();

            using var request = AuthorizedJson(
                HttpMethod.Patch,
                "/api/privacy-consent",
                seeded.AdminJwt,
                new { smsMarketingPermissionEnabled = false }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Patch_PersistsToggle_AndAppendsActivity()
        {
            var seeded = await SeedOwnerWithPrivacyStateAsync(
                emailEnabled: true,
                smsEnabled: true,
                feedbackEnabled: true,
                smsWording: "SMS copy",
                emailWording: "Email copy",
                readyAt: DateTime.UtcNow
            );

            using var patchRequest = AuthorizedJson(
                HttpMethod.Patch,
                "/api/privacy-consent",
                seeded.OwnerJwt,
                new { smsMarketingPermissionEnabled = false }
            );
            var patchResponse = await _client.SendAsync(patchRequest);
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.RestaurantId);
            Assert.False(restaurant.SmsMarketingPermissionEnabled);

            var activity = await context.LocationActivities
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind
                        == LocationActivityKinds.GuestPermissionToggleChanged
                )
                .SingleAsync();
            Assert.Contains("disabled SMS marketing", activity.Description);
        }

        [Fact]
        public async Task GetActivity_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/privacy-consent/activity"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_Returns403_WhenPrivacyConsentIsNoAccess()
        {
            var seeded = await SeedAdminWithPrivacyNoAccessAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_ReturnsPrivacyKindsOnly_NewestFirst()
        {
            var seeded = await SeedOwnerWithMixedActivitiesAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal(
                LocationActivityKinds.ConsentCopyChanged,
                items[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                LocationActivityKinds.GuestPermissionToggleChanged,
                items[1].GetProperty("kind").GetString()
            );
            Assert.DoesNotContain(
                items,
                item =>
                    item.GetProperty("kind").GetString()
                    == LocationActivityKinds.LocationCreated
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
                FullName = "Privacy Read Gate Owner",
                Email = $"priv-read-gate-owner-{Guid.NewGuid():N}@example.com",
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
                FullName = "Privacy Read Gate Admin",
                Email = $"priv-read-gate-admin-{Guid.NewGuid():N}@example.com",
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
                Name = "Privacy Read Gate Venue",
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
            int RestaurantId
        )> SeedOwnerWithPrivacyStateAsync(
            bool emailEnabled,
            bool smsEnabled,
            bool feedbackEnabled,
            string? smsWording,
            string? emailWording,
            DateTime? readyAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Privacy Read Owner",
                Email = $"priv-read-owner-{Guid.NewGuid():N}@example.com",
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
                Name = "Privacy Read Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                PrivacyConsentReadyAt = readyAt,
                SmsConsentWording = smsWording,
                EmailConsentWording = emailWording,
                EmailMarketingPermissionEnabled = emailEnabled,
                SmsMarketingPermissionEnabled = smsEnabled,
                FeedbackFollowUpPermissionEnabled = feedbackEnabled,
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
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                restaurant.Id
            );
        }

        private async Task<(
            string OwnerJwt,
            int RestaurantId
        )> SeedOwnerWithIncompletePrivacyAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Privacy Qualify Owner",
                Email = $"priv-qualify-owner-{Guid.NewGuid():N}@example.com",
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
                Name = "Privacy Qualify Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                PrivacyConsentReadyAt = null,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
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
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                restaurant.Id
            );
        }

        private async Task<(
            string OwnerJwt,
            int RestaurantId
        )> SeedOwnerWithMixedActivitiesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Privacy Activity Owner",
                Email = $"priv-activity-owner-{Guid.NewGuid():N}@example.com",
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
                Name = "Privacy Activity Venue",
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

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                City = "London",
                Postcode = "W1A 1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);

            var t0 = DateTime.UtcNow.AddHours(-2);
            var t1 = DateTime.UtcNow.AddHours(-1);
            var t2 = DateTime.UtcNow;

            context.LocationActivities.AddRange(
                new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = location.Id,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.LocationCreated,
                    Description = "Created location Main.",
                    OccurredAt = t0,
                },
                new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = null,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.GuestPermissionToggleChanged,
                    Description = "Disabled SMS marketing.",
                    OccurredAt = t1,
                },
                new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = null,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.ConsentCopyChanged,
                    Description = "Updated consent wording.",
                    OccurredAt = t2,
                }
            );
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                restaurant.Id
            );
        }

        private static HttpRequestMessage Authorized(
            HttpMethod method,
            string path,
            string jwt
        )
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string path,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(method, path)
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
