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
    public class AdminOpsEndpointsTests : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminOpsEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Overview_AdminJwt_ReturnsEveryCountWithAvailableFlag()
        {
            var adminJwt = await SeedAdminJwtAsync();
            using var request = AuthorizedGet("/api/admin/overview", adminJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data");
            Assert.True(data.TryGetProperty("generatedAtUtc", out _));
            foreach (var metric in new[]
            {
                "shopOrdersPaidNotDispatched",
                "shopOrdersFlaggedForReview",
                "printAssetsFailed",
                "pilotsEndingWithin7Days",
                "openHelpCentreQueries",
                "failedAuditEventsLast7Days",
            })
            {
                Assert.True(
                    data.GetProperty(metric).GetProperty("available").GetBoolean(),
                    metric
                );
            }
        }

        [Fact]
        public async Task Overview_OwnerJwt_Returns403()
        {
            using var request = AuthorizedGet("/api/admin/overview", OwnerJwt());

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Environment_AdminJwt_ReportsNonProductionTestingEnvironment()
        {
            var adminJwt = await SeedAdminJwtAsync();
            using var request = AuthorizedGet("/api/admin/environment", adminJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data");
            Assert.Equal("Testing", data.GetProperty("environmentName").GetString());
            Assert.False(data.GetProperty("isProduction").GetBoolean());
            Assert.Equal("unknown", data.GetProperty("commitSha").GetString());
        }

        [Fact]
        public async Task Environment_OwnerJwt_Returns403()
        {
            using var request = AuthorizedGet("/api/admin/environment", OwnerJwt());

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AuditEvents_FiltersByTargetId_AndExposesCorrelationIdField()
        {
            var adminJwt = await SeedAdminJwtAsync();
            var targetId = $"order-{Guid.NewGuid():N}";
            await SeedAuditRowAsync(targetId);
            await SeedAuditRowAsync($"order-{Guid.NewGuid():N}");

            using var request = AuthorizedGet(
                $"/api/admin/audit-events?targetId={targetId}",
                adminJwt
            );

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            var row = Assert.Single(body.GetProperty("data").EnumerateArray());
            Assert.Equal(targetId, row.GetProperty("targetId").GetString());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("correlationId").ValueKind);
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private string OwnerJwt()
        {
            using var scope = _factory.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            return jwtService.GenerateToken("999", "owner@example.com", "Owner");
        }

        private async Task<string> SeedAdminJwtAsync()
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
            context.Admins.Add(admin);
            await context.SaveChangesAsync();

            return jwtService.GenerateAdminToken(admin);
        }

        private async Task SeedAuditRowAsync(string targetId)
        {
            using var scope = _factory.Services.CreateScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAdminAuditService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            audit.Append(new AdminAuditAppendRequest(
                AdminAuditActions.PaymentRefund,
                "admin@tummly.com",
                AdminAuditTargetTypes.PaymentOrder,
                targetId
            ));
            await context.SaveChangesAsync();
        }
    }
}
