using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class LocationShapedPermissionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationShapedPermissionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RestaurantLocations_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/restaurant/locations");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RestaurantLocations_Returns403_ForTummlyStaffJwt()
        {
            var jwt = await SeedTummlyStaffJwtAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RestaurantLocations_ReturnsOnlyNamedLocationScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var locations = body.GetProperty("locations");
            Assert.Equal(1, locations.GetArrayLength());
            Assert.Equal(
                seeded.InScopeLocationId,
                locations[0].GetProperty("id").GetInt32()
            );
        }

        [Fact]
        public async Task RestaurantLocations_Returns403_WhenNamedListIsEmpty()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var membership = await context.RestaurantMemberships
                .FirstAsync(m => m.UserId == seeded.MemberUserId);
            membership.NamedLocationIdsJson = "[]";
            await context.SaveChangesAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GuestsRead_Returns403_WhenAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests?locationId={seeded.InScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GuestsRead_Returns200_WhenViewInScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests?locationId={seeded.InScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GuestsRead_Returns403_WhenLocationOutsideScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests?locationId={seeded.OutOfScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GuestsRead_Returns404_WhenLocationUnknown()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/guests?locationId=999999"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GuestsRead_NamedUnknownId_Returns404()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests?locationId={seeded.InScopeLocationId}&locationIds=999999"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GuestsDelete_Returns403_WhenAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/guests/1?locationId={seeded.InScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GuestsRead_LocationScopeAll_UsesActorLiveSet()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true,
                seedGuests: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests?locationId={seeded.InScopeLocationId}&locationScope=all"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "In Scope Guest",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task CaptureSnapshot_Returns403_WhenViewOnMutationPause()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.ReportingOnly,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{seeded.InScopeLocationId}/pause"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureSnapshot_Returns200_WhenViewInScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.ReportingOnly,
                namedInScopeOnly: true
            );

            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/locations/{seeded.InScopeLocationId}/snapshot"
                    + $"?from={Uri.EscapeDataString(from.ToString("o"))}"
                    + $"&to={Uri.EscapeDataString(to.ToString("o"))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CaptureLocations_NamedIdOutsideScope_Returns403()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.ReportingOnly,
                namedInScopeOnly: true
            );

            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/locations"
                    + $"?from={Uri.EscapeDataString(from.ToString("o"))}"
                    + $"&to={Uri.EscapeDataString(to.ToString("o"))}"
                    + $"&locationIds={seeded.OutOfScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureLocations_NamedUnknownId_Returns404()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.ReportingOnly,
                namedInScopeOnly: true
            );

            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/locations"
                    + $"?from={Uri.EscapeDataString(from.ToString("o"))}"
                    + $"&to={Uri.EscapeDataString(to.ToString("o"))}"
                    + "&locationIds=999999"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CampaignsRead_Returns403_WhenAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/campaigns?locationId={seeded.InScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task FeedbackRead_Returns403_WhenAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback?locationId={seeded.InScopeLocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OffersWrite_Returns403_WhenView()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.ReportingOnly,
                namedInScopeOnly: true
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    offerType = "percentage_discount",
                    title = "10% off next visit",
                    description = "View must not create.",
                    validity = "days_14_after_issue",
                    discountPercentage = 10m,
                    staffInstructions = "Apply 10% off.",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OffersWrite_Succeeds_WhenManageInScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    offerType = "percentage_discount",
                    title = "10% off next visit",
                    description = "Enjoy 10% off your next meal with us.",
                    validity = "30_days_after_issue",
                    discountPercentage = 10m,
                    staffInstructions = "Apply 10% off.",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OffersRecommend_Returns403_WhenStaffHasOnlyScoped()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/1/recommendation",
                seeded.MemberJwt,
                new { locationId = seeded.InScopeLocationId }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OffersRedeem_Returns403_WhenLocationOutsideScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/redeem/check",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.OutOfScopeLocationId,
                    code = "TUM-OUT1",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OffersRedeem_AllowsStaffScopedInScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/redeem/check",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    code = "TUM-MISS1",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<string> SeedTummlyStaffJwtAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var admin = new Admin
            {
                FullName = "Staff",
                Email = $"staff-12-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();
            return jwtService.GenerateAdminToken(admin);
        }

        private async Task<PermissionSeed> SeedOwnerAndMemberAsync(
            string memberRole,
            bool namedInScopeOnly,
            bool seedGuests = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Location Scope Owner",
                Email = $"owner-12-{Guid.NewGuid():N}@example.com",
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
                Name = "Scope Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "Location Scope Member",
                Email = $"member-12-{Guid.NewGuid():N}@example.com",
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
                ? new[] { inScope.Id }
                : new[] { inScope.Id, outOfScope.Id };

            context.RestaurantMemberships.Add(new RestaurantMembership
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
            });
            await context.SaveChangesAsync();

            if (seedGuests)
            {
                await AddGuestAsync(
                    context,
                    restaurant.Id,
                    inScope.Id,
                    "In Scope Guest"
                );
                await AddGuestAsync(
                    context,
                    restaurant.Id,
                    outOfScope.Id,
                    "Out Of Scope Guest"
                );
            }

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new PermissionSeed(
                memberJwt,
                member.Id,
                inScope.Id,
                outOfScope.Id
            );
        }

        private static async Task AddGuestAsync(
            ApplicationDbContext context,
            int restaurantId,
            int locationId,
            string name
        )
        {
            var email = $"{Guid.NewGuid():N}@example.com";
            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = email,
                NormalizedEmail = email,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = locationId,
                Name = name,
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record PermissionSeed(
            string MemberJwt,
            int MemberUserId,
            int InScopeLocationId,
            int OutOfScopeLocationId
        );
    }
}
