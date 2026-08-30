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
    public class StaffManualAdjustmentEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StaffManualAdjustmentEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostStaffCreditAdjustment_Returns403_ForOperatorJwt()
        {
            var seeded = await SeedOperatorWorkspaceAsync();
            using var request = AuthorizedPost(
                seeded.OwnerJwt,
                new
                {
                    restaurantId = seeded.RestaurantId,
                    channel = CreditChannels.Email,
                    direction = StaffManualAdjustDirections.Grant,
                    quantity = 10,
                    reason = "Should not work for operator",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostStaffCreditAdjustment_Returns403_ForSupportStaff()
        {
            var seeded = await SeedOperatorWorkspaceAsync();
            using var request = AuthorizedPost(
                seeded.SupportJwt,
                new
                {
                    restaurantId = seeded.RestaurantId,
                    channel = CreditChannels.Email,
                    direction = StaffManualAdjustDirections.Grant,
                    quantity = 10,
                    reason = "Support cannot write manual adjustments",
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostStaffCreditAdjustment_GrantRaisesUsageRemaining_ForTummlyAdmin()
        {
            var seeded = await SeedOperatorWorkspaceAsync();
            using var grantRequest = AuthorizedPost(
                seeded.TummlyAdminJwt,
                new
                {
                    restaurantId = seeded.RestaurantId,
                    channel = CreditChannels.Email,
                    direction = StaffManualAdjustDirections.Grant,
                    quantity = 40,
                    reason = "Pilot goodwill email credits",
                }
            );
            var grantResponse = await _client.SendAsync(grantRequest);
            Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);

            var grantBody = await ReadJsonAsync(grantResponse);
            Assert.Equal(40, grantBody.GetProperty("combinedRemaining").GetInt32());

            using var usageRequest = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/usage",
                seeded.AdminJwt
            );
            var usageResponse = await _client.SendAsync(usageRequest);
            Assert.Equal(HttpStatusCode.OK, usageResponse.StatusCode);

            var usageBody = await ReadJsonAsync(usageResponse);
            var email = usageBody.GetProperty("channels").EnumerateArray()
                .First(row => row.GetProperty("channel").GetString() == CreditChannels.Email);
            Assert.Equal(40, email.GetProperty("combinedRemaining").GetInt32());
        }

        private static HttpRequestMessage AuthorizedPost(string jwt, object payload)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/admin/credit-adjustments"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static HttpRequestMessage Authorized(
            HttpMethod method,
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private async Task<Seeded> SeedOperatorWorkspaceAsync()
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
                Name = "Staff Adjust Venue",
                AccountType = "Single",
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
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
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

            var adminMember = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Operator Admin",
                Role = "Owner",
                PhoneNumber = "07700900112",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(adminMember);
            owner.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = adminMember.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Admin,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            var tummlyAdmin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            var support = new Admin
            {
                FullName = "Tummly Support",
                Email = $"support-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Support",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.AddRange(tummlyAdmin, support);
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateToken(
                    adminMember.Id.ToString(),
                    adminMember.Email,
                    adminMember.Role
                ),
                jwtService.GenerateAdminToken(tummlyAdmin),
                jwtService.GenerateAdminToken(support)
            );
        }

        private sealed record Seeded(
            int RestaurantId,
            string OwnerJwt,
            string AdminJwt,
            string TummlyAdminJwt,
            string SupportJwt
        );
    }
}
