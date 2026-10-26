using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AdminPilotExtensionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminPilotExtensionEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Extend_AsAdmin_200_WritesOneAuditRow()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                seeded.OwnerUserId,
                days: 14,
                key: $"ep-{Guid.NewGuid():N}",
                jwt: seeded.AdminJwt
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            var data = body.GetProperty("data");
            Assert.Equal(seeded.RestaurantId, data.GetProperty("restaurantId").GetInt32());
            Assert.Equal(1, data.GetProperty("extensionCount").GetInt32());
            Assert.Equal(14, data.GetProperty("extensionDaysTotal").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rows = await context.AdminAuditEvents
                .AsNoTracking()
                .Where(row =>
                    row.Action == AdminAuditActions.OperatorExtendPilot
                    && row.RestaurantId == seeded.RestaurantId
                )
                .ToListAsync();
            var row = Assert.Single(rows);
            Assert.Equal(AdminAuditTargetTypes.OperatorUser, row.TargetType);
            Assert.Equal(seeded.OwnerUserId.ToString(), row.TargetId);
            Assert.Contains("\"countAfter\":1", row.DetailJson ?? string.Empty);
            Assert.DoesNotContain(seeded.OwnerEmail, row.DetailJson ?? string.Empty);
            Assert.DoesNotContain("Pilot Owner", row.DetailJson ?? string.Empty);
        }

        [Fact]
        public async Task Extend_SameKeyTwice_WritesOneAuditRow()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );
            var key = $"ep-{Guid.NewGuid():N}";

            using var first = await PostExtendAsync(seeded.OwnerUserId, 14, key, seeded.AdminJwt);
            using var second = await PostExtendAsync(seeded.OwnerUserId, 14, key, seeded.AdminJwt);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, secondBody.GetProperty("data").GetProperty("extensionCount").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await context.AdminAuditEvents.CountAsync(row =>
                row.Action == AdminAuditActions.OperatorExtendPilot
                && row.RestaurantId == seeded.RestaurantId
            );
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Extend_AsSupport_403()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                seeded.OwnerUserId,
                14,
                $"ep-{Guid.NewGuid():N}",
                seeded.SupportJwt
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Extend_Days7_400()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                seeded.OwnerUserId,
                7,
                $"ep-{Guid.NewGuid():N}",
                seeded.AdminJwt
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("pilot_extension_invalid_days", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Extend_Third_409()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 2,
                extensionDaysTotal: 28
            );

            using var response = await PostExtendAsync(
                seeded.OwnerUserId,
                14,
                $"ep-{Guid.NewGuid():N}",
                seeded.AdminJwt
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("pilot_extension_limit", body.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        }

        [Fact]
        public async Task Extend_PaidAccount_409_NotAllowed()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                seeded.OwnerUserId,
                14,
                $"ep-{Guid.NewGuid():N}",
                seeded.AdminJwt
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("pilot_extension_not_allowed", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Extend_UnknownOperator_404()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                int.MaxValue - 7,
                14,
                $"ep-{Guid.NewGuid():N}",
                seeded.AdminJwt
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Extend_WithRestaurantId_ExtendsThatRestaurant()
        {
            var owner = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );
            var secondRestaurantId = await AddPilotRestaurantAsync(owner.OwnerUserId);

            using var response = await PostExtendAsync(
                owner.OwnerUserId,
                14,
                $"ep-{Guid.NewGuid():N}",
                owner.AdminJwt,
                restaurantId: secondRestaurantId
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var data = body.GetProperty("data");
            Assert.Equal(secondRestaurantId, data.GetProperty("restaurantId").GetInt32());
            Assert.Equal(1, data.GetProperty("extensionCount").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var first = await context.BillingAccounts.AsNoTracking()
                .SingleAsync(row => row.RestaurantId == owner.RestaurantId);
            var second = await context.BillingAccounts.AsNoTracking()
                .SingleAsync(row => row.RestaurantId == secondRestaurantId);
            Assert.Equal(0, first.PilotExtensionCount);
            Assert.Equal(1, second.PilotExtensionCount);
        }

        [Fact]
        public async Task Extend_WithForeignRestaurantId_404()
        {
            var owner = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );
            var foreign = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                owner.OwnerUserId,
                14,
                $"ep-{Guid.NewGuid():N}",
                owner.AdminJwt,
                restaurantId: foreign.RestaurantId
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var billing = await context.BillingAccounts.AsNoTracking()
                .SingleAsync(row => row.RestaurantId == foreign.RestaurantId);
            Assert.Equal(0, billing.PilotExtensionCount);
        }

        [Fact]
        public async Task ListPilotAccounts_ReturnsPilotOnly_WithCanExtend()
        {
            var pilot = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );
            var exhausted = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.SoftLock,
                extensionCount: 2,
                extensionDaysTotal: 28
            );
            var paid = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/pilot-accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pilot.AdminJwt);
            using var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            var rows = body.GetProperty("data").EnumerateArray()
                .ToDictionary(row => row.GetProperty("restaurantId").GetInt32());

            Assert.DoesNotContain(paid.RestaurantId, rows.Keys);

            Assert.True(rows[pilot.RestaurantId].GetProperty("canExtend").GetBoolean());
            Assert.Equal(JsonValueKind.Null, rows[pilot.RestaurantId].GetProperty("cannotExtendReason").ValueKind);

            Assert.False(rows[exhausted.RestaurantId].GetProperty("canExtend").GetBoolean());
            Assert.Equal(
                "pilot_extension_limit",
                rows[exhausted.RestaurantId].GetProperty("cannotExtendReason").GetString()
            );
        }

        private Task<HttpResponseMessage> PostExtendAsync(
            int userId,
            int days,
            string key,
            string jwt,
            int? restaurantId = null
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/admin/operators/{userId}/extend-activation"
            )
            {
                Content = JsonContent.Create(
                    new { days, idempotencyKey = key, restaurantId }
                ),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return _client.SendAsync(request);
        }

        private async Task<int> AddPilotRestaurantAsync(int ownerUserId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var restaurant = new Restaurant
            {
                Name = $"Pilot Venue {Guid.NewGuid():N}",
                AccountType = "Single",
                OwnerUserId = ownerUserId,
                BillingContactUserId = ownerUserId,
                PrivacyContactUserId = ownerUserId,
                SupportContactUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Pilot,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                    PilotStartedAtUtc = DateTime.UtcNow.AddDays(-5),
                    PilotPeriodEnd = DateTime.UtcNow.AddDays(5),
                    PilotExtensionCount = 0,
                    PilotExtensionDaysTotal = 0,
                }
            );
            await context.SaveChangesAsync();

            return restaurant.Id;
        }

        private async Task<SeededPilot> SeedPilotAsync(
            string plan,
            string status,
            int extensionCount,
            int extensionDaysTotal
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var ownerEmail = $"pilot-{Guid.NewGuid():N}@example.com";
            var owner = new User
            {
                FullName = "Pilot Owner",
                Email = ownerEmail,
                PasswordHash = "x",
                Role = "Owner",
                PhoneNumber = "07700900222",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow.AddDays(-5),
                ActivationExpiresAt = DateTime.UtcNow.AddDays(5),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Pilot Venue {Guid.NewGuid():N}",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = plan,
                    BillingStatus = status,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                    PilotStartedAtUtc = plan == BillingSubscriptionPlans.Pilot
                        ? DateTime.UtcNow.AddDays(-5)
                        : null,
                    PilotPeriodEnd = plan == BillingSubscriptionPlans.Pilot
                        ? DateTime.UtcNow.AddDays(5)
                        : null,
                    PilotExtensionCount = extensionCount,
                    PilotExtensionDaysTotal = extensionDaysTotal,
                }
            );
            await context.SaveChangesAsync();

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
            context.Admins.AddRange(admin, support);
            await context.SaveChangesAsync();

            return new SeededPilot(
                restaurant.Id,
                owner.Id,
                ownerEmail,
                jwtService.GenerateAdminToken(admin),
                jwtService.GenerateAdminToken(support)
            );
        }

        private sealed record SeededPilot(
            int RestaurantId,
            int OwnerUserId,
            string OwnerEmail,
            string AdminJwt,
            string SupportJwt
        );
    }
}
