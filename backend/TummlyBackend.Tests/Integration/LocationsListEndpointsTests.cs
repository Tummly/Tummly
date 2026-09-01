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
    public class LocationsListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationsListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLocations_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/locations");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetLocations_Returns403_ForTummlyStaffJwt()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var admin = new Admin
            {
                FullName = "Staff",
                Email = $"staff-loc-list-{Guid.NewGuid():N}@example.com",
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
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetLocations_Returns403_WhenLocationsAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                memberRole: "NoAccess"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetLocations_Returns200_ForViewOnlyBillingAdmin()
        {
            var seeded = await SeedListScenarioAsync(
                memberRole: PermissionRoles.BillingAdmin
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
        }

        [Fact]
        public async Task GetLocations_ReturnsEnvelopeRowsKpisAndCityFacets()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(4, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(10, body.GetProperty("pageSize").GetInt32());

            var kpis = body.GetProperty("kpis");
            Assert.Equal(1, kpis.GetProperty("active").GetInt32());
            Assert.Equal(1, kpis.GetProperty("draft").GetInt32());
            Assert.Equal(1, kpis.GetProperty("paused").GetInt32());
            // Paused has no Active QR → needs-attention. Active has QR + privacy ready.
            Assert.Equal(
                1,
                kpis.GetProperty("setupNeedsAttention").GetInt32()
            );

            var facets = body.GetProperty("cityFacets");
            Assert.Equal(2, facets.GetArrayLength());
            var facetIds = facets
                .EnumerateArray()
                .Select(f => f.GetProperty("id").GetString())
                .OrderBy(x => x)
                .ToArray();
            Assert.Equal(["camden", "soho"], facetIds);

            var rows = body.GetProperty("rows");
            Assert.Equal(4, rows.GetArrayLength());
            var first = rows[0];
            Assert.True(first.TryGetProperty("lifecycleStatus", out _));
            Assert.True(first.TryGetProperty("setupStatus", out _));
            Assert.True(first.TryGetProperty("managerName", out _));
            Assert.True(first.TryGetProperty("cityId", out _));
            Assert.True(first.TryGetProperty("lastActivityAt", out _));
        }

        [Fact]
        public async Task GetLocations_Paginates()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?page=1&pageSize=2"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(4, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(2, body.GetProperty("rows").GetArrayLength());
            // KPIs stay restaurant-scoped (unfiltered).
            Assert.Equal(
                1,
                body.GetProperty("kpis").GetProperty("active").GetInt32()
            );
        }

        [Fact]
        public async Task GetLocations_FiltersByLifecycle()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?lifecycle=draft"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "draft",
                body.GetProperty("rows")[0]
                    .GetProperty("lifecycleStatus")
                    .GetString()
            );
            // KPI totals ignore lifecycle filter.
            Assert.Equal(
                1,
                body.GetProperty("kpis").GetProperty("active").GetInt32()
            );
        }

        [Fact]
        public async Task GetLocations_FiltersBySetup()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?setup=needs-attention"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            foreach (var row in body.GetProperty("rows").EnumerateArray())
            {
                Assert.Equal(
                    "needs-attention",
                    row.GetProperty("setupStatus").GetString()
                );
            }
        }

        [Fact]
        public async Task GetLocations_FiltersByCity()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?city=soho"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "soho",
                body.GetProperty("rows")[0].GetProperty("cityId").GetString()
            );
        }

        [Fact]
        public async Task GetLocations_AttentionIncludesNoActiveQr()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            Assert.True(body.GetProperty("success").GetBoolean());

            var attention = body.GetProperty("attentionItems");
            var noQr = attention
                .EnumerateArray()
                .ToList()
                .Single(item =>
                    item.GetProperty("id").GetString() == "no-active-qr"
                );
            Assert.Contains(
                "no active QR",
                noQr.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
            var locationIds = noQr
                .GetProperty("locationIds")
                .EnumerateArray()
                .Select(id => id.GetInt32())
                .ToArray();
            Assert.Contains(seeded.PausedLocationId, locationIds);
            Assert.DoesNotContain(seeded.ActiveLocationId, locationIds);
        }

        [Fact]
        public async Task GetLocations_AttentionIncludesPrivacyReview_WhenNotReady()
        {
            var seeded = await SeedListScenarioAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurant = context.Restaurants.Single(row =>
                    row.Id == seeded.RestaurantId
                );
                restaurant.PrivacyConsentReadyAt = null;
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var attention = body.GetProperty("attentionItems");
            var privacy = attention
                .EnumerateArray()
                .ToList()
                .Single(item =>
                    item.GetProperty("id").GetString() == "privacy-review"
                );
            var locationIds = privacy
                .GetProperty("locationIds")
                .EnumerateArray()
                .Select(id => id.GetInt32())
                .OrderBy(id => id)
                .ToArray();
            Assert.Equal(
                new[] { seeded.ActiveLocationId, seeded.PausedLocationId }
                    .OrderBy(id => id)
                    .ToArray(),
                locationIds
            );
        }

        [Fact]
        public async Task GetLocations_SetupReadyFilter_ExcludesArchived()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?setup=ready"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var rows = body.GetProperty("rows").EnumerateArray().ToArray();
            Assert.All(
                rows,
                row =>
                    Assert.NotEqual(
                        "archived",
                        row.GetProperty("lifecycleStatus").GetString()
                    )
            );
            Assert.Contains(
                rows,
                row =>
                    row.GetProperty("id").GetInt32() == seeded.ActiveLocationId
            );
            Assert.DoesNotContain(
                rows,
                row =>
                    row.GetProperty("id").GetInt32()
                    == seeded.ArchivedLocationId
            );
        }

        [Fact]
        public async Task GetLocations_SearchesByQ()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?q=Paused"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Contains(
                "Paused",
                body.GetProperty("rows")[0].GetProperty("name").GetString(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task GetLocations_SortsByName()
        {
            var seeded = await SeedListScenarioAsync();

            using var ascRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?sort=name-asc"
            );
            ascRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var ascBody = await ReadJsonAsync(
                await _client.SendAsync(ascRequest)
            );
            var ascNames = ascBody
                .GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .ToArray();

            using var descRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?sort=name-desc"
            );
            descRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var descBody = await ReadJsonAsync(
                await _client.SendAsync(descRequest)
            );
            var descNames = descBody
                .GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .ToArray();

            Assert.Equal(ascNames.OrderBy(n => n).ToArray(), ascNames);
            Assert.Equal(
                ascNames.Reverse().ToArray(),
                descNames
            );
        }

        [Fact]
        public async Task GetLocations_ScopesNamedListMember()
        {
            var seeded = await SeedListScenarioAsync(
                memberRole: PermissionRoles.BillingAdmin,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                seeded.ActiveLocationId,
                body.GetProperty("rows")[0].GetProperty("id").GetInt32()
            );
        }

        [Fact]
        public async Task GetLocations_ReturnsManagerNameAndLastActivity()
        {
            var seeded = await SeedListScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/locations?lifecycle=active"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var row = body.GetProperty("rows")[0];
            Assert.Equal("Aisha Khan", row.GetProperty("managerName").GetString());
            Assert.False(
                string.IsNullOrEmpty(
                    row.GetProperty("lastActivityAt").GetString()
                )
            );
        }

        private async Task<ListSeed> SeedListScenarioAsync(
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
                FullName = "Locations List Owner",
                Email = $"loc-list-owner-{Guid.NewGuid():N}@example.com",
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

            var manager = new User
            {
                FullName = "Aisha Khan",
                Email = $"loc-mgr-{Guid.NewGuid():N}@example.com",
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
            context.Users.Add(manager);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Locations List Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                // Privacy ready so only missing-QR Active/Paused need attention.
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
            var paused = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Paused Venue",
                Address = "3 High Street",
                City = "Camden",
                Postcode = "NW1 2BB",
                LifecycleStatus = LocationLifecycleStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Archived Venue",
                Address = "4 High Street",
                // Empty city — not in facets.
                LifecycleStatus = LocationLifecycleStatus.Archived,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(
                active,
                draft,
                paused,
                archived
            );
            await context.SaveChangesAsync();

            // Active has QR → ready. Paused has no Active QR → needs-attention.
            // Draft → not-started. Archived setup is out of filters.
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

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"guest-{Guid.NewGuid():N}@example.com",
                NormalizedEmail = $"guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = active.Id,
                Name = "Active Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            context.LocationGuestActivityEvents.Add(
                new LocationGuestActivityEvent
                {
                    LocationGuestId = locationGuest.Id,
                    Kind = "captured",
                    OccurredAt = DateTime.UtcNow.AddHours(-2),
                    CreatedAt = DateTime.UtcNow,
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

            var member = new User
            {
                FullName = "Locations List Member",
                Email = $"loc-list-member-{Guid.NewGuid():N}@example.com",
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

            var namedIds = namedInScopeOnly
                ? new[] { active.Id }
                : new[] { active.Id, draft.Id, paused.Id, archived.Id };

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = member.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = memberRole,
                    LocationScope = namedInScopeOnly
                        ? LocationScopeKind.NamedList
                        : LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = namedInScopeOnly
                        ? MembershipLocationScope.SerializeNamedIds(namedIds)
                        : "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return new ListSeed(
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
                restaurant.Id,
                active.Id,
                paused.Id,
                archived.Id
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
                Email = $"loc-na-owner-{Guid.NewGuid():N}@example.com",
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
                Email = $"loc-na-member-{Guid.NewGuid():N}@example.com",
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

        private sealed record ListSeed(
            string OwnerJwt,
            string MemberJwt,
            int RestaurantId,
            int ActiveLocationId,
            int PausedLocationId,
            int ArchivedLocationId
        );
    }
}
