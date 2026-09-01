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
    public class LocationsDetailEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationsDetailEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLocationDetail_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/locations/1/detail");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetLocationDetail_Returns403_ForTummlyStaffJwt()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var admin = new Admin
            {
                FullName = "Staff",
                Email = $"staff-loc-detail-{Guid.NewGuid():N}@example.com",
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
                "/api/locations/1/detail"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetLocationDetail_Returns403_WhenLocationsAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                memberRole: "NoAccess"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/locations/{seeded.ActiveLocationId}/detail"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetLocationDetail_Returns404_WhenLocationNotInScope()
        {
            var seeded = await SeedDetailScenarioAsync(namedInScopeOnly: true);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/locations/{seeded.OutOfScopeLocationId}/detail"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetLocationDetail_Returns404_WhenLocationMissing()
        {
            var seeded = await SeedDetailScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations/999999/detail"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetLocationDetail_ReturnsHeaderAndSetupChecklist()
        {
            var seeded = await SeedDetailScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/locations/{seeded.ActiveLocationId}/detail"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var header = body.GetProperty("header");
            Assert.Equal(seeded.ActiveLocationId, header.GetProperty("id").GetInt32());
            Assert.Equal("Active Camden", header.GetProperty("name").GetString());
            Assert.Equal("Camden", header.GetProperty("city").GetString());
            Assert.Equal("active", header.GetProperty("lifecycleStatus").GetString());
            Assert.Equal("ready", header.GetProperty("setupStatus").GetString());
            Assert.Equal("Aisha Khan", header.GetProperty("managerName").GetString());
            Assert.Equal(
                seeded.ManagerUserId,
                header.GetProperty("managerUserId").GetInt32()
            );
            Assert.Equal("1 High Street", header.GetProperty("address").GetString());
            Assert.Equal("NW1 1AA", header.GetProperty("postcode").GetString());
            Assert.Equal(1, header.GetProperty("liveQrCount").GetInt32());

            var checklist = body.GetProperty("setupChecklist");
            Assert.Equal(
                "complete",
                checklist.GetProperty("locationDetailsAdded").GetString()
            );
            Assert.Equal(
                "complete",
                checklist.GetProperty("qrCodePublishedLive").GetString()
            );
            Assert.Equal(
                "complete",
                checklist.GetProperty("guestFormConnected").GetString()
            );
            Assert.Equal(
                "complete",
                checklist.GetProperty("teamAccessAssigned").GetString()
            );
            Assert.Equal(
                "complete",
                checklist.GetProperty("guestPrivacyNotice").GetString()
            );
            Assert.Equal(
                "complete",
                checklist.GetProperty("firstOfferCreated").GetString()
            );
            Assert.Equal(
                "complete",
                checklist.GetProperty("atLeastOneQrCreated").GetString()
            );
        }

        [Fact]
        public async Task GetLocationDetail_DraftLocation_HasNotStartedChecklistItems()
        {
            var seeded = await SeedDetailScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/locations/{seeded.DraftLocationId}/detail"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var header = body.GetProperty("header");
            Assert.Equal("draft", header.GetProperty("lifecycleStatus").GetString());
            Assert.Equal("not-started", header.GetProperty("setupStatus").GetString());

            var checklist = body.GetProperty("setupChecklist");
            Assert.Equal(
                "not-started",
                checklist.GetProperty("locationDetailsAdded").GetString()
            );
            Assert.Equal(
                "not-started",
                checklist.GetProperty("qrCodePublishedLive").GetString()
            );
            Assert.Equal(
                "optional",
                checklist.GetProperty("teamAccessAssigned").GetString()
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private async Task<DetailSeed> SeedDetailScenarioAsync(
            string memberRole = PermissionRoles.BillingAdmin,
            bool namedInScopeOnly = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Locations Detail Owner",
                Email = $"loc-detail-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900201",
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

            var manager = new User
            {
                FullName = "Aisha Khan",
                Email = $"loc-detail-mgr-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900202",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(manager);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Locations Detail Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                PrivacyConsentReadyAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var active = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Active Camden",
                Address = "1 High Street",
                City = "Camden",
                Postcode = "NW1 1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                ManagerUserId = manager.Id,
                CreatedAt = DateTime.UtcNow,
            };
            var draft = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Draft Soho",
                Address = "2 High Street",
                City = "Soho",
                Postcode = "W1D 1AA",
                LifecycleStatus = LocationLifecycleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(active, draft);
            await context.SaveChangesAsync();

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = active.Id,
                    QrType = QrType.CounterCard,
                    Token = Guid.NewGuid().ToString("N")[..16],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.CatalogOffers.Add(
                new CatalogOffer
                {
                    RestaurantLocationId = active.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "Welcome offer",
                    Description = "10% off",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );

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

            var ownerJwt = jwtService.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );

            string memberJwt = ownerJwt;
            if (namedInScopeOnly)
            {
                var member = new User
                {
                    FullName = "Scoped Member",
                    Email = $"loc-detail-member-{Guid.NewGuid():N}@example.com",
                    PasswordHash = "hash",
                    PhoneNumber = "07700900203",
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
                        LocationScope = LocationScopeKind.NamedList,
                        NamedLocationIdsJson =
                            MembershipLocationScope.SerializeNamedIds([active.Id]),
                        Status = MembershipStatus.Active,
                    }
                );
                await context.SaveChangesAsync();
                memberJwt = jwtService.GenerateToken(
                    member.Id.ToString(),
                    member.Email,
                    member.Role
                );
            }

            return new DetailSeed(
                OwnerJwt: ownerJwt,
                MemberJwt: memberJwt,
                ActiveLocationId: active.Id,
                DraftLocationId: draft.Id,
                OutOfScopeLocationId: draft.Id,
                ManagerUserId: manager.Id
            );
        }

        private async Task<(string MemberJwt, int ActiveLocationId)> SeedOwnerAndMemberAsync(
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
                Email = $"loc-detail-na-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900211",
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
                Name = "No Access Detail Venue",
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
                Email = $"loc-detail-na-member-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900212",
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
                location.Id
            );
        }

        private sealed record DetailSeed(
            string OwnerJwt,
            string MemberJwt,
            int ActiveLocationId,
            int DraftLocationId,
            int OutOfScopeLocationId,
            int ManagerUserId
        );
    }
}
