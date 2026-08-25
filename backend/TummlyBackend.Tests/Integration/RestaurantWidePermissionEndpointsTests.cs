using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class RestaurantWidePermissionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public RestaurantWidePermissionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AccountWorkspace_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/account-workspace");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspace_Returns403_WhenAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync("NoAccess");

            using var request = Authorized(
                HttpMethod.Get,
                "/api/account-workspace",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspace_Returns200_WhenNamedLocationScopeAndView()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/account-workspace",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspaceMutation_Returns403_WhenView()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing
            );

            using var request = AuthorizedJson(
                HttpMethod.Put,
                "/api/account-workspace/workspace-defaults",
                seeded.MemberJwt,
                new
                {
                    weekStartsOn = "friday",
                    defaultReportingPeriod = "30days",
                    defaultCampaignSenderName = "Harbour",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspaceMutation_Succeeds_WhenManage()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin
            );

            using var request = AuthorizedJson(
                HttpMethod.Put,
                "/api/account-workspace/workspace-defaults",
                seeded.MemberJwt,
                new
                {
                    weekStartsOn = "friday",
                    defaultReportingPeriod = "30days",
                    defaultCampaignSenderName = "Harbour",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Pause_Returns403_WhenTeamManageIsNotOwner()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin
            );

            using var request = Authorized(
                HttpMethod.Post,
                "/api/account-workspace/pause",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GuestDataExport_Returns403_WhenOnlyView()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/account-workspace/guest-data-export?format=csv",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GuestDataExport_DumpsAllLocations_WhenManageAndNamedScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: true,
                seedGuests: true
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/account-workspace/guest-data-export?format=csv",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("In Scope Guest", csv);
            Assert.Contains("Out Of Scope Guest", csv);
        }

        [Fact]
        public async Task GuestsExport_Returns403_WhenSelectedGuestOutsideScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true,
                seedGuests: true
            );

            using var request = Authorized(
                HttpMethod.Get,
                $"/api/guests/export?locationId={seeded.InScopeLocationId}"
                    + $"&guestIds={seeded.OutOfScopeGuestId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task WeeklyBrief_Returns403_WhenReportsIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = Authorized(
                HttpMethod.Get,
                $"/api/home/weekly-brief?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task WeeklyBriefGenerate_Returns403_WhenReportsIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = Authorized(
                HttpMethod.Post,
                $"/api/home/weekly-brief/generate?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task WeeklyBriefGenerate_Returns401_WithoutJwt()
        {
            var response = await _client.PostAsync(
                "/api/home/weekly-brief/generate?locationId=1",
                null
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task HomeRecommendation_ReturnsNone_WhenSourceAreasAreNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true,
                seedGuests: true,
                seedOpenFeedback: true
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.MemberJwt,
                Last7Body(seeded.InScopeLocationId)
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
        }

        [Fact]
        public async Task HomeRecommendation_SkipsType_WhenSourceAreaIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing,
                namedInScopeOnly: true,
                seedGuests: true,
                seedOpenFeedback: true
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.MemberJwt,
                Last7Body(seeded.InScopeLocationId)
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "thank-or-follow-guest",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
        }

        private static Dictionary<string, object?> Last7Body(int locationId)
        {
            return new Dictionary<string, object?>
            {
                ["locationId"] = locationId,
                ["overviewDatePreset"] = "last7",
                ["from"] = "2026-08-15T00:00:00.000Z",
                ["to"] = "2026-08-21T15:30:00.000Z",
                ["refresh"] = true,
            };
        }

        private static HttpRequestMessage Authorized(
            HttpMethod method,
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
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

        private async Task<PermissionSeed> SeedOwnerAndMemberAsync(
            string memberRole,
            bool namedInScopeOnly = false,
            bool seedGuests = false,
            bool seedOpenFeedback = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Restaurant Wide Owner",
                Email = $"owner-13-{Guid.NewGuid():N}@example.com",
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
                Name = "Restaurant Wide Venue",
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
                FullName = "Restaurant Wide Member",
                Email = $"member-13-{Guid.NewGuid():N}@example.com",
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

            int? inScopeGuestId = null;
            int? outOfScopeGuestId = null;
            if (seedGuests)
            {
                inScopeGuestId = await AddGuestAsync(
                    context,
                    restaurant.Id,
                    inScope.Id,
                    "In Scope Guest"
                );
                outOfScopeGuestId = await AddGuestAsync(
                    context,
                    restaurant.Id,
                    outOfScope.Id,
                    "Out Of Scope Guest"
                );
            }

            if (seedOpenFeedback)
            {
                context.Feedbacks.Add(new Feedback
                {
                    RestaurantLocationId = inScope.Id,
                    GuestName = "Alex Guest",
                    GuestContact = "alex@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Great food",
                    WorkflowStatus = FeedbackWorkflowStatus.New,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                });
                await context.SaveChangesAsync();
            }

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new PermissionSeed(
                memberJwt,
                member.Id,
                restaurant.Id,
                inScope.Id,
                outOfScope.Id,
                inScopeGuestId,
                outOfScopeGuestId
            );
        }

        private static async Task<int> AddGuestAsync(
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

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = locationId,
                Name = name,
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();
            return guest.Id;
        }

        private sealed record PermissionSeed(
            string MemberJwt,
            int MemberUserId,
            int RestaurantId,
            int InScopeLocationId,
            int OutOfScopeLocationId,
            int? InScopeGuestId,
            int? OutOfScopeGuestId
        );
    }
}
