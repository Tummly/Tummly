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
    public class AdminAccountsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string Pricebook = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminAccountsEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task List_WithoutToken_401()
        {
            using var response = await _client.GetAsync("/api/admin/accounts");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task List_AsSupport_403()
        {
            var tokens = await SeedStaffAsync();

            using var response = await GetAsync("/api/admin/accounts", tokens.SupportJwt);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task List_AsAdmin_ReturnsItemsWithTotalCount_AndNoGuestFields()
        {
            var tokens = await SeedStaffAsync();
            var restaurantId = await SeedWorkspaceAsync($"Directory Venue {Guid.NewGuid():N}");

            using var response = await GetAsync(
                $"/api/admin/accounts?search={restaurantId}",
                tokens.AdminJwt
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
            Assert.Equal(50, body.GetProperty("take").GetInt32());
            var row = body.GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("restaurantId").GetInt32() == restaurantId);
            Assert.Equal(restaurantId, row.GetProperty("restaurantId").GetInt32());
            Assert.Equal(BillingSubscriptionPlans.Pilot, row.GetProperty("subscriptionPlan").GetString());
            Assert.Equal(BillingStatuses.Pilot, row.GetProperty("billingStatus").GetString());
            Assert.Equal(1, row.GetProperty("activeLocationCount").GetInt32());
            Assert.Equal("Directory Owner", row.GetProperty("ownerDisplayName").GetString());
            Assert.False(row.TryGetProperty("guests", out _));
        }

        [Fact]
        public async Task List_InvertedCreatedRange_400()
        {
            var tokens = await SeedStaffAsync();

            using var response = await GetAsync(
                "/api/admin/accounts?createdFrom=2026-10-20&createdTo=2026-10-10",
                tokens.AdminJwt
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task List_CreatedToIsInclusiveCalendarDay()
        {
            var tokens = await SeedStaffAsync();
            var day = new DateTime(2026, 10, 10, 15, 30, 0, DateTimeKind.Utc);
            var restaurantId = await SeedWorkspaceAsync(
                $"Dated Venue {Guid.NewGuid():N}",
                createdAt: day
            );

            using var response = await GetAsync(
                "/api/admin/accounts?createdFrom=2026-10-10&createdTo=2026-10-10",
                tokens.AdminJwt
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var ids = body.GetProperty("items").EnumerateArray()
                .Select(row => row.GetProperty("restaurantId").GetInt32())
                .ToList();
            Assert.Contains(restaurantId, ids);
        }

        private Task<HttpResponseMessage> GetAsync(string path, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return _client.SendAsync(request);
        }

        private async Task<int> SeedWorkspaceAsync(
            string name,
            DateTime? createdAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var created = createdAt ?? DateTime.UtcNow;

            var owner = new User
            {
                FullName = "Directory Owner",
                Email = $"directory-{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                Role = "Owner",
                PhoneNumber = "07700900333",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = created,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = name,
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = created,
                BillingAccount = new BillingAccount
                {
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Pilot,
                    ContractedPricebookId = Pricebook,
                    PilotStartedAtUtc = DateTime.UtcNow.AddDays(-5),
                    PilotPeriodEnd = DateTime.UtcNow.AddDays(5),
                },
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                LifecycleStatus = LocationLifecycleStatus.Active,
            });
            await context.SaveChangesAsync();

            return restaurant.Id;
        }

        private async Task<StaffTokens> SeedStaffAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

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

            return new StaffTokens(
                jwtService.GenerateAdminToken(admin),
                jwtService.GenerateAdminToken(support)
            );
        }

        private sealed record StaffTokens(string AdminJwt, string SupportJwt);
    }
}
