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
    public class LocationsActivityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationsActivityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetActivity_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/locations/activity");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_Returns403_ForTummlyStaffJwt()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var admin = new Admin
            {
                FullName = "Staff",
                Email = $"staff-loc-act-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();
            var jwt = jwtService.GenerateAdminToken(admin);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/activity"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_Returns403_WhenLocationsAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                memberRole: "NoAccess"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/activity"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_ReturnsEmptyItems_WhenNoRows()
        {
            var seeded = await SeedRestaurantAsync(withActivities: false);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/activity"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task GetActivity_ReturnsNewestFirst_WithItemShape()
        {
            var seeded = await SeedRestaurantAsync(withActivities: true);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/activity"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var items = body.GetProperty("items");
            Assert.Equal(3, items.GetArrayLength());

            var first = items[0];
            Assert.Equal(seeded.NewestActivityId, first.GetProperty("id").GetInt32());
            Assert.Equal(
                seeded.LocationId,
                first.GetProperty("locationId").GetInt32()
            );
            Assert.Equal(
                LocationActivityKinds.LifecycleChanged,
                first.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Activated draft location.",
                first.GetProperty("description").GetString()
            );
            Assert.False(
                string.IsNullOrWhiteSpace(
                    first.GetProperty("occurredAt").GetString()
                )
            );

            Assert.Equal(
                seeded.MiddleActivityId,
                items[1].GetProperty("id").GetInt32()
            );
            Assert.Equal(
                seeded.OldestActivityId,
                items[2].GetProperty("id").GetInt32()
            );
            Assert.True(items[2].GetProperty("locationId").ValueKind == JsonValueKind.Null);
        }

        [Fact]
        public async Task GetActivity_ScopesToRestaurant_AndNamedListPlusNullLocation()
        {
            var seeded = await SeedTwoRestaurantsWithNamedMemberAsync();

            using var ownerRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/activity"
            );
            ownerRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var ownerResponse = await _client.SendAsync(ownerRequest);
            var ownerBody = await ReadJsonAsync(ownerResponse);
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
            var ownerIds = ownerBody
                .GetProperty("items")
                .EnumerateArray()
                .Select(i => i.GetProperty("id").GetInt32())
                .ToHashSet();
            Assert.Contains(seeded.RestaurantAActivityId, ownerIds);
            Assert.Contains(seeded.RestaurantANullLocationActivityId, ownerIds);
            Assert.DoesNotContain(seeded.RestaurantBActivityId, ownerIds);

            using var memberRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/activity"
            );
            memberRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.NamedMemberJwt);
            var memberResponse = await _client.SendAsync(memberRequest);
            var memberBody = await ReadJsonAsync(memberResponse);
            Assert.Equal(HttpStatusCode.OK, memberResponse.StatusCode);
            var memberIds = memberBody
                .GetProperty("items")
                .EnumerateArray()
                .Select(i => i.GetProperty("id").GetInt32())
                .ToHashSet();
            Assert.Contains(seeded.InScopeLocationActivityId, memberIds);
            Assert.Contains(seeded.RestaurantANullLocationActivityId, memberIds);
            Assert.DoesNotContain(seeded.OutOfScopeLocationActivityId, memberIds);
            Assert.DoesNotContain(seeded.RestaurantBActivityId, memberIds);
        }

        private async Task<ActivitySeed> SeedRestaurantAsync(bool withActivities)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Activity Owner",
                Email = $"loc-act-owner-{Guid.NewGuid():N}@example.com",
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
                Name = "Activity Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                PrivacyConsentReadyAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main Location",
                Address = "1 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

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

            int oldestId = 0;
            int middleId = 0;
            int newestId = 0;

            if (withActivities)
            {
                var t0 = DateTime.UtcNow.AddHours(-3);
                var t1 = DateTime.UtcNow.AddHours(-2);
                var t2 = DateTime.UtcNow.AddHours(-1);

                var oldest = new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = null,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.ConsentCopyChanged,
                    Description = "Consent wording updated.",
                    OccurredAt = t0,
                };
                var middle = new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = location.Id,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.LocationCreated,
                    Description = "Created location Main Location.",
                    OccurredAt = t1,
                };
                var newest = new LocationActivity
                {
                    RestaurantId = restaurant.Id,
                    LocationId = location.Id,
                    ActorUserId = owner.Id,
                    ActorDisplayName = owner.FullName,
                    Kind = LocationActivityKinds.LifecycleChanged,
                    Description = "Activated draft location.",
                    OccurredAt = t2,
                };
                context.LocationActivities.AddRange(oldest, middle, newest);
                await context.SaveChangesAsync();
                oldestId = oldest.Id;
                middleId = middle.Id;
                newestId = newest.Id;
            }

            return new ActivitySeed(
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                location.Id,
                oldestId,
                middleId,
                newestId
            );
        }

        private async Task<NamedScopeSeed> SeedTwoRestaurantsWithNamedMemberAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Scope Owner",
                Email = $"loc-act-scope-owner-{Guid.NewGuid():N}@example.com",
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

            var restaurantA = new Restaurant
            {
                Name = "Restaurant A",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                PrivacyConsentReadyAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            var otherOwner = new User
            {
                FullName = "Other Owner",
                Email = $"loc-act-other-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900114",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(otherOwner);
            await context.SaveChangesAsync();

            var restaurantB = new Restaurant
            {
                Name = "Restaurant B",
                AccountType = "Multi",
                OwnerUserId = otherOwner.Id,
                PrivacyConsentReadyAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.AddRange(restaurantA, restaurantB);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurantA.Id;
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurantA.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurantA.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var otherLoc = new RestaurantLocation
            {
                RestaurantId = restaurantB.Id,
                LocationName = "Other Restaurant Loc",
                Address = "3 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope, otherLoc);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurantA.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = otherOwner.Id,
                    RestaurantId = restaurantB.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            var member = new User
            {
                FullName = "Named Member",
                Email = $"loc-act-named-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurantA.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = member.Id,
                    RestaurantId = restaurantA.Id,
                    PermissionRole = PermissionRoles.BillingAdmin,
                    LocationScope = LocationScopeKind.NamedList,
                    NamedLocationIdsJson =
                        MembershipLocationScope.SerializeNamedIds(
                            [inScope.Id]
                        ),
                    Status = MembershipStatus.Active,
                }
            );

            var nullLocActivity = new LocationActivity
            {
                RestaurantId = restaurantA.Id,
                LocationId = null,
                ActorUserId = owner.Id,
                Kind = LocationActivityKinds.PrivacyReviewCompleted,
                Description = "Privacy review completed.",
                OccurredAt = DateTime.UtcNow.AddHours(-1),
            };
            var inScopeActivity = new LocationActivity
            {
                RestaurantId = restaurantA.Id,
                LocationId = inScope.Id,
                ActorUserId = owner.Id,
                Kind = LocationActivityKinds.LocationCreated,
                Description = "Created In Scope.",
                OccurredAt = DateTime.UtcNow.AddHours(-2),
            };
            var outOfScopeActivity = new LocationActivity
            {
                RestaurantId = restaurantA.Id,
                LocationId = outOfScope.Id,
                ActorUserId = owner.Id,
                Kind = LocationActivityKinds.LocationCreated,
                Description = "Created Out Of Scope.",
                OccurredAt = DateTime.UtcNow.AddHours(-3),
            };
            var otherRestaurantActivity = new LocationActivity
            {
                RestaurantId = restaurantB.Id,
                LocationId = otherLoc.Id,
                ActorUserId = otherOwner.Id,
                Kind = LocationActivityKinds.LocationCreated,
                Description = "Other restaurant activity.",
                OccurredAt = DateTime.UtcNow.AddHours(-1),
            };
            context.LocationActivities.AddRange(
                nullLocActivity,
                inScopeActivity,
                outOfScopeActivity,
                otherRestaurantActivity
            );
            await context.SaveChangesAsync();

            return new NamedScopeSeed(
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateToken(
                    member.Id.ToString(),
                    member.Email,
                    member.Role
                ),
                inScopeActivity.Id,
                outOfScopeActivity.Id,
                nullLocActivity.Id,
                otherRestaurantActivity.Id
            );
        }

        private async Task<(string MemberJwt, int MemberUserId)> SeedOwnerAndMemberAsync(
            string memberRole
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "No Access Owner",
                Email = $"loc-act-na-owner-{Guid.NewGuid():N}@example.com",
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
                Name = "No Access Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                PrivacyConsentReadyAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Only Location",
                Address = "1 High Street",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

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

            var member = new User
            {
                FullName = "No Access Member",
                Email = $"loc-act-na-member-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = member.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = memberRole,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateToken(
                    member.Id.ToString(),
                    member.Email,
                    member.Role
                ),
                member.Id
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record ActivitySeed(
            string OwnerJwt,
            int LocationId,
            int OldestActivityId,
            int MiddleActivityId,
            int NewestActivityId
        );

        private sealed record NamedScopeSeed(
            string OwnerJwt,
            string NamedMemberJwt,
            int InScopeLocationActivityId,
            int OutOfScopeLocationActivityId,
            int RestaurantANullLocationActivityId,
            int RestaurantBActivityId
        )
        {
            public int RestaurantAActivityId => InScopeLocationActivityId;
        }
    }
}
