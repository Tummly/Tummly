using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class LocationEntitlementEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationEntitlementEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SetupAccount_StillCreatesOverCapVenues_OnPilot()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.TrialRequests.Add(
                    new TrialRequest
                    {
                        BusinessName = "Over Cap Pilot Group",
                        BusinessCategory = "multi-site",
                        Locations = "2",
                        FullName = "Multi Owner",
                        Email = "over-cap-pilot@example.com",
                        Mobile = "07911123456",
                        MainLocation = "1 High Street",
                        TownCity = "Leeds",
                        Postcode = "LS1 1AA",
                        Role = "Owner",
                        Goal = "Grow",
                        TermsAccepted = true,
                        IsEmailVerified = true,
                        IsApproved = true,
                        Status = TrialRequestStatus.Approved,
                        ApprovalToken = "over-cap-pilot-token",
                        InviteExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsAccountCreated = false,
                        AccountType = "Multi",
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            var response = await _client.PostAsJsonAsync(
                "/api/auth/setup-account",
                new
                {
                    token = "over-cap-pilot-token",
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "Multi Owner",
                    groupName = "Over Cap Pilot Group",
                    businessCategory = "multi-site",
                    primaryPhone = "07911123456",
                    locations = new[]
                    {
                        new
                        {
                            locationName = "Site A",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                        new
                        {
                            locationName = "Site B",
                            address = "2 High Street",
                            city = "Leeds",
                            postcode = "LS1 2AA",
                        },
                    },
                }
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurant = await context.Restaurants
                    .AsNoTracking()
                    .SingleAsync(row => row.Name == "Over Cap Pilot Group");
                var billingAccount = await context.BillingAccounts
                    .AsNoTracking()
                    .SingleAsync(row => row.RestaurantId == restaurant.Id);
                var locationCount = await context.RestaurantLocations
                    .CountAsync(row => row.RestaurantId == restaurant.Id);

                Assert.Equal(2, locationCount);
                Assert.Equal(
                    BillingSubscriptionPlans.Pilot,
                    billingAccount.SubscriptionPlan
                );
            }
        }

        [Fact]
        public async Task AddOwnedLocation_AtCap_Returns409_WithCapAndCurrent()
        {
            var seeded = await SeedPilotAtCapAsync();

            using var request = AuthorizedPostLocation(
                seeded.OwnerJwt,
                new
                {
                    locationName = "Second",
                    address = "2 High Street",
                    city = "Leeds",
                    postcode = "LS1 2AA",
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "location_cap_reached",
                body.GetProperty("code").GetString()
            );
            Assert.Equal(1, body.GetProperty("cap").GetInt32());
            Assert.Equal(1, body.GetProperty("current").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var locationCount = await context.RestaurantLocations
                .CountAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(1, locationCount);
        }

        [Fact]
        public async Task AddOwnedLocation_MissingPricebook_FailsClosed()
        {
            var seeded = await SeedPilotAtCapAsync(
                restaurantName: "Missing Book Venue"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var billingAccount = await context.BillingAccounts
                    .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
                billingAccount.ContractedPricebookId = "missing-pricebook";
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedPostLocation(
                seeded.OwnerJwt,
                new
                {
                    locationName = "Second",
                    address = "2 High Street",
                    city = "Leeds",
                    postcode = "LS1 2AA",
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(
                body.TryGetProperty("code", out var code)
                && code.GetString() == "location_cap_reached"
            );

            using var verify = _factory.Services.CreateScope();
            var verifyContext = verify.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var locationCount = await verifyContext.RestaurantLocations
                .CountAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(1, locationCount);
        }

        private async Task<Seeded> SeedPilotAtCapAsync(
            string restaurantName = "Pilot Cap Venue"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = $"Owner {restaurantName}",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
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
                Name = restaurantName,
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    CurrentPricebookId
                )
            );

            owner.SelectedRestaurantId = restaurant.Id;
            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role)
            );
        }

        private static HttpRequestMessage AuthorizedPostLocation(
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/locations");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(int RestaurantId, string OwnerJwt);
    }
}
