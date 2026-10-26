using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// R3.5: a Free account keeps read access to Feedback, Guests, Reports history
    /// and Billing. Only live writes are gated by plan_required.
    /// </summary>
    public class FreePlanReadEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FreePlanReadEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task FreePlan_ReadEndpoints_StillOk()
        {
            var (jwt, locationId) = await SeedFreeOwnerWithLocationAsync("free-plan-reads");

            foreach (var url in new[]
            {
                $"/api/feedback/inbox?locationId={locationId}&from=2026-10-01T00:00:00Z&to=2026-10-26T00:00:00Z",
                $"/api/guests?locationId={locationId}",
                "/api/billing-credits",
            })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var response = await _client.SendAsync(request);

                Assert.True(
                    response.StatusCode == HttpStatusCode.OK,
                    $"GET {url} returned {(int)response.StatusCode}."
                );
            }
        }

        private async Task<(string Jwt, int LocationId)> SeedFreeOwnerWithLocationAsync(
            string emailLocalPart
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Free Plan Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900124",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = null,
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Free Plan Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role);
            return (jwt, location.Id);
        }
    }
}
