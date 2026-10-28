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
        private const string ValidReason = "Support asked for a delay";

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
        public async Task Extend_Success_AuditDetailCarriesReason()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0
            );

            using var response = await PostExtendAsync(
                seeded.OwnerUserId,
                days: 30,
                key: $"ep-{Guid.NewGuid():N}",
                jwt: seeded.AdminJwt,
                reason: "  Venue closed for refit  "
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var row = Assert.Single(await PilotAuditRowsAsync(seeded.RestaurantId));
            Assert.True(row.Succeeded);
            Assert.Contains("\"reason\":\"Venue closed for refit\"", row.DetailJson ?? string.Empty);
            Assert.Contains("\"days\":30", row.DetailJson ?? string.Empty);
        }

        [Fact]
        public async Task Extend_MissingReason_400_AuditsRefusalWithoutChange()
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
                seeded.AdminJwt,
                reason: null
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("pilot_extension_invalid_reason", body.GetProperty("code").GetString());

            var row = Assert.Single(await PilotAuditRowsAsync(seeded.RestaurantId));
            Assert.False(row.Succeeded);
            Assert.Contains("\"code\":\"pilot_extension_invalid_reason\"", row.DetailJson ?? string.Empty);
            Assert.Equal(0, await BillingExtensionCountAsync(seeded.RestaurantId));
        }

        [Fact]
        public async Task Extend_StaleVersion_409_AuditsRefusalWithoutChange()
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
                seeded.AdminJwt,
                expectedExtensionCount: 1
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("stale_pilot_version", body.GetProperty("code").GetString());

            var row = Assert.Single(await PilotAuditRowsAsync(seeded.RestaurantId));
            Assert.False(row.Succeeded);
            Assert.Contains("\"code\":\"stale_pilot_version\"", row.DetailJson ?? string.Empty);
            Assert.Equal(0, await BillingExtensionCountAsync(seeded.RestaurantId));
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

            var row = Assert.Single(await PilotAuditRowsAsync(seeded.RestaurantId));
            Assert.False(row.Succeeded);
            Assert.Contains("\"code\":\"pilot_extension_invalid_days\"", row.DetailJson ?? string.Empty);
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
                seeded.AdminJwt,
                expectedExtensionCount: 2
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("pilot_extension_limit", body.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));

            var row = Assert.Single(await PilotAuditRowsAsync(seeded.RestaurantId));
            Assert.False(row.Succeeded);
            Assert.Contains("\"code\":\"pilot_extension_limit\"", row.DetailJson ?? string.Empty);
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
        public async Task Extend_PostCancelAccount_409_NotAllowed()
        {
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.SoftLock,
                extensionCount: 0,
                extensionDaysTotal: 0,
                renewalDateUtc: DateTime.UtcNow.AddDays(-2)
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
        public async Task ListPilotAccounts_PostCancelAccount_CannotExtend_WithHumanReason()
        {
            var postCancel = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.SoftLock,
                extensionCount: 0,
                extensionDaysTotal: 0,
                renewalDateUtc: DateTime.UtcNow.AddDays(-2)
            );

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/pilot-accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", postCancel.AdminJwt);
            using var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var rows = body.GetProperty("data").EnumerateArray()
                .ToDictionary(row => row.GetProperty("restaurantId").GetInt32());
            var row = rows[postCancel.RestaurantId];
            Assert.False(row.GetProperty("canExtend").GetBoolean());
            Assert.Equal(
                "Only a Pilot that is active or locked, with no paid plan history, can be extended.",
                row.GetProperty("cannotExtendReason").GetString()
            );
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
            var reason = rows[exhausted.RestaurantId].GetProperty("cannotExtendReason").GetString();
            Assert.StartsWith("This Pilot has used its extensions.", reason);
        }

        [Fact]
        public async Task ListPilotAccounts_ShowsOriginalEndHistoryAndAllowances()
        {
            var originalEnd = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPilotAsync(
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                extensionCount: 0,
                extensionDaysTotal: 0,
                originalEndUtc: originalEnd
            );
            using (var extend = await PostExtendAsync(
                seeded.OwnerUserId,
                14,
                $"ep-{Guid.NewGuid():N}",
                seeded.AdminJwt,
                reason: "Guest support asked for a delay"
            ))
            {
                Assert.Equal(HttpStatusCode.OK, extend.StatusCode);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/pilot-accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.AdminJwt);
            using var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var row = body.GetProperty("data").EnumerateArray()
                .Single(item => item.GetProperty("restaurantId").GetInt32() == seeded.RestaurantId);

            Assert.Equal(originalEnd, row.GetProperty("pilotOriginalEndUtc").GetDateTime());
            Assert.NotEqual(originalEnd, row.GetProperty("pilotPeriodEnd").GetDateTime());
            Assert.Equal(5, row.GetProperty("daysUsed").GetInt32());
            Assert.False(row.GetProperty("hasPaidHistory").GetBoolean());
            Assert.Equal(1, row.GetProperty("extensionCount").GetInt32());

            var history = Assert.Single(row.GetProperty("extensionHistory").EnumerateArray());
            Assert.Equal(14, history.GetProperty("days").GetInt32());
            Assert.Equal("Guest support asked for a delay", history.GetProperty("reason").GetString());
            Assert.False(string.IsNullOrWhiteSpace(history.GetProperty("actorIdentity").GetString()));

            var allowances = row.GetProperty("allowances");
            Assert.Equal(JsonValueKind.Array, allowances.ValueKind);
            Assert.Equal(
                new[] { "ai", "email", "sms" },
                allowances.EnumerateArray()
                    .Select(item => item.GetProperty("channel").GetString())
                    .OrderBy(channel => channel)
                    .ToArray()
            );
        }

        private async Task<List<AdminAuditEvent>> PilotAuditRowsAsync(int restaurantId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.AdminAuditEvents
                .AsNoTracking()
                .Where(row =>
                    row.Action == AdminAuditActions.OperatorExtendPilot
                    && row.RestaurantId == restaurantId
                )
                .OrderBy(row => row.OccurredAtUtc)
                .ToListAsync();
        }

        private async Task<int> BillingExtensionCountAsync(int restaurantId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (await context.BillingAccounts.AsNoTracking()
                .SingleAsync(row => row.RestaurantId == restaurantId))
                .PilotExtensionCount;
        }

        private Task<HttpResponseMessage> PostExtendAsync(
            int userId,
            int days,
            string key,
            string jwt,
            int? restaurantId = null,
            string? reason = ValidReason,
            int expectedExtensionCount = 0
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/admin/operators/{userId}/extend-activation"
            )
            {
                Content = JsonContent.Create(
                    new
                    {
                        days,
                        idempotencyKey = key,
                        restaurantId,
                        reason,
                        expectedExtensionCount,
                    }
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
            int extensionDaysTotal,
            DateTime? renewalDateUtc = null,
            DateTime? originalEndUtc = null
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
                    PilotOriginalEndUtc = plan == BillingSubscriptionPlans.Pilot
                        ? originalEndUtc
                        : null,
                    PilotExtensionCount = extensionCount,
                    PilotExtensionDaysTotal = extensionDaysTotal,
                    RenewalDateUtc = renewalDateUtc,
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
