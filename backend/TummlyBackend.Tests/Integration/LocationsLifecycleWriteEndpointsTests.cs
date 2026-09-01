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
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class LocationsLifecycleWriteEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationsLifecycleWriteEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AddOwnedLocation_CreatesDraft_WithCityAndLocationCreatedActivity()
        {
            var seeded = await SeedPilotWithRoomAsync();

            using var request = AuthorizedPost(
                "/api/locations",
                seeded.OwnerJwt,
                new
                {
                    locationName = "Soho Draft",
                    address = "10 Wardour Street",
                    city = "London",
                    postcode = "W1D 6QF",
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var locationId = body.GetProperty("locationId").GetInt32();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == locationId);

            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
            Assert.Equal("Soho Draft", location.LocationName);
            Assert.Equal("London", location.City);
            Assert.Equal("W1D 6QF", location.Postcode);

            var activity = await context.LocationActivities
                .AsNoTracking()
                .SingleAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.LocationId == locationId
                );
            Assert.Equal(LocationActivityKinds.LocationCreated, activity.Kind);
            Assert.Equal(seeded.OwnerUserId, activity.ActorUserId);
        }

        [Fact]
        public async Task AddOwnedLocation_MissingCity_Returns400()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Missing City Venue"
            );

            using var request = AuthorizedPost(
                "/api/locations",
                seeded.OwnerJwt,
                new
                {
                    locationName = "No City",
                    address = "1 High Street",
                    postcode = "LS1 1AA",
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var count = await context.RestaurantLocations
                .CountAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Activate_Draft_RequiresCity_AndDoesNotNeedQrOrPrivacy()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Activate Incomplete Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: null
            );

            using var request = AuthorizedPost(
                $"/api/locations/{locationId}/activate",
                seeded.OwnerJwt,
                new { }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == locationId);
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
        }

        [Fact]
        public async Task Activate_CompleteDraft_MovesToActive_AndEmitsLifecycleChanged()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Activate Complete Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );

            using var request = AuthorizedPost(
                $"/api/locations/{locationId}/activate",
                seeded.OwnerJwt,
                new { }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == locationId);
            Assert.Equal(LocationLifecycleStatus.Active, location.LifecycleStatus);

            var activity = await context.LocationActivities
                .AsNoTracking()
                .Where(row =>
                    row.LocationId == locationId
                    && row.Kind == LocationActivityKinds.LifecycleChanged
                )
                .SingleAsync();
            Assert.Equal("draft", activity.FromValue);
            Assert.Equal("active", activity.ToValue);
        }

        [Fact]
        public async Task DeleteDraft_SafeDraft_HardDeletes()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Delete Safe Draft Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );

            using var request = AuthorizedDelete(
                $"/api/locations/{locationId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.RestaurantLocations.AnyAsync(row =>
                    row.Id == locationId
                )
            );
            Assert.True(
                await context.LocationActivities.AnyAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind == LocationActivityKinds.LifecycleChanged
                    && row.ToValue == "deleted"
                )
            );
        }

        [Fact]
        public async Task DeleteDraft_WithCatalogOffer_Refuses()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Delete Offer Draft Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.CatalogOffers.Add(
                    new CatalogOffer
                    {
                        RestaurantLocationId = locationId,
                        Status = CatalogOfferStatus.Draft,
                        OfferType = CatalogOfferType.PercentageDiscount,
                        Title = "Draft offer",
                        Description = "Tied to draft location",
                        Validity = CatalogOfferValidity.Days14AfterIssue,
                        DiscountPercentage = 10m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedDelete(
                $"/api/locations/{locationId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var verify = _factory.Services.CreateScope();
            var verifyContext = verify.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.True(
                await verifyContext.RestaurantLocations.AnyAsync(row =>
                    row.Id == locationId
                )
            );
        }

        [Fact]
        public async Task DeleteDraft_WithGuestHistory_Refuses()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Delete Unsafe Draft Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var master = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = "guest@example.com",
                    NormalizedEmail = "guest@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();
                context.LocationGuests.Add(
                    new LocationGuest
                    {
                        MasterGuestId = master.Id,
                        RestaurantLocationId = locationId,
                        Name = "Guest",
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedDelete(
                $"/api/locations/{locationId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var verify = _factory.Services.CreateScope();
            var verifyContext = verify.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.True(
                await verifyContext.RestaurantLocations.AnyAsync(row =>
                    row.Id == locationId
                )
            );
        }

        [Fact]
        public async Task SetManager_ValidMember_SetsAndClears()
        {
            var seeded = await SeedPilotWithRoomAndManagerCandidateAsync();
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );

            using (
                var setRequest = AuthorizedPut(
                    $"/api/locations/{locationId}/manager",
                    seeded.OwnerJwt,
                    new { managerUserId = seeded.ManagerUserId }
                )
            )
            {
                var setResponse = await _client.SendAsync(setRequest);
                Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = await context.RestaurantLocations
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == locationId);
                Assert.Equal(seeded.ManagerUserId, location.ManagerUserId);
                Assert.True(
                    await context.LocationActivities.AnyAsync(row =>
                        row.LocationId == locationId
                        && row.Kind == LocationActivityKinds.ManagerChanged
                    )
                );
            }

            using var clearRequest = AuthorizedPut(
                $"/api/locations/{locationId}/manager",
                seeded.OwnerJwt,
                new { managerUserId = (int?)null }
            );
            var clearResponse = await _client.SendAsync(clearRequest);
            Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);

            using var verify = _factory.Services.CreateScope();
            var verifyContext = verify.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var cleared = await verifyContext.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == locationId);
            Assert.Null(cleared.ManagerUserId);
        }

        [Fact]
        public async Task SetManager_StaffRole_Returns400()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Manager Bad Role Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );

            int staffUserId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var staff = new User
                {
                    FullName = "Staff Member",
                    Email = $"{Guid.NewGuid():N}@example.com",
                    PasswordHash = "hash",
                    PhoneNumber = "07700900333",
                    Role = "User",
                    AccountType = "Multi",
                    IsEmailVerified = true,
                    IsApprovedByAdmin = true,
                    CreatedAt = DateTime.UtcNow,
                    ActivatedAt = DateTime.UtcNow,
                    ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
                };
                context.Users.Add(staff);
                await context.SaveChangesAsync();
                staffUserId = staff.Id;
                context.RestaurantMemberships.Add(
                    new RestaurantMembership
                    {
                        UserId = staff.Id,
                        RestaurantId = seeded.RestaurantId,
                        PermissionRole = PermissionRoles.Staff,
                        LocationScope = LocationScopeKind.AllLocations,
                        NamedLocationIdsJson = "[]",
                        Status = MembershipStatus.Active,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedPut(
                $"/api/locations/{locationId}/manager",
                seeded.OwnerJwt,
                new { managerUserId = staffUserId }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SetManager_NamedListMissingLocation_Returns400()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Manager Scope Venue"
            );
            var locationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "London"
            );
            var otherLocationId = await SeedDraftLocationAsync(
                seeded.RestaurantId,
                city: "Manchester"
            );

            int managerUserId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var manager = new User
                {
                    FullName = "Scoped Manager",
                    Email = $"{Guid.NewGuid():N}@example.com",
                    PasswordHash = "hash",
                    PhoneNumber = "07700900444",
                    Role = "User",
                    AccountType = "Multi",
                    IsEmailVerified = true,
                    IsApprovedByAdmin = true,
                    CreatedAt = DateTime.UtcNow,
                    ActivatedAt = DateTime.UtcNow,
                    ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
                };
                context.Users.Add(manager);
                await context.SaveChangesAsync();
                managerUserId = manager.Id;
                context.RestaurantMemberships.Add(
                    new RestaurantMembership
                    {
                        UserId = manager.Id,
                        RestaurantId = seeded.RestaurantId,
                        PermissionRole = PermissionRoles.LocationManager,
                        LocationScope = LocationScopeKind.NamedList,
                        NamedLocationIdsJson = $"[{otherLocationId}]",
                        Status = MembershipStatus.Active,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedPut(
                $"/api/locations/{locationId}/manager",
                seeded.OwnerJwt,
                new { managerUserId }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<int> SeedDraftLocationAsync(
            int restaurantId,
            string? city
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = "Draft Spot",
                Address = "10 Wardour Street",
                City = city,
                Postcode = "W1D 6QF",
                LifecycleStatus = LocationLifecycleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<Seeded> SeedPilotWithRoomAndManagerCandidateAsync()
        {
            var seeded = await SeedPilotWithRoomAsync(
                restaurantName: "Manager Venue"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var manager = new User
            {
                FullName = "Site Manager",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900222",
                Role = "User",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(manager);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = manager.Id,
                    RestaurantId = seeded.RestaurantId,
                    PermissionRole = PermissionRoles.LocationManager,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return seeded with { ManagerUserId = manager.Id };
        }

        private async Task<Seeded> SeedPilotWithRoomAsync(
            string restaurantName = "Pilot Room Venue"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = $"Owner {restaurantName}",
                Email = $"{Guid.NewGuid():N}@example.com",
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
                Name = restaurantName,
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    CurrentPricebookId
                )
            );

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

            return new Seeded(
                restaurant.Id,
                owner.Id,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                )
            );
        }

        private static HttpRequestMessage AuthorizedPost(
            string path,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static HttpRequestMessage AuthorizedPut(
            string path,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static HttpRequestMessage AuthorizedDelete(
            string path,
            string jwt
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(
            int RestaurantId,
            int OwnerUserId,
            string OwnerJwt,
            int ManagerUserId = 0
        );
    }
}
