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
    public class TeamPermissionsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public TeamPermissionsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/team-permissions");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsOwnerDirectory_IncludingOwnerRow()
        {
            var seeded = await SeedWorkspaceAsync();

            using var request = Authorized(HttpMethod.Get, "/api/team-permissions", seeded.OwnerJwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("actorCanManage").GetBoolean());
            Assert.Equal(3, body.GetProperty("stats").GetProperty("activeMembers").GetInt32());
            var members = body.GetProperty("members");
            Assert.Equal(3, members.GetArrayLength());
            Assert.Contains(
                members.EnumerateArray(),
                row => row.GetProperty("isAccountOwner").GetBoolean()
            );
        }

        [Fact]
        public async Task Me_ReturnsManage_ForAccountOwner()
        {
            var seeded = await SeedWorkspaceAsync();

            using var request = Authorized(HttpMethod.Get, "/api/auth/me", seeded.OwnerJwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var data = (await ReadJsonAsync(response)).GetProperty("data");
            Assert.Equal(
                "manage",
                data.GetProperty("teamPermissionsAccess").GetString()
            );
        }

        [Fact]
        public async Task Me_ReturnsNone_ForStaff()
        {
            var seeded = await SeedWorkspaceAsync();

            using var request = Authorized(HttpMethod.Get, "/api/auth/me", seeded.StaffJwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var data = (await ReadJsonAsync(response)).GetProperty("data");
            Assert.Equal(
                "none",
                data.GetProperty("teamPermissionsAccess").GetString()
            );
        }

        [Fact]
        public async Task RestaurantLocations_ReturnsTeamPermissionsAccess_ForOwnerAndStaff()
        {
            var seeded = await SeedWorkspaceAsync();

            using var ownerRequest = Authorized(
                HttpMethod.Get,
                "/api/restaurant/locations",
                seeded.OwnerJwt
            );
            var ownerResponse = await _client.SendAsync(ownerRequest);
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
            Assert.Equal(
                "manage",
                (await ReadJsonAsync(ownerResponse))
                    .GetProperty("teamPermissionsAccess")
                    .GetString()
            );

            using var staffRequest = Authorized(
                HttpMethod.Get,
                "/api/restaurant/locations",
                seeded.StaffJwt
            );
            var staffResponse = await _client.SendAsync(staffRequest);
            Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
            Assert.Equal(
                "none",
                (await ReadJsonAsync(staffResponse))
                    .GetProperty("teamPermissionsAccess")
                    .GetString()
            );
        }

        [Fact]
        public async Task Get_Returns403_ForStaff()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions",
                seeded.StaffJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PatchRole_AdminManage_CanChangeStaffRole()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/role",
                seeded.AdminJwt,
                new { permissionRole = PermissionRoles.Marketing }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task PatchRole_Returns403_OnSelfWrite()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/team-permissions/members/{seeded.AdminMembershipId}/role",
                seeded.AdminJwt,
                new { permissionRole = PermissionRoles.Staff }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PatchRole_Returns403_OnAccountOwner()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/team-permissions/members/{seeded.OwnerMembershipId}/role",
                seeded.OwnerJwt,
                new { permissionRole = PermissionRoles.Admin }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Deactivate_ReassignsKeyContacts_AndDeniesNextRequest()
        {
            var seeded = await SeedWorkspaceAsync();

            using var deactivate = Authorized(
                HttpMethod.Post,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/deactivate",
                seeded.OwnerJwt
            );
            var deactivateResponse = await _client.SendAsync(deactivate);
            Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .FirstAsync(row => row.Id == seeded.RestaurantId);
            Assert.Equal(seeded.OwnerUserId, restaurant.BillingContactUserId);

            using var next = Authorized(
                HttpMethod.Get,
                "/api/restaurant/locations",
                seeded.StaffJwt
            );
            var nextResponse = await _client.SendAsync(next);
            Assert.Equal(HttpStatusCode.Forbidden, nextResponse.StatusCode);
        }

        [Fact]
        public async Task Deactivate_FreezesRoleChange()
        {
            var seeded = await SeedWorkspaceAsync();
            using var deactivate = Authorized(
                HttpMethod.Post,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/deactivate",
                seeded.OwnerJwt
            );
            await _client.SendAsync(deactivate);

            using var patch = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/role",
                seeded.OwnerJwt,
                new { permissionRole = PermissionRoles.Marketing }
            );
            var response = await _client.SendAsync(patch);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Remove_EndsMembership_AndKeepsUser()
        {
            var seeded = await SeedWorkspaceAsync();
            using var remove = Authorized(
                HttpMethod.Delete,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(remove);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.RestaurantMemberships.AnyAsync(row =>
                    row.Id == seeded.StaffMembershipId
                )
            );
            Assert.True(await context.Users.AnyAsync(row => row.Id == seeded.StaffUserId));
            Assert.True(
                await context.RestaurantAccessActivities.AnyAsync(row =>
                    row.Kind == AccessActivityKinds.MemberRemoved
                )
            );
        }

        [Fact]
        public async Task Admin_CannotDemoteOwnerGlue()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                $"/api/team-permissions/members/{seeded.OwnerMembershipId}/deactivate",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsEveryAreaRowOnThePermissionMatrix()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var matrix = body.GetProperty("matrix");
            Assert.Equal(13, matrix.GetArrayLength());
            Assert.Equal(
                OperatorAreaIds.All,
                matrix.EnumerateArray().Select(row => row.GetProperty("id").GetString())
            );
            var billing = matrix
                .EnumerateArray()
                .First(row => row.GetProperty("id").GetString() == OperatorAreaIds.BillingCredits);
            Assert.Equal(
                "View",
                billing.GetProperty("cells").GetProperty("Admin").GetString()
            );
        }

        [Fact]
        public async Task PutMatrix_OwnerSavesAdminCells_AndEmitsActivityInAreaIdOrder()
        {
            var seeded = await SeedWorkspaceAsync();
            using var put = AuthorizedJson(
                HttpMethod.Put,
                "/api/team-permissions/matrix",
                seeded.OwnerJwt,
                new
                {
                    adminCells = new[]
                    {
                        new { areaId = OperatorAreaIds.BillingCredits, level = "Manage" },
                        new { areaId = OperatorAreaIds.Locations, level = "View" },
                    },
                }
            );
            var putResponse = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

            using var get = Authorized(
                HttpMethod.Get,
                "/api/team-permissions",
                seeded.OwnerJwt
            );
            var getResponse = await _client.SendAsync(get);
            var body = await ReadJsonAsync(getResponse);
            var cellsByArea = body
                .GetProperty("matrix")
                .EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("id").GetString()!,
                    row => row.GetProperty("cells").GetProperty("Admin").GetString()
                );
            Assert.Equal("View", cellsByArea[OperatorAreaIds.Locations]);
            Assert.Equal("Manage", cellsByArea[OperatorAreaIds.BillingCredits]);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var rows = await context.RestaurantAccessActivities
                .Where(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind == "permission-cell-changed"
                )
                .OrderBy(row => row.Id)
                .ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.Equal($"{OperatorAreaIds.Locations}:Manage", rows[0].FromValue);
            Assert.Equal($"{OperatorAreaIds.Locations}:View", rows[0].ToValue);
            Assert.Equal($"{OperatorAreaIds.BillingCredits}:View", rows[1].FromValue);
            Assert.Equal($"{OperatorAreaIds.BillingCredits}:Manage", rows[1].ToValue);
            Assert.All(rows, row => Assert.Equal(seeded.OwnerUserId, row.ActorUserId));
        }

        [Fact]
        public async Task PutMatrix_Returns403_ForAdminActor()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Put,
                "/api/team-permissions/matrix",
                seeded.AdminJwt,
                new
                {
                    adminCells = new[]
                    {
                        new { areaId = OperatorAreaIds.BillingCredits, level = "Manage" },
                    },
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData(OperatorAreaIds.AccountWorkspace, "Scoped")]
        [InlineData(OperatorAreaIds.TeamPermissions, "No access")]
        public async Task PutMatrix_RejectsIllegalAdminValues(
            string areaId,
            string level
        )
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Put,
                "/api/team-permissions/matrix",
                seeded.OwnerJwt,
                new
                {
                    adminCells = new[] { new { areaId, level } },
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.RestaurantAccessActivities.AnyAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind == "permission-cell-changed"
                )
            );
        }

        [Fact]
        public async Task PutMatrix_DoesNotEmit_WhenFromEqualsTo()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Put,
                "/api/team-permissions/matrix",
                seeded.OwnerJwt,
                new
                {
                    adminCells = new[]
                    {
                        new { areaId = OperatorAreaIds.BillingCredits, level = "View" },
                    },
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.RestaurantAccessActivities.AnyAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind == "permission-cell-changed"
                )
            );
        }

        private async Task<Seeded> SeedWorkspaceAsync()
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
                Name = "Team Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var locA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var locB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(locA, locB);
            await context.SaveChangesAsync();

            var ownerMembership = AddMembership(
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

            restaurant.BillingContactUserId = staff.Id;
            var adminMembership = AddMembership(
                context,
                admin.Id,
                restaurant.Id,
                PermissionRoles.Admin,
                LocationScopeKind.AllLocations,
                "[]"
            );
            var staffMembership = AddMembership(
                context,
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff,
                LocationScopeKind.NamedList,
                MembershipLocationScope.SerializeNamedIds([locA.Id])
            );
            await context.SaveChangesAsync();

            return new Seeded(
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(staff.Id.ToString(), staff.Email, staff.Role),
                restaurant.Id,
                owner.Id,
                staff.Id,
                ownerMembership.Id,
                adminMembership.Id,
                staffMembership.Id
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

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = Authorized(method, url, jwt);
            request.Content = JsonContent.Create(body);
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
            int RestaurantId,
            int OwnerUserId,
            int StaffUserId,
            int OwnerMembershipId,
            int AdminMembershipId,
            int StaffMembershipId
        );
    }
}
