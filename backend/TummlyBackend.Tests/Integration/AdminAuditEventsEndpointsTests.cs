using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AdminAuditEventsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminAuditEventsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListAuditEvents_AdminJwt_ReturnsRows()
        {
            var seeded = await SeedWithAuditEventsAsync();
            using var request = AuthorizedGet(
                "/api/admin/audit-events",
                seeded.TummlyAdminJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("totalCount").GetInt32() >= 2);

            var data = body.GetProperty("data");
            Assert.True(data.GetArrayLength() >= 2);
            var actions = data.EnumerateArray()
                .Select(row => row.GetProperty("action").GetString())
                .ToHashSet();
            Assert.Contains(AdminAuditActions.CreditAdjust, actions);
            Assert.Contains(AdminAuditActions.PaymentRefund, actions);
        }

        [Fact]
        public async Task ListAuditEvents_OperatorJwt_Returns403()
        {
            var seeded = await SeedWithAuditEventsAsync();
            using var request = AuthorizedGet(
                "/api/admin/audit-events",
                seeded.OwnerJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListAuditEvents_FiltersByAction()
        {
            var seeded = await SeedWithAuditEventsAsync();
            using var request = AuthorizedGet(
                $"/api/admin/audit-events?action={AdminAuditActions.CreditAdjust}",
                seeded.TummlyAdminJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());

            var data = body.GetProperty("data");
            Assert.True(data.GetArrayLength() >= 1);
            foreach (var row in data.EnumerateArray())
            {
                Assert.Equal(
                    AdminAuditActions.CreditAdjust,
                    row.GetProperty("action").GetString()
                );
            }
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            return request;
        }

        private async Task<Seeded> SeedWithAuditEventsAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Owner",
                Role = "Owner",
                PhoneNumber = "07700900111",
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
                Name = "Audit Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;

            var tummlyAdmin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(tummlyAdmin);
            await context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            context.AdminAuditEvents.AddRange(
                new AdminAuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = now.AddMinutes(-2),
                    Action = AdminAuditActions.CreditAdjust,
                    ActorAdminUserId = tummlyAdmin.Id,
                    ActorIdentity = tummlyAdmin.Email,
                    TargetType = AdminAuditTargetTypes.Restaurant,
                    TargetId = restaurant.Id.ToString(),
                    RestaurantId = restaurant.Id,
                    DetailJson = """{"qty":10}""",
                    Succeeded = true,
                },
                new AdminAuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = now.AddMinutes(-1),
                    Action = AdminAuditActions.PaymentRefund,
                    ActorAdminUserId = tummlyAdmin.Id,
                    ActorIdentity = tummlyAdmin.Email,
                    TargetType = AdminAuditTargetTypes.PaymentOrder,
                    TargetId = $"ord_{Guid.NewGuid():N}",
                    RestaurantId = restaurant.Id,
                    DetailJson = null,
                    Succeeded = true,
                }
            );
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateAdminToken(tummlyAdmin)
            );
        }

        private sealed record Seeded(
            int RestaurantId,
            string OwnerJwt,
            string TummlyAdminJwt
        );
    }
}
