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
    public class PermissionRecordsListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PermissionRecordsListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetPermissionRecords_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/privacy-consent/permission-records"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPermissionRecords_Returns403_WhenPrivacyConsentIsNoAccess()
        {
            var seeded = await SeedAdminWithPrivacyNoAccessAsync();

            using var request = AuthorizedGet(
                "/api/privacy-consent/permission-records",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetPermissionRecords_ReturnsEmptyRows_WhenNoLedgerEntries()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "perm-records-empty-token"
            );

            using var request = AuthorizedGet(
                "/api/privacy-consent/permission-records",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, body.GetProperty("rows").GetArrayLength());
        }

        [Fact]
        public async Task GetPermissionRecords_ReturnsLatestStatePerGuestPermissionLocation()
        {
            var seeded = await SeedOwnerWithLedgerRowsAsync(
                "perm-records-shape-token"
            );

            using var request = AuthorizedGet(
                "/api/privacy-consent/permission-records",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, body.GetProperty("totalCount").GetInt32());

            var rows = body.GetProperty("rows").EnumerateArray().ToList();
            Assert.Equal(2, rows.Count);

            var emailRow = rows.Single(row =>
                row.GetProperty("permissionId").GetString() == "email-marketing"
            );
            Assert.Equal(
                "withdrawn",
                emailRow.GetProperty("currentState").GetString()
            );
            Assert.Equal(
                seeded.LocationGuestId,
                emailRow.GetProperty("locationGuestId").GetInt32()
            );
            Assert.Equal(
                seeded.LocationId,
                emailRow.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Amira Khan", emailRow.GetProperty("guestName").GetString());
            Assert.Equal(
                "Email marketing",
                emailRow.GetProperty("permissionLabel").GetString()
            );
            Assert.Equal("Camden", emailRow.GetProperty("locationLabel").GetString());
            Assert.Equal("Guest form", emailRow.GetProperty("sourceLabel").GetString());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    emailRow.GetProperty("recordedAt").GetString()
                )
            );

            var smsRow = rows.Single(row =>
                row.GetProperty("permissionId").GetString() == "sms-marketing"
            );
            Assert.Equal(
                "granted",
                smsRow.GetProperty("currentState").GetString()
            );
        }

        [Fact]
        public async Task GetPermissionRecords_SearchMatchesGuestNameCaseInsensitive()
        {
            var seeded = await SeedOwnerWithLedgerRowsAsync(
                "perm-records-search-token"
            );

            using var request = AuthorizedGet(
                "/api/privacy-consent/permission-records?q=AMIRA",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
            Assert.All(
                body.GetProperty("rows").EnumerateArray(),
                row =>
                    Assert.Equal(
                        "Amira Khan",
                        row.GetProperty("guestName").GetString()
                    )
            );
        }

        [Fact]
        public async Task GetPermissionRecords_FiltersByPermissionCurrentStateAndLocation()
        {
            var seeded = await SeedOwnerWithLedgerRowsAsync(
                "perm-records-filter-token",
                includeSecondLocation: true
            );

            using var request = AuthorizedGet(
                $"/api/privacy-consent/permission-records"
                    + $"?permission=sms-marketing"
                    + $"&currentState=granted"
                    + $"&location={seeded.LocationId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "sms-marketing",
                body.GetProperty("rows")[0]
                    .GetProperty("permissionId")
                    .GetString()
            );
        }

        [Fact]
        public async Task GetPermissionRecords_FiltersByDatePreset()
        {
            var seeded = await SeedOwnerWithLedgerRowsAsync(
                "perm-records-date-token"
            );

            using var inWindow = AuthorizedGet(
                "/api/privacy-consent/permission-records"
                    + "?dateFrom=2026-08-22T00:00:00Z"
                    + "&dateTo=2026-08-23T00:00:00Z",
                seeded.OwnerJwt
            );
            var inWindowResponse = await _client.SendAsync(inWindow);
            var inWindowBody = await ReadJsonAsync(inWindowResponse);

            Assert.Equal(HttpStatusCode.OK, inWindowResponse.StatusCode);
            Assert.Equal(1, inWindowBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "email-marketing",
                inWindowBody.GetProperty("rows")[0]
                    .GetProperty("permissionId")
                    .GetString()
            );

            using var outOfWindow = AuthorizedGet(
                "/api/privacy-consent/permission-records"
                    + "?dateFrom=2026-08-19T00:00:00Z"
                    + "&dateTo=2026-08-20T00:00:00Z",
                seeded.OwnerJwt
            );
            var outOfWindowResponse = await _client.SendAsync(outOfWindow);
            var outOfWindowBody = await ReadJsonAsync(outOfWindowResponse);

            Assert.Equal(HttpStatusCode.OK, outOfWindowResponse.StatusCode);
            Assert.Equal(0, outOfWindowBody.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetPermissionRecords_PaginatesWithStableSort()
        {
            var seeded = await SeedOwnerWithManyLedgerRowsAsync(
                "perm-records-page-token",
                guestCount: 26
            );

            using var pageOne = AuthorizedGet(
                "/api/privacy-consent/permission-records?page=1",
                seeded.OwnerJwt
            );
            var pageOneResponse = await _client.SendAsync(pageOne);
            var pageOneBody = await ReadJsonAsync(pageOneResponse);

            Assert.Equal(HttpStatusCode.OK, pageOneResponse.StatusCode);
            Assert.Equal(26, pageOneBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(25, pageOneBody.GetProperty("rows").GetArrayLength());
            Assert.Equal(1, pageOneBody.GetProperty("page").GetInt32());
            Assert.Equal(25, pageOneBody.GetProperty("pageSize").GetInt32());

            var firstPageRecordedAt = pageOneBody
                .GetProperty("rows")
                .EnumerateArray()
                .Select(row =>
                    DateTime.Parse(
                        row.GetProperty("recordedAt").GetString()!
                    )
                )
                .ToList();
            Assert.Equal(25, firstPageRecordedAt.Count);
            for (var i = 1; i < firstPageRecordedAt.Count; i++)
            {
                Assert.True(
                    firstPageRecordedAt[i - 1] >= firstPageRecordedAt[i]
                );
            }
            Assert.True(
                firstPageRecordedAt[0] > firstPageRecordedAt[^1],
                "Default sort should return newest recorded rows first."
            );

            var firstPageIds = pageOneBody
                .GetProperty("rows")
                .EnumerateArray()
                .Select(row => row.GetProperty("id").GetString())
                .ToList();

            using var pageTwo = AuthorizedGet(
                "/api/privacy-consent/permission-records?page=2",
                seeded.OwnerJwt
            );
            var pageTwoResponse = await _client.SendAsync(pageTwo);
            var pageTwoBody = await ReadJsonAsync(pageTwoResponse);

            Assert.Equal(1, pageTwoBody.GetProperty("rows").GetArrayLength());
            var secondPageIds = pageTwoBody
                .GetProperty("rows")
                .EnumerateArray()
                .Select(row => row.GetProperty("id").GetString())
                .ToList();
            Assert.Empty(firstPageIds.Intersect(secondPageIds));
        }

        private async Task<(string OwnerJwt, int LocationId)> SeedOwnerWithManyLedgerRowsAsync(
            string token,
            int guestCount
        )
        {
            var seeded = await SeedOwnerWithLocationAsync(token);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var ledger = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var restaurantId = await context.RestaurantLocations
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();

            var at = new DateTime(2026, 8, 22, 14, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < guestCount; i++)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurantId,
                    Email = $"{token}-guest-{i}@example.com",
                    NormalizedEmail = $"{token}-guest-{i}@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var guest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = $"Guest {i:D2}",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                };
                context.LocationGuests.Add(guest);
                await context.SaveChangesAsync();

                ledger.RecordEvent(
                    guest.Id,
                    seeded.LocationId,
                    LocationGuestPermissionKind.EmailMarketing,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    at.AddMinutes(i)
                );
            }

            await context.SaveChangesAsync();
            return (seeded.OwnerJwt, seeded.LocationId);
        }

        private async Task<(
            string OwnerJwt,
            int LocationId,
            int LocationGuestId
        )> SeedOwnerWithLedgerRowsAsync(
            string token,
            bool includeSecondLocation = false
        )
        {
            var seeded = await SeedOwnerWithLocationAsync(token);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var ledger = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var guest = await context.LocationGuests
                .Include(lg => lg.MasterGuest)
                .SingleAsync(lg => lg.Id == seeded.LocationGuestId);
            guest.Name = "Amira Khan";
            await context.SaveChangesAsync();

            var older = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 8, 22, 14, 26, 0, DateTimeKind.Utc);

            ledger.RecordEvent(
                guest.Id,
                seeded.LocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                older
            );
            ledger.RecordEvent(
                guest.Id,
                seeded.LocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.GuestForm,
                newer
            );
            ledger.RecordEvent(
                guest.Id,
                seeded.LocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                older.AddHours(1)
            );

            if (includeSecondLocation)
            {
                var restaurantId = await context.RestaurantLocations
                    .Where(row => row.Id == seeded.LocationId)
                    .Select(row => row.RestaurantId)
                    .SingleAsync();

                var secondLocation = new RestaurantLocation
                {
                    RestaurantId = restaurantId,
                    LocationName = "Shoreditch",
                    Address = "2 High Street",
                    CreatedAt = DateTime.UtcNow,
                };
                context.RestaurantLocations.Add(secondLocation);
                await context.SaveChangesAsync();

                ledger.RecordEvent(
                    guest.Id,
                    secondLocation.Id,
                    LocationGuestPermissionKind.SmsMarketing,
                    LocationGuestPermissionLedgerEventKinds.Withdraw,
                    LocationGuestPermissionLedgerSources.Operator,
                    newer.AddHours(2)
                );
            }

            await context.SaveChangesAsync();
            return seeded;
        }

        private async Task<(
            string OwnerJwt,
            int LocationId,
            int LocationGuestId
        )> SeedOwnerWithLocationAsync(string token)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Permission Records Owner",
                Email = $"perm-records-{token}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900401",
                Role = "Owner",
                AccountType = "Single",
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
                Name = "Permission Records Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{token}@example.com",
                NormalizedEmail = $"{token}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );

            return (jwt, location.Id, locationGuest.Id);
        }

        private async Task<(string AdminJwt, int RestaurantId)>
            SeedAdminWithPrivacyNoAccessAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Perm Records Gate Owner",
                Email = $"perm-rec-gate-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900402",
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
                FullName = "Perm Records Gate Admin",
                Email = $"perm-rec-gate-admin-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900403",
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
                Name = "Perm Records Gate Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
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

            var adminJwt = jwtService.GenerateToken(
                admin.Id.ToString(),
                admin.Email,
                admin.Role
            );

            return (adminJwt, restaurant.Id);
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
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
