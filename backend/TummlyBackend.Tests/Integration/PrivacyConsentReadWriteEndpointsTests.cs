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
        public async Task Get_ReturnsSetupRows_Toggles_AndReady()
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
            Assert.False(body.TryGetProperty("emailConsentWording", out _));
            Assert.False(body.TryGetProperty("smsConsentWording", out _));
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
            Assert.True(body.GetProperty("actorCanManage").GetBoolean());
            Assert.True(body.GetProperty("canViewGuests").GetBoolean());

            var rows = body.GetProperty("privacySetupRows").EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("id").GetString()!,
                    row => row.GetProperty("status").GetString()
                );
            Assert.Equal(
                PrivacyConsentSetupDerivation.StatusConfigured,
                rows["privacy-notice"]
            );
            Assert.False(rows.ContainsKey("guest-permission-wording"));
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
        public async Task Save_SetsReady_WithoutConsentWording()
        {
            var seeded = await SeedOwnerWithIncompletePrivacyAsync();

            using var saveRequest = Authorized(
                HttpMethod.Put,
                "/api/privacy-consent",
                seeded.OwnerJwt
            );
            var saveResponse = await _client.SendAsync(saveRequest);
            Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
            var saveBody = await ReadJsonAsync(saveResponse);
            Assert.True(saveBody.GetProperty("privacyReady").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.RestaurantId);
            Assert.NotNull(restaurant.PrivacyConsentReadyAt);
        }

        [Fact]
        public async Task Save_IsIdempotent_WhenAlreadyReady()
        {
            var seeded = await SeedOwnerWithPrivacyStateAsync(
                emailEnabled: true,
                smsEnabled: false,
                feedbackEnabled: true,
                smsWording: null,
                emailWording: null,
                readyAt: DateTime.UtcNow.AddDays(-1)
            );

            using var saveRequest = Authorized(
                HttpMethod.Put,
                "/api/privacy-consent",
                seeded.OwnerJwt
            );
            var saveResponse = await _client.SendAsync(saveRequest);
            Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
            var saveBody = await ReadJsonAsync(saveResponse);
            Assert.True(saveBody.GetProperty("privacyReady").GetBoolean());
        }

        [Fact]
        public async Task Patch_Returns401_WhenUnauthenticated()
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                "/api/privacy-consent"
            )
            {
                Content = JsonContent.Create(
                    new { smsMarketingPermissionEnabled = false }
                ),
            };
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
            Assert.Equal(4, items.Count);
            Assert.Equal(
                LocationActivityKinds.ConsentCopyChanged,
                items[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                LocationActivityKinds.GuestMarketingUnsubscribed,
                items[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                LocationActivityKinds.PrivacyReviewCompleted,
                items[2].GetProperty("kind").GetString()
            );
            Assert.Equal(
                LocationActivityKinds.GuestPermissionToggleChanged,
                items[3].GetProperty("kind").GetString()
            );
            Assert.DoesNotContain(
                items,
                item =>
                    item.GetProperty("kind").GetString()
                    == LocationActivityKinds.LocationCreated
            );
        }

        [Fact]
        public async Task Get_ReturnsActorIsWorkspaceOwner_TrueForOwner_FalseForNamedListManager()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            var ownerBody = await GetJsonAsync(
                "/api/privacy-consent",
                seeded.OwnerJwt
            );
            Assert.True(
                ownerBody.GetProperty("actorIsWorkspaceOwner").GetBoolean()
            );

            var managerBody = await GetJsonAsync(
                "/api/privacy-consent",
                seeded.ManagerJwt
            );
            Assert.False(
                managerBody.GetProperty("actorIsWorkspaceOwner").GetBoolean()
            );
        }

        [Fact]
        public async Task GetActivity_Default_ExcludesArchivedLocationRows_ForOwner()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            var descriptions = await GetActivityDescriptionsAsync(
                "/api/privacy-consent/activity",
                seeded.OwnerJwt
            );

            Assert.Contains("Live Location withdrawal.", descriptions);
            Assert.Contains("Restaurant-wide toggle.", descriptions);
            Assert.DoesNotContain("Archived Location withdrawal.", descriptions);
        }

        [Fact]
        public async Task GetActivity_IncludeArchived_ReturnsOnlyArchivedLocationRows_ForOwner()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity?includeArchived=true",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items").EnumerateArray().ToList();
            Assert.Single(items);
            Assert.Equal(
                "Archived Location withdrawal.",
                items[0].GetProperty("description").GetString()
            );
            Assert.True(items[0].GetProperty("locationArchived").GetBoolean());
            Assert.Equal(
                "Old Site",
                items[0].GetProperty("locationName").GetString()
            );
        }

        [Fact]
        public async Task GetActivity_IncludeArchived_Returns403_ForNamedListManager()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity?includeArchived=true",
                seeded.ManagerJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_IncludeArchived_Owner_WritesOneReadAuditRow_WithoutHistoryText()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();
            var before = await CountArchivedReadAuditAsync(seeded.RestaurantId);

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity?includeArchived=true",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var rows = await ReadArchivedReadAuditAsync(seeded.RestaurantId);
            Assert.Equal(before + 1, rows.Count);
            var row = rows.Single();
            Assert.Equal($"user:{seeded.OwnerId}", row.ActorIdentity);
            Assert.Null(row.ActorAdminUserId);
            Assert.Equal(AdminAuditTargetTypes.Restaurant, row.TargetType);
            Assert.Equal(seeded.RestaurantId.ToString(), row.TargetId);
            Assert.Equal(seeded.RestaurantId, row.RestaurantId);
            Assert.True(row.Succeeded);
            Assert.Contains("\"role\":\"Owner\"", row.DetailJson);
            Assert.Contains("\"scope\":\"archived_locations\"", row.DetailJson);
            Assert.DoesNotContain("Archived Location withdrawal.", row.DetailJson ?? string.Empty);
            Assert.DoesNotContain("Old Site", row.DetailJson ?? string.Empty);
        }

        [Fact]
        public async Task GetActivity_IncludeArchived_Manager_Returns403_WithAccessRestrictedCopy_AndWritesNoReadAuditRow()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();
            var before = await CountArchivedReadAuditAsync(seeded.RestaurantId);

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity?includeArchived=true",
                seeded.ManagerJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("Access restricted", body.GetProperty("title").GetString());
            Assert.Equal(
                "You do not have permission to view this archived information. Access requires the appropriate Workspace permissions. Contact your Workspace Owner if you believe you need access.",
                body.GetProperty("message").GetString()
            );
            Assert.False(body.TryGetProperty("items", out _));
            Assert.Equal(before, await CountArchivedReadAuditAsync(seeded.RestaurantId));
        }

        [Fact]
        public async Task GetActivity_IncludeArchived_OtherTenantOwner_SeesOnlyOwnArchivedRows_AndAuditsOwnTenant()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity?includeArchived=true",
                seeded.OtherOwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var descriptions = body.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("description").GetString())
                .ToList();
            Assert.Equal(new[] { "Other tenant archived withdrawal." }, descriptions);

            Assert.Empty(await ReadArchivedReadAuditAsync(seeded.RestaurantId));
            var otherRows = await ReadArchivedReadAuditAsync(seeded.OtherRestaurantId);
            Assert.Single(otherRows);
            Assert.Equal($"user:{seeded.OtherOwnerId}", otherRows[0].ActorIdentity);
        }

        [Fact]
        public async Task GetActivity_NamedListManager_KeepsLiveRowsOnly_EvenWhenArchivedLocationIsAssigned()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            var descriptions = await GetActivityDescriptionsAsync(
                "/api/privacy-consent/activity",
                seeded.ManagerJwt
            );

            Assert.Contains("Live Location withdrawal.", descriptions);
            Assert.Contains("Restaurant-wide toggle.", descriptions);
            Assert.DoesNotContain("Archived Location withdrawal.", descriptions);
        }

        [Fact]
        public async Task GetActivity_IncludeArchived_NeverReturnsOtherTenantRows()
        {
            var seeded = await SeedWorkspaceWithArchivedHistoryAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/privacy-consent/activity?includeArchived=true",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var descriptions = body.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("description").GetString())
                .ToList();
            Assert.Contains("Archived Location withdrawal.", descriptions);
            Assert.DoesNotContain(
                "Other tenant archived withdrawal.",
                descriptions
            );
        }

        private async Task<JsonElement> GetJsonAsync(string path, string jwt)
        {
            using var request = Authorized(HttpMethod.Get, path, jwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadJsonAsync(response);
        }

        private async Task<List<string?>> GetActivityDescriptionsAsync(
            string path,
            string jwt
        )
        {
            var body = await GetJsonAsync(path, jwt);
            return body.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("description").GetString())
                .ToList();
        }

        private async Task<int> CountArchivedReadAuditAsync(int restaurantId)
        {
            return (await ReadArchivedReadAuditAsync(restaurantId)).Count;
        }

        private async Task<List<AdminAuditEvent>> ReadArchivedReadAuditAsync(
            int restaurantId
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await context.AdminAuditEvents
                .AsNoTracking()
                .Where(e =>
                    e.Action == AdminAuditActions.PrivacyArchivedHistoryRead
                    && e.RestaurantId == restaurantId
                )
                .ToListAsync();
        }

        private async Task<ArchivedHistorySeed> SeedWorkspaceWithArchivedHistoryAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = NewActivatedUser("Archived History Owner", "priv-archived-owner");
            var otherOwner = NewActivatedUser("Other Tenant Owner", "priv-archived-other");
            var manager = NewActivatedUser("Archived History Manager", "priv-archived-manager");
            context.Users.AddRange(owner, otherOwner, manager);
            await context.SaveChangesAsync();

            var restaurant = NewRestaurant("Archived History Venue", owner.Id);
            var otherRestaurant = NewRestaurant("Other Tenant Venue", otherOwner.Id);
            context.Restaurants.AddRange(restaurant, otherRestaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;
            manager.SelectedRestaurantId = restaurant.Id;
            context.RestaurantMemberships.AddRange(
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

            var live = NewLocation(restaurant.Id, "Live Site", LocationLifecycleStatus.Active);
            var archived = NewLocation(restaurant.Id, "Old Site", LocationLifecycleStatus.Archived);
            var otherArchived = NewLocation(
                otherRestaurant.Id,
                "Other Tenant Old Site",
                LocationLifecycleStatus.Archived
            );
            context.RestaurantLocations.AddRange(live, archived, otherArchived);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = manager.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.LocationManager,
                    LocationScope = LocationScopeKind.NamedList,
                    NamedLocationIdsJson = MembershipLocationScope
                        .SerializeNamedIds(new[] { live.Id, archived.Id }),
                    Status = MembershipStatus.Active,
                }
            );

            var now = DateTime.UtcNow;
            context.LocationActivities.AddRange(
                NewPrivacyActivity(restaurant.Id, live.Id, owner.Id, "Live Location withdrawal.", now.AddHours(-3)),
                NewPrivacyActivity(restaurant.Id, archived.Id, owner.Id, "Archived Location withdrawal.", now.AddHours(-2)),
                NewPrivacyActivity(restaurant.Id, null, owner.Id, "Restaurant-wide toggle.", now.AddHours(-1)),
                NewPrivacyActivity(
                    otherRestaurant.Id,
                    otherArchived.Id,
                    otherOwner.Id,
                    "Other tenant archived withdrawal.",
                    now
                )
            );
            await context.SaveChangesAsync();

            return new ArchivedHistorySeed(
                owner.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                manager.Id,
                jwtService.GenerateToken(manager.Id.ToString(), manager.Email, manager.Role),
                restaurant.Id,
                otherOwner.Id,
                jwtService.GenerateToken(otherOwner.Id.ToString(), otherOwner.Email, otherOwner.Role),
                otherRestaurant.Id
            );
        }

        private static User NewActivatedUser(string fullName, string emailPrefix)
        {
            return new User
            {
                FullName = fullName,
                Email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com",
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
        }

        private static Restaurant NewRestaurant(string name, int ownerUserId)
        {
            return new Restaurant
            {
                Name = name,
                AccountType = "Multi",
                OwnerUserId = ownerUserId,
                BillingContactUserId = ownerUserId,
                PrivacyContactUserId = ownerUserId,
                SupportContactUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private static RestaurantLocation NewLocation(
            int restaurantId,
            string name,
            LocationLifecycleStatus lifecycle
        )
        {
            return new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = name,
                Address = "1 High Street",
                City = "London",
                Postcode = "W1A 1AA",
                LifecycleStatus = lifecycle,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private static LocationActivity NewPrivacyActivity(
            int restaurantId,
            int? locationId,
            int actorUserId,
            string description,
            DateTime occurredAt
        )
        {
            return new LocationActivity
            {
                RestaurantId = restaurantId,
                LocationId = locationId,
                ActorUserId = actorUserId,
                ActorDisplayName = "Privacy Actor",
                Kind = LocationActivityKinds.GuestPermissionToggleChanged,
                Description = description,
                OccurredAt = occurredAt,
            };
        }

        private sealed record ArchivedHistorySeed(
            int OwnerId,
            string OwnerJwt,
            int ManagerId,
            string ManagerJwt,
            int RestaurantId,
            int OtherOwnerId,
            string OtherOwnerJwt,
            int OtherRestaurantId
        );

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

            var t0 = DateTime.UtcNow.AddHours(-4);
            var t1 = DateTime.UtcNow.AddHours(-3);
            var t2 = DateTime.UtcNow.AddHours(-2);
            var t3 = DateTime.UtcNow.AddHours(-1);
            var t4 = DateTime.UtcNow;

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
                    Kind = LocationActivityKinds.PrivacyReviewCompleted,
                    Description = "Completed the privacy review.",
                    OccurredAt = t2,
                },
                new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = location.Id,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.GuestMarketingUnsubscribed,
                    Description = "Guest opted out of marketing.",
                    OccurredAt = t3,
                },
                new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = null,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.ConsentCopyChanged,
                    Description = "Updated consent wording.",
                    OccurredAt = t4,
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
