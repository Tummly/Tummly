using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class HomeScopeEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string PerformanceFrom = "2026-07-10T00:00:00Z";
        private const string PerformanceTo = "2026-07-17T00:00:00Z";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeScopeEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task LatestActivity_MasksContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await GetAsync(
                seeded.OwnerJwt,
                LatestActivityUrl(seeded.InScopeLocationId)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(seeded.GuestEmail, raw);
            Assert.DoesNotContain(seeded.GuestMobile, raw);

            var feedbackItems = ItemsOfKind(raw, "feedback");
            Assert.Equal(2, feedbackItems.Count);
            Assert.Contains(
                feedbackItems,
                item => item.GetProperty("guestContact").GetString()
                    == GuestContactMask.MaskEmail(seeded.GuestEmail)
            );
            Assert.Contains(
                feedbackItems,
                item => item.GetProperty("guestContact").GetString()
                    == GuestContactMask.MaskPhone(seeded.GuestMobile)
            );
        }

        [Fact]
        public async Task LatestActivity_UnassignedLocation_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                LatestActivityUrl(seeded.OutOfScopeLocationId)
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task LatestActivity_OtherTenant_Denied()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                LatestActivityUrl(seeded.OtherTenantLocationId)
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task LatestActivity_MarketingRole_OnlyGuestJoinedItems()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Marketing
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                LatestActivityUrl(seeded.InScopeLocationId)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var raw = await response.Content.ReadAsStringAsync();
            var items = Items(raw);
            Assert.NotEmpty(items);
            Assert.All(items, item =>
                Assert.Equal(
                    "guest-joined",
                    item.GetProperty("kind").GetString()
                )
            );
        }

        [Fact]
        public async Task LatestActivity_StaffRole_EmptyItems()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                LatestActivityUrl(seeded.InScopeLocationId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(Items(await response.Content.ReadAsStringAsync()));
        }

        [Fact]
        public async Task Performance_StaffRole_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                PerformanceUrl(seeded.InScopeLocationId)
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Performance_LocationManager_Assigned_200()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                PerformanceUrl(seeded.InScopeLocationId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChecklistAcks_UnassignedLocation_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                $"/api/operator-home/checklist-acks?locationId={seeded.OutOfScopeLocationId}"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<HttpResponseMessage> GetAsync(string jwt, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private static string LatestActivityUrl(int locationId)
        {
            return $"/api/home/latest-activity?locationId={locationId}";
        }

        private static string PerformanceUrl(int locationId)
        {
            return $"/api/home/performance?locationId={locationId}"
                + $"&from={PerformanceFrom}&to={PerformanceTo}";
        }

        private static List<JsonElement> Items(string raw)
        {
            return JsonDocument.Parse(raw).RootElement
                .GetProperty("items")
                .EnumerateArray()
                .ToList();
        }

        private static List<JsonElement> ItemsOfKind(string raw, string kind)
        {
            return Items(raw)
                .Where(item => item.GetProperty("kind").GetString() == kind)
                .ToList();
        }
    }
}
