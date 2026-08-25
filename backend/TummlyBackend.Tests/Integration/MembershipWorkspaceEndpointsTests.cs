using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class MembershipWorkspaceEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MembershipWorkspaceEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Workspaces_ListsActiveMemberships_AndOmitsDeactivated()
        {
            var seeded = await SeedOwnerWithSecondMembershipAsync(
                deactivateSecond: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/workspaces"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var data = body.GetProperty("data");
            Assert.Equal(1, data.GetArrayLength());
            Assert.Equal(
                seeded.FirstRestaurantId,
                data[0].GetProperty("restaurantId").GetInt32()
            );
        }

        [Fact]
        public async Task Workspaces_ListsBoth_WhenTwoActiveMemberships()
        {
            var seeded = await SeedOwnerWithSecondMembershipAsync(
                deactivateSecond: false
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/workspaces"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var data = body.GetProperty("data");
            Assert.Equal(2, data.GetArrayLength());
            var ids = data.EnumerateArray()
                .Select(item => item.GetProperty("restaurantId").GetInt32())
                .ToHashSet();
            Assert.Contains(seeded.FirstRestaurantId, ids);
            Assert.Contains(seeded.SecondRestaurantId, ids);
        }

        [Fact]
        public async Task AccountWorkspace_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/account-workspace");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspace_Returns403_WhenMembershipDeactivated()
        {
            var seeded = await SeedOwnerWithSecondMembershipAsync(
                deactivateSecond: true
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var first = await context.RestaurantMemberships
                .FirstAsync(m => m.RestaurantId == seeded.FirstRestaurantId);
            first.Status = MembershipStatus.Deactivated;
            await context.SaveChangesAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/account-workspace"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspace_Returns403_ForTummlyStaffJwt()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var admin = new Admin
            {
                FullName = "Staff",
                Email = "staff-11@example.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateAdminToken(admin);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/account-workspace"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AccountWorkspace_CountsActiveMemberships()
        {
            var seeded = await SeedOwnerWithSecondMembershipAsync(
                deactivateSecond: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/account-workspace"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                1,
                body.GetProperty("status").GetProperty("teamMembers").GetInt32()
            );
            Assert.Equal(
                1,
                body.GetProperty("keyContacts")
                    .GetProperty("eligibleMembers")
                    .GetArrayLength()
            );
        }

        [Fact]
        public async Task SignInMe_Succeeds_WhenOnlyMembershipDeactivated()
        {
            var seeded = await SeedOwnerWithSecondMembershipAsync(
                deactivateSecond: true
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            foreach (var membership in context.RestaurantMemberships.Where(m =>
                m.UserId == seeded.UserId
            ))
            {
                membership.Status = MembershipStatus.Deactivated;
            }

            await context.SaveChangesAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int UserId,
            int FirstRestaurantId,
            int SecondRestaurantId
        )> SeedOwnerWithSecondMembershipAsync(bool deactivateSecond)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Membership Owner",
                Email = $"mem-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var first = new Restaurant
            {
                Name = "First Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(first);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = user.Id,
                RestaurantId = first.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var otherOwner = new User
            {
                FullName = "Other Owner",
                Email = $"other-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900112",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(otherOwner);
            await context.SaveChangesAsync();

            var second = new Restaurant
            {
                Name = "Second Venue",
                AccountType = "Single",
                OwnerUserId = otherOwner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(second);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = user.Id,
                RestaurantId = second.Id,
                PermissionRole = PermissionRoles.Staff,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = deactivateSecond
                    ? MembershipStatus.Deactivated
                    : MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, user.Id, first.Id, second.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
    }
}
