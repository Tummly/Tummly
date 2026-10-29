using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Admin review of Locations saved as Draft for a possible duplicate
    /// premises. Admin sees the matched Locations; the operator never does.
    /// </summary>
    public class AdminPremisesReviewEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminPremisesReviewEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task List_AsAdmin_ShowsPendingLocation_WithMatchedLocationAcrossWorkspaces()
        {
            var scenario = await SeedFlaggedAsync();

            using var response = await SendAsync(
                HttpMethod.Get,
                "/api/admin/premises-reviews",
                scenario.AdminJwt
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var item = body.GetProperty("items")
                .EnumerateArray()
                .Single(row => row.GetProperty("locationId").GetInt32() == scenario.FlaggedLocationId);
            Assert.Equal(scenario.Ours.RestaurantId, item.GetProperty("restaurantId").GetInt32());
            Assert.Equal("Review Our Cafe", item.GetProperty("locationName").GetString());
            var match = Assert.Single(item.GetProperty("matches").EnumerateArray());
            Assert.Equal(scenario.TheirLocationId, match.GetProperty("locationId").GetInt32());
            Assert.Equal(scenario.Theirs.RestaurantId, match.GetProperty("restaurantId").GetInt32());
            Assert.Equal("Review Rival Cafe", match.GetProperty("locationName").GetString());
            Assert.False(match.GetProperty("sameWorkspace").GetBoolean());
            Assert.True(match.GetProperty("premisesKeyMatch").GetBoolean());
        }

        [Fact]
        public async Task List_DoesNotShowResolvedLocations()
        {
            var scenario = await SeedFlaggedAsync();
            using var clear = await ResolveAsync(
                scenario.FlaggedLocationId,
                "clear",
                scenario.AdminJwt,
                "Different business, confirmed by phone."
            );
            Assert.Equal(HttpStatusCode.OK, clear.StatusCode);

            using var response = await SendAsync(
                HttpMethod.Get,
                "/api/admin/premises-reviews",
                scenario.AdminJwt
            );

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.DoesNotContain(
                body.GetProperty("items").EnumerateArray(),
                row => row.GetProperty("locationId").GetInt32() == scenario.FlaggedLocationId
            );
        }

        [Theory]
        [InlineData("list")]
        [InlineData("clear")]
        [InlineData("reject")]
        public async Task NonAdmin_IsDenied_AndNothingChanges(string endpoint)
        {
            var scenario = await SeedFlaggedAsync();

            foreach (var jwt in new[] { scenario.SupportJwt, scenario.Ours.OwnerJwt })
            {
                using var response = endpoint == "list"
                    ? await SendAsync(HttpMethod.Get, "/api/admin/premises-reviews", jwt)
                    : await ResolveAsync(scenario.FlaggedLocationId, endpoint, jwt, "Because.");
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }

            using var anonymous = endpoint == "list"
                ? await SendAsync(HttpMethod.Get, "/api/admin/premises-reviews", null)
                : await ResolveAsync(scenario.FlaggedLocationId, endpoint, null, "Because.");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

            var location = await LoadLocationAsync(scenario.FlaggedLocationId);
            Assert.Equal(PremisesReviewStatus.PendingReview, location.PremisesReviewStatus);
            Assert.Equal(0, await CountAuditAsync(scenario.FlaggedLocationId, AdminAuditActions.LocationPremisesCleared));
            Assert.Equal(0, await CountAuditAsync(scenario.FlaggedLocationId, AdminAuditActions.LocationPremisesRejected));
        }

        [Fact]
        public async Task Clear_WritesAuditRow_AndOperatorCanThenActivate()
        {
            var scenario = await SeedFlaggedAsync();

            using var blocked = await ActivateAsync(scenario);
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Contains(
                PremisesReviewGate.BlockedMessage,
                await blocked.Content.ReadAsStringAsync()
            );

            using var clear = await ResolveAsync(
                scenario.FlaggedLocationId,
                "clear",
                scenario.AdminJwt,
                "Different business, confirmed by phone."
            );
            Assert.Equal(HttpStatusCode.OK, clear.StatusCode);

            var location = await LoadLocationAsync(scenario.FlaggedLocationId);
            Assert.Equal(PremisesReviewStatus.Cleared, location.PremisesReviewStatus);
            Assert.Equal(scenario.AdminId, location.PremisesReviewedByAdminUserId);
            Assert.NotNull(location.PremisesReviewedAtUtc);
            Assert.Equal("Different business, confirmed by phone.", location.PremisesReviewReason);
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);

            var audit = await SingleAuditAsync(
                scenario.FlaggedLocationId,
                AdminAuditActions.LocationPremisesCleared
            );
            Assert.Equal(scenario.AdminId, audit.ActorAdminUserId);
            Assert.Equal(AdminAuditTargetTypes.RestaurantLocation, audit.TargetType);
            Assert.Equal(scenario.Ours.RestaurantId, audit.RestaurantId);
            Assert.Contains("Different business", audit.DetailJson);

            using var activated = await ActivateAsync(scenario);
            Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
            var after = await LoadLocationAsync(scenario.FlaggedLocationId);
            Assert.Equal(LocationLifecycleStatus.Active, after.LifecycleStatus);
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(
                await context.QrCodes.AnyAsync(qr => qr.RestaurantLocationId == scenario.FlaggedLocationId)
            );
        }

        [Fact]
        public async Task Reject_WritesAuditRow_AndLocationStaysBlocked()
        {
            var scenario = await SeedFlaggedAsync();

            using var reject = await ResolveAsync(
                scenario.FlaggedLocationId,
                "reject",
                scenario.AdminJwt,
                "Premises already holds a Pilot."
            );
            Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

            var location = await LoadLocationAsync(scenario.FlaggedLocationId);
            Assert.Equal(PremisesReviewStatus.Rejected, location.PremisesReviewStatus);
            Assert.Equal(scenario.AdminId, location.PremisesReviewedByAdminUserId);
            var audit = await SingleAuditAsync(
                scenario.FlaggedLocationId,
                AdminAuditActions.LocationPremisesRejected
            );
            Assert.Contains("already holds a Pilot", audit.DetailJson);

            using var activated = await ActivateAsync(scenario);
            Assert.Equal(HttpStatusCode.Conflict, activated.StatusCode);
            var after = await LoadLocationAsync(scenario.FlaggedLocationId);
            Assert.Equal(LocationLifecycleStatus.Draft, after.LifecycleStatus);
            Assert.Equal(0, await CountOrdersAsync(scenario.Ours.RestaurantId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Resolve_WithoutReason_IsRejected_NoChangeNoAudit(string? reason)
        {
            var scenario = await SeedFlaggedAsync();

            foreach (var endpoint in new[] { "clear", "reject" })
            {
                using var response = await ResolveAsync(
                    scenario.FlaggedLocationId,
                    endpoint,
                    scenario.AdminJwt,
                    reason
                );
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            var location = await LoadLocationAsync(scenario.FlaggedLocationId);
            Assert.Equal(PremisesReviewStatus.PendingReview, location.PremisesReviewStatus);
            Assert.Equal(0, await CountAuditAsync(scenario.FlaggedLocationId, AdminAuditActions.LocationPremisesCleared));
            Assert.Equal(0, await CountAuditAsync(scenario.FlaggedLocationId, AdminAuditActions.LocationPremisesRejected));
        }

        [Fact]
        public async Task Resolve_AlreadyResolved_IsConflict_AndWritesNoSecondAudit()
        {
            var scenario = await SeedFlaggedAsync();
            using var first = await ResolveAsync(scenario.FlaggedLocationId, "reject", scenario.AdminJwt, "No.");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            using var second = await ResolveAsync(scenario.FlaggedLocationId, "clear", scenario.AdminJwt, "Changed mind.");

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            Assert.Equal(PremisesReviewStatus.Rejected, (await LoadLocationAsync(scenario.FlaggedLocationId)).PremisesReviewStatus);
            Assert.Equal(0, await CountAuditAsync(scenario.FlaggedLocationId, AdminAuditActions.LocationPremisesCleared));
        }

        [Fact]
        public async Task Resolve_UnknownLocation_IsNotFound()
        {
            var scenario = await SeedFlaggedAsync();

            using var response = await ResolveAsync(int.MaxValue, "clear", scenario.AdminJwt, "Because.");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Operator_SeesStatusOnTheLocation_ButNoMatchData()
        {
            var scenario = await SeedFlaggedAsync();

            using var list = await SendAsync(
                HttpMethod.Get,
                "/api/locations?pageSize=50",
                scenario.Ours.OwnerJwt
            );
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var listRaw = await list.Content.ReadAsStringAsync();
            var row = JsonDocument.Parse(listRaw).RootElement.GetProperty("rows")
                .EnumerateArray()
                .Single(r => r.GetProperty("id").GetInt32() == scenario.FlaggedLocationId);
            Assert.Equal("pending_review", row.GetProperty("premisesReviewStatus").GetString());

            using var detail = await SendAsync(
                HttpMethod.Get,
                $"/api/locations/{scenario.FlaggedLocationId}/detail",
                scenario.Ours.OwnerJwt
            );
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var detailRaw = await detail.Content.ReadAsStringAsync();
            var header = JsonDocument.Parse(detailRaw).RootElement.GetProperty("header");
            Assert.Equal("pending_review", header.GetProperty("premisesReviewStatus").GetString());

            foreach (var raw in new[] { listRaw, detailRaw })
            {
                Assert.DoesNotContain("Review Rival Cafe", raw, StringComparison.Ordinal);
                Assert.DoesNotContain("matches", raw, StringComparison.Ordinal);
                Assert.DoesNotContain("priorPilot", raw, StringComparison.Ordinal);
            }
        }

        // --- arrange / act helpers ---

        private async Task<Scenario> SeedFlaggedAsync()
        {
            var theirs = await SeedWorkspaceAsync("Review Theirs");
            var ours = await SeedWorkspaceAsync("Review Ours");
            var address = $"{Random.Shared.Next(100, 999)} Review Street";
            var postcode = "RV1 1AA";

            int theirLocationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var theirLocation = new RestaurantLocation
                {
                    RestaurantId = theirs.RestaurantId,
                    LocationName = "Review Rival Cafe",
                    Address = address,
                    City = "Reviewton",
                    Postcode = postcode,
                    LifecycleStatus = LocationLifecycleStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                };
                context.RestaurantLocations.Add(theirLocation);
                await context.SaveChangesAsync();
                theirLocationId = theirLocation.Id;
            }

            using var add = new HttpRequestMessage(HttpMethod.Post, "/api/locations")
            {
                Content = JsonContent.Create(new
                {
                    locationName = "Review Our Cafe",
                    address,
                    city = "Reviewton",
                    postcode,
                }),
            };
            add.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ours.OwnerJwt);
            var created = await _client.SendAsync(add);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var flaggedId = (await created.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("locationId")
                .GetInt32();

            using var adminScope = _factory.Services.CreateScope();
            var db = adminScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwt = adminScope.ServiceProvider.GetRequiredService<IJwtService>();
            var admin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            var support = new Admin
            {
                FullName = "Tummly Support",
                Email = $"support-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Support",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Admins.AddRange(admin, support);
            await db.SaveChangesAsync();

            return new Scenario(
                ours,
                theirs,
                flaggedId,
                theirLocationId,
                admin.Id,
                jwt.GenerateAdminToken(admin),
                jwt.GenerateAdminToken(support)
            );
        }

        private Task<HttpResponseMessage> ActivateAsync(Scenario scenario) =>
            SendAsync(
                HttpMethod.Post,
                $"/api/locations/{scenario.FlaggedLocationId}/activate",
                scenario.Ours.OwnerJwt,
                new { }
            );

        private Task<HttpResponseMessage> ResolveAsync(
            int locationId,
            string action,
            string? jwt,
            string? reason
        ) =>
            SendAsync(
                HttpMethod.Post,
                $"/api/admin/premises-reviews/{locationId}/{action}",
                jwt,
                new { reason }
            );

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            string? jwt,
            object? payload = null
        )
        {
            var request = new HttpRequestMessage(method, path);
            if (jwt != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }

            if (payload != null)
            {
                request.Content = JsonContent.Create(payload);
            }

            return await _client.SendAsync(request);
        }

        private async Task<RestaurantLocation> LoadLocationAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == locationId);
        }

        private async Task<int> CountAuditAsync(int locationId, string action)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.AdminAuditEvents.CountAsync(row =>
                row.Action == action && row.TargetId == locationId.ToString()
            );
        }

        private async Task<AdminAuditEvent> SingleAuditAsync(int locationId, string action)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.AdminAuditEvents
                .AsNoTracking()
                .SingleAsync(row => row.Action == action && row.TargetId == locationId.ToString());
        }

        private async Task<int> CountOrdersAsync(int restaurantId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.ShopOrders.CountAsync(row =>
                row.RestaurantId == restaurantId && row.IsComplimentary
            );
        }

        private async Task<Workspace> SeedWorkspaceAsync(string name)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = $"Owner {name}",
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
                Name = name,
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                CurrentPricebookId
            );
            billing.SubscriptionPlan = BillingSubscriptionPlans.Group;
            billing.BillingStatus = BillingStatuses.Active;
            context.BillingAccounts.Add(billing);

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

            return new Workspace(
                restaurant.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role)
            );
        }

        private sealed record Workspace(int RestaurantId, string OwnerJwt);

        private sealed record Scenario(
            Workspace Ours,
            Workspace Theirs,
            int FlaggedLocationId,
            int TheirLocationId,
            int AdminId,
            string AdminJwt,
            string SupportJwt
        );
    }
}
