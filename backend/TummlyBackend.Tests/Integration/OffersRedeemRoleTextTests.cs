using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// R7.2: a refusal caused by the role gets the permission text; a refusal
    /// caused by the Location set or the tenant keeps the Location text.
    /// Seam: POST /api/offers/redeem/check and /api/offers/redeem.
    /// </summary>
    public class OffersRedeemRoleTextTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string PermissionText =
            "You do not have permission to use this feature.";
        private const string LocationText =
            "You do not have access to this location.";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersRedeemRoleTextTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task BillingAdmin_RedeemCheck_403PermissionText()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.BillingAdmin,
                namedInScopeOnly: false
            );

            var response = await PostAsync(
                "/api/offers/redeem/check",
                seeded.MemberJwt,
                new { locationId = seeded.InScopeLocationId, code = "TUM-ROLE01" }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(PermissionText, await MessageAsync(response));
        }

        [Fact]
        public async Task BillingAdmin_RedeemConfirm_403PermissionText()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.BillingAdmin,
                namedInScopeOnly: false
            );

            var response = await PostAsync(
                "/api/offers/redeem",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    code = "TUM-ROLE02",
                    issueId = "1",
                }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(PermissionText, await MessageAsync(response));
        }

        [Fact]
        public async Task OtherTenantLocation_RedeemCheck_403LocationText()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.BillingAdmin,
                namedInScopeOnly: false
            );

            var response = await PostAsync(
                "/api/offers/redeem/check",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.OtherTenantLocationId,
                    code = "TUM-ROLE03",
                }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(LocationText, await MessageAsync(response));
        }

        private async Task<HttpResponseMessage> PostAsync(
            string path,
            string jwt,
            object body
        )
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private static async Task<string?> MessageAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("message").GetString();
        }
    }
}
