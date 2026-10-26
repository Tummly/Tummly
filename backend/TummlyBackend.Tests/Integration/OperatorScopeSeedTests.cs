using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class OperatorScopeSeedTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OperatorScopeSeedTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Seed_CreatesTwoTenantsAndScopedMember()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var inScope = context.RestaurantLocations
                    .Single(l => l.Id == seeded.InScopeLocationId);
                var outOfScope = context.RestaurantLocations
                    .Single(l => l.Id == seeded.OutOfScopeLocationId);
                var otherTenant = context.RestaurantLocations
                    .Single(l => l.Id == seeded.OtherTenantLocationId);

                Assert.Equal(seeded.RestaurantId, inScope.RestaurantId);
                Assert.Equal(seeded.RestaurantId, outOfScope.RestaurantId);
                Assert.NotEqual(seeded.RestaurantId, otherTenant.RestaurantId);

                var member = context.Users
                    .Single(u => u.Id == seeded.MemberUserId);
                Assert.NotEqual("Owner", member.Role);
            }

            var inScopeResponse = await GetGuestsAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId
            );
            Assert.Equal(HttpStatusCode.OK, inScopeResponse.StatusCode);

            var outOfScopeResponse = await GetGuestsAsync(
                seeded.MemberJwt,
                seeded.OutOfScopeLocationId
            );
            Assert.Equal(HttpStatusCode.Forbidden, outOfScopeResponse.StatusCode);
        }

        private async Task<HttpResponseMessage> GetGuestsAsync(
            string jwt,
            int locationId
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests?locationId={locationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }
    }
}
