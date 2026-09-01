using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class PlanEntitlementCapEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PlanEntitlementCapEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetTeamPermissionsPage_IncludesStarterTeamCap()
        {
            var seeded = await SeedPaidOwnerAsync(BillingSubscriptionPlans.Starter);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/team-permissions"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var entitlements = body.GetProperty("entitlements");
            var teamMembers = entitlements.GetProperty("teamMembers");
            Assert.Equal(3, teamMembers.GetProperty("cap").GetInt32());
            Assert.Equal(1, teamMembers.GetProperty("current").GetInt32());
            Assert.False(teamMembers.GetProperty("atCap").GetBoolean());
        }

        [Fact]
        public async Task SendInvite_Returns409_WhenStarterTeamCapReached()
        {
            var seeded = await SeedPaidOwnerAsync(BillingSubscriptionPlans.Starter);

            for (var i = 0; i < 2; i++)
            {
                var ok = await SendInviteAsync(
                    seeded.Jwt,
                    $"{Guid.NewGuid():N}@example.com"
                );
                Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
            }

            var blocked = await SendInviteAsync(
                seeded.Jwt,
                $"{Guid.NewGuid():N}@example.com"
            );
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            var body = await ReadJsonAsync(blocked);
            Assert.Equal(
                "team_member_cap_reached",
                body.GetProperty("code").GetString()
            );
            Assert.Equal(3, body.GetProperty("cap").GetInt32());
            Assert.Equal(3, body.GetProperty("current").GetInt32());
        }

        [Fact]
        public async Task ListOffers_IncludesStarterActiveOfferCap()
        {
            var seeded = await SeedPaidOwnerAsync(BillingSubscriptionPlans.Starter);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/offers?locationId={seeded.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var activeOffers = body
                .GetProperty("entitlements")
                .GetProperty("activeOffers");
            Assert.Equal(3, activeOffers.GetProperty("cap").GetInt32());
            Assert.Equal(0, activeOffers.GetProperty("current").GetInt32());
        }

        private sealed record SeededOwner(string Jwt, int LocationId);

        private async Task<SeededOwner> SeedPaidOwnerAsync(string plan)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Paid Owner",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Paid {plan} Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                CurrentPricebookId
            );
            account.SubscriptionPlan = plan;
            account.BillingStatus = BillingPlanSnapshotHelper.ActiveStatus;
            account.BillingCycle = BillingCycles.Monthly;
            context.BillingAccounts.Add(account);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = user.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new SeededOwner(jwt, location.Id);
        }

        private async Task<HttpResponseMessage> SendInviteAsync(
            string jwt,
            string email
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/team-permissions/invitations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new
            {
                email,
                fullName = "New Member",
                permissionRole = PermissionRoles.Staff,
                locationScope = "all",
                namedLocationIds = Array.Empty<int>(),
            });
            return await _client.SendAsync(request);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }
    }
}
