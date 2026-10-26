using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class GuestDataExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string ExportUrl =
            "/api/account-workspace/guest-data-export?format=csv";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestDataExportEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GuestDataExport_Owner_200_WritesOneAuditWithoutContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Admin
            );

            var response = await GetAsync(seeded.OwnerJwt, ExportUrl);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var audits = await ReadGuestDataExportAuditsAsync(
                seeded.RestaurantId
            );
            var audit = Assert.Single(audits);
            Assert.Equal(
                $"user:{seeded.OwnerUserId}",
                audit.ActorIdentity
            );
            Assert.Equal(AdminAuditTargetTypes.Restaurant, audit.TargetType);
            Assert.Equal(seeded.RestaurantId.ToString(), audit.TargetId);
            Assert.Equal(seeded.RestaurantId, audit.RestaurantId);
            Assert.NotNull(audit.DetailJson);
            Assert.DoesNotContain(seeded.GuestEmail, audit.DetailJson);
            Assert.DoesNotContain(seeded.GuestMobile, audit.DetailJson);

            var detail = JsonDocument.Parse(audit.DetailJson!).RootElement;
            Assert.Equal("csv", detail.GetProperty("format").GetString());
            Assert.Equal(3, detail.GetProperty("rows").GetInt32());
        }

        [Fact]
        public async Task GuestDataExport_AdminWithManage_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Admin
            );

            var response = await GetAsync(seeded.MemberJwt, ExportUrl);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                "Only the account owner can export guest data.",
                await ReadMessageAsync(response)
            );
            Assert.Empty(
                await ReadGuestDataExportAuditsAsync(seeded.RestaurantId)
            );
        }

        [Fact]
        public async Task GuestDataExport_Staff_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await GetAsync(seeded.MemberJwt, ExportUrl);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(
                await ReadGuestDataExportAuditsAsync(seeded.RestaurantId)
            );
        }

        [Fact]
        public async Task GuestDataExport_Owner_ContainsFullContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Admin
            );

            var response = await GetAsync(seeded.OwnerJwt, ExportUrl);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains(seeded.GuestEmail, csv);
            Assert.Contains(seeded.GuestMobile, csv);
        }

        private async Task<HttpResponseMessage> GetAsync(string jwt, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private static async Task<string> ReadMessageAsync(
            HttpResponseMessage response
        )
        {
            var json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync()
            );
            return json.RootElement.GetProperty("message").GetString() ?? string.Empty;
        }

        private async Task<List<AdminAuditEvent>> ReadGuestDataExportAuditsAsync(
            int restaurantId
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await context.AdminAuditEvents
                .AsNoTracking()
                .Where(e =>
                    e.Action == AdminAuditActions.GuestDataExport
                    && e.RestaurantId == restaurantId
                )
                .ToListAsync();
        }
    }
}
