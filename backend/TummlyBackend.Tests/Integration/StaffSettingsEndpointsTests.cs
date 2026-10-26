using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// WS2b Task 9: Staff have no Account &amp; workspace Settings; pause and
    /// resume the workspace need Manage; Staff keep Offers redeem.
    /// </summary>
    public class StaffSettingsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StaffSettingsEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task StaffLocationsPayload_HidesSettingsChrome()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                "/api/restaurant/locations"
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "none",
                body.GetProperty("locationsAccess").GetString()
            );
            Assert.Equal(
                "none",
                body.GetProperty("accountWorkspaceAccess").GetString()
            );
            Assert.True(body.GetProperty("locations").GetArrayLength() > 0);
        }

        [Fact]
        public async Task OwnerLocationsPayload_KeepsManageChrome()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await GetAsync(
                seeded.OwnerJwt,
                "/api/restaurant/locations"
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "manage",
                body.GetProperty("locationsAccess").GetString()
            );
            Assert.Equal(
                "manage",
                body.GetProperty("accountWorkspaceAccess").GetString()
            );
        }

        [Fact]
        public async Task AccountWorkspace_Staff_Returns403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await GetAsync(
                seeded.MemberJwt,
                "/api/account-workspace"
            );
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Pause_ViewRole_Returns403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await PostAsync(
                seeded.MemberJwt,
                "/api/account-workspace/pause"
            );
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task StaffRedeemCheck_StillAllowed()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );
            var issueId = await SeedClaimAsync(
                seeded.InScopeLocationId,
                "TUM-STAFF01"
            );

            var response = await PostJsonAsync(
                seeded.MemberJwt,
                "/api/offers/redeem/check",
                new
                {
                    locationId = seeded.InScopeLocationId,
                    code = "tum-staff01",
                }
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                issueId.ToString(),
                body.GetProperty("preview").GetProperty("issueId").GetString()
            );
        }

        [Fact]
        public async Task StaffRedeemCheck_UnknownCode_ReturnsInvalid()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await PostJsonAsync(
                seeded.MemberJwt,
                "/api/offers/redeem/check",
                new
                {
                    locationId = seeded.InScopeLocationId,
                    code = "TUM-NOSUCH",
                }
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("invalid", body.GetProperty("reason").GetString());
        }

        private async Task<int> SeedClaimAsync(int locationId, string claimCode)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "£5 off",
                Description = "Seeded for Staff redeem test.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                StaffInstructions = "Apply £5 off before payment.",
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = context.RestaurantLocations
                    .Single(row => row.Id == locationId)
                    .RestaurantId,
                Email = $"staff-redeem-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = "Staff Redeem Guest",
                CreatedAt = now,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            var issue = new OfferIssue
            {
                CatalogOfferId = offer.Id,
                LocationGuestId = guest.Id,
                ClaimCode = claimCode,
                IssuedAtUtc = now.AddDays(-1),
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = now.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "£5 off",
                Description = "Seeded issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                StaffInstructions = "Apply £5 off before payment.",
            };
            context.OfferIssues.Add(issue);
            await context.SaveChangesAsync();
            return issue.Id;
        }

        private async Task<HttpResponseMessage> GetAsync(string jwt, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PostAsync(string jwt, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PostJsonAsync(
            string jwt,
            string url,
            object body
        )
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
    }
}
