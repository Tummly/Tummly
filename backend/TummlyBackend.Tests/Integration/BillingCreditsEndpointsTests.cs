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

namespace TummlyBackend.Tests.Integration
{
    public class BillingCreditsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BillingCreditsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/billing-credits");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_Returns403_ForStaff()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.StaffJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsPilotSnapshot_ForAdminView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("actorCanManage").GetBoolean());
            Assert.Equal("Admin", body.GetProperty("actorPermissionRole").GetString());

            var plan = body.GetProperty("planSubscription");
            Assert.Equal("Pilot", plan.GetProperty("subscriptionPlan").GetString());
            Assert.Equal("Pilot", plan.GetProperty("billingStatus").GetString());
            Assert.Equal(500, plan.GetProperty("emailCreditsRemaining").GetInt32());
            Assert.Equal(20, plan.GetProperty("smsCreditsRemaining").GetInt32());
            Assert.Equal(20, plan.GetProperty("aiCreditsRemaining").GetInt32());
            Assert.True(plan.GetProperty("isPilot").GetBoolean());
        }

        [Fact]
        public async Task PostPlanChange_Returns403_ForAdminView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.AdminJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPlanChange_Returns403_ForBillingAdminManage()
        {
            var seeded = await SeedWorkspaceAsync(includeBillingAdmin: true);
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.BillingAdminJwt!
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPlanChange_ReturnsPayRedirect_ForOwnerPilotConversion()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
            Assert.Contains(
                "checkout.revolut.com",
                body.GetProperty("redirectUrl").GetString()
            );
        }

        [Fact]
        public async Task PostPlanChange_ReturnsPayRedirect_ForOwnerAnnualPilotConversion()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "annual",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
        }

        private async Task<Seeded> SeedWorkspaceAsync(bool includeBillingAdmin = false)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, "Owner Fifteen", "Owner");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Billing Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );

            var admin = AddUser(context, "Admin Fifteen", "Owner");
            admin.SelectedRestaurantId = restaurant.Id;
            var staff = AddUser(context, "Staff Fifteen", "Owner");
            staff.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            AddMembership(
                context,
                admin.Id,
                restaurant.Id,
                PermissionRoles.Admin,
                LocationScopeKind.AllLocations,
                "[]"
            );
            AddMembership(
                context,
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff,
                LocationScopeKind.NamedList,
                MembershipLocationScope.SerializeNamedIds([location.Id])
            );

            string? billingAdminJwt = null;
            if (includeBillingAdmin)
            {
                var billingAdmin = AddUser(context, "Billing Admin Sixteen", "Owner");
                billingAdmin.SelectedRestaurantId = restaurant.Id;
                await context.SaveChangesAsync();

                AddMembership(
                    context,
                    billingAdmin.Id,
                    restaurant.Id,
                    PermissionRoles.BillingAdmin,
                    LocationScopeKind.AllLocations,
                    "[]"
                );
                await context.SaveChangesAsync();
                billingAdminJwt = jwtService.GenerateToken(
                    billingAdmin.Id.ToString(),
                    billingAdmin.Email,
                    billingAdmin.Role
                );
            }

            await context.SaveChangesAsync();

            return new Seeded(
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(staff.Id.ToString(), staff.Email, staff.Role),
                billingAdminJwt
            );
        }

        private static User AddUser(
            ApplicationDbContext context,
            string name,
            string role
        )
        {
            var user = new User
            {
                FullName = name,
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = role,
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            return user;
        }

        private static RestaurantMembership AddMembership(
            ApplicationDbContext context,
            int userId,
            int restaurantId,
            string permissionRole,
            LocationScopeKind scope,
            string namedJson
        )
        {
            var row = new RestaurantMembership
            {
                UserId = userId,
                RestaurantId = restaurantId,
                PermissionRole = permissionRole,
                LocationScope = scope,
                NamedLocationIdsJson = namedJson,
                Status = MembershipStatus.Active,
            };
            context.RestaurantMemberships.Add(row);
            return row;
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

        private sealed record Seeded(
            string OwnerJwt,
            string AdminJwt,
            string StaffJwt,
            string? BillingAdminJwt = null
        );
    }
}
