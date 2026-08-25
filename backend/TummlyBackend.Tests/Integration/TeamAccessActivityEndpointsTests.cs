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
    public class TeamAccessActivityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public TeamAccessActivityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync(
                "/api/team-permissions/access-activity"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_Returns403_WhenPrivacyConsentIsNoAccess()
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
                        new
                        {
                            areaId = OperatorAreaIds.PrivacyConsent,
                            level = "No access",
                        },
                    },
                }
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await _client.SendAsync(put)).StatusCode
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions/access-activity",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_Returns403_ForStaff()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions/access-activity",
                seeded.StaffJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsNewestFirst_WithSnapshots_AndOmitsUnchanged()
        {
            var seeded = await SeedWorkspaceAsync();

            using var sameMatrix = AuthorizedJson(
                HttpMethod.Put,
                "/api/team-permissions/matrix",
                seeded.OwnerJwt,
                new
                {
                    adminCells = new[]
                    {
                        new
                        {
                            areaId = OperatorAreaIds.BillingCredits,
                            level = "View",
                        },
                    },
                }
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await _client.SendAsync(sameMatrix)).StatusCode
            );

            using var role = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/role",
                seeded.OwnerJwt,
                new { permissionRole = PermissionRoles.Marketing }
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await _client.SendAsync(role)).StatusCode
            );

            using var scope = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/location-scope",
                seeded.OwnerJwt,
                new
                {
                    locationScope = "named",
                    namedLocationIds = new[] { seeded.LocationBId },
                }
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await _client.SendAsync(scope)).StatusCode
            );

            using var matrix = AuthorizedJson(
                HttpMethod.Put,
                "/api/team-permissions/matrix",
                seeded.OwnerJwt,
                new
                {
                    adminCells = new[]
                    {
                        new
                        {
                            areaId = OperatorAreaIds.BillingCredits,
                            level = "Manage",
                        },
                        new
                        {
                            areaId = OperatorAreaIds.Locations,
                            level = "View",
                        },
                    },
                }
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await _client.SendAsync(matrix)).StatusCode
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions/access-activity?page=1&pageSize=10",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items");
            Assert.Equal(4, items.GetArrayLength());
            Assert.Equal(4, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(10, body.GetProperty("pageSize").GetInt32());

            var kinds = items
                .EnumerateArray()
                .Select(row => row.GetProperty("kind").GetString())
                .ToArray();
            Assert.Equal(
                new[]
                {
                    AccessActivityKinds.PermissionCellChanged,
                    AccessActivityKinds.PermissionCellChanged,
                    AccessActivityKinds.LocationScopeChanged,
                    AccessActivityKinds.RoleChanged,
                },
                kinds
            );

            var cells = items
                .EnumerateArray()
                .Where(row =>
                    row.GetProperty("kind").GetString()
                    == AccessActivityKinds.PermissionCellChanged
                )
                .ToArray();
            Assert.Equal(
                $"{OperatorAreaIds.Locations}:View",
                cells[0].GetProperty("toValue").GetString()
            );
            Assert.Equal(
                $"{OperatorAreaIds.BillingCredits}:Manage",
                cells[1].GetProperty("toValue").GetString()
            );

            var roleRow = items
                .EnumerateArray()
                .First(row =>
                    row.GetProperty("kind").GetString()
                    == AccessActivityKinds.RoleChanged
                );
            Assert.Equal("Owner Fifteen", roleRow.GetProperty("actorDisplayName").GetString());
            Assert.Equal("Staff Fifteen", roleRow.GetProperty("targetDisplayName").GetString());
            Assert.Equal(PermissionRoles.Staff, roleRow.GetProperty("fromValue").GetString());
            Assert.Equal(
                PermissionRoles.Marketing,
                roleRow.GetProperty("toValue").GetString()
            );

            var scopeRow = items
                .EnumerateArray()
                .First(row =>
                    row.GetProperty("kind").GetString()
                    == AccessActivityKinds.LocationScopeChanged
                );
            Assert.Equal("Camden only", scopeRow.GetProperty("fromValue").GetString());
            Assert.Equal("Soho only", scopeRow.GetProperty("toValue").GetString());
        }

        [Fact]
        public async Task Get_KeepsRows_AfterMemberRemove()
        {
            var seeded = await SeedWorkspaceAsync();
            using var deactivate = Authorized(
                HttpMethod.Post,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}/deactivate",
                seeded.OwnerJwt
            );
            await _client.SendAsync(deactivate);
            using var remove = Authorized(
                HttpMethod.Delete,
                $"/api/team-permissions/members/{seeded.StaffMembershipId}",
                seeded.OwnerJwt
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await _client.SendAsync(remove)).StatusCode
            );

            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions/access-activity",
                seeded.OwnerJwt
            );
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var kinds = body
                .GetProperty("items")
                .EnumerateArray()
                .Select(row => row.GetProperty("kind").GetString())
                .ToArray();
            Assert.Contains(AccessActivityKinds.MemberDeactivated, kinds);
            Assert.Contains(AccessActivityKinds.MemberRemoved, kinds);
        }

        [Fact]
        public async Task Get_ReturnsInvitationAndAcceptSnapshots()
        {
            var seeded = await SeedWorkspaceAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.RestaurantAccessActivities.AddRange(
                    new RestaurantAccessActivity
                    {
                        RestaurantId = seeded.RestaurantId,
                        ActorUserId = seeded.OwnerUserId,
                        ActorDisplayName = "Owner Fifteen",
                        TargetDisplayName = "Mark Invitee",
                        TargetEmail = "mark@example.com",
                        Kind = AccessActivityKinds.InvitationSent,
                        FromValue = PermissionRoles.ReportingOnly,
                        ToValue = "All locations",
                        OccurredAt = DateTime.UtcNow.AddMinutes(-2),
                    },
                    new RestaurantAccessActivity
                    {
                        RestaurantId = seeded.RestaurantId,
                        ActorUserId = 9001,
                        ActorDisplayName = "Mark Invitee",
                        TargetUserId = 9001,
                        TargetDisplayName = "Mark Invitee",
                        TargetEmail = "mark@example.com",
                        Kind = AccessActivityKinds.InvitationAccepted,
                        ToValue = PermissionRoles.ReportingOnly,
                        OccurredAt = DateTime.UtcNow.AddMinutes(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions/access-activity",
                seeded.OwnerJwt
            );
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var items = body.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(AccessActivityKinds.InvitationAccepted, items[0].GetProperty("kind").GetString());
            Assert.Equal("Mark Invitee", items[0].GetProperty("actorDisplayName").GetString());
            Assert.Equal(AccessActivityKinds.InvitationSent, items[1].GetProperty("kind").GetString());
            Assert.Equal(PermissionRoles.ReportingOnly, items[1].GetProperty("fromValue").GetString());
            Assert.Equal("All locations", items[1].GetProperty("toValue").GetString());
        }

        [Fact]
        public async Task Get_PagesSheetSizeTwenty()
        {
            var seeded = await SeedWorkspaceAsync();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 25; i++)
            {
                context.RestaurantAccessActivities.Add(
                    new RestaurantAccessActivity
                    {
                        RestaurantId = seeded.RestaurantId,
                        ActorUserId = seeded.OwnerUserId,
                        ActorDisplayName = "Owner Fifteen",
                        TargetDisplayName = $"Row {i}",
                        Kind = AccessActivityKinds.MemberRemoved,
                        OccurredAt = DateTime.UtcNow.AddMinutes(-i),
                    }
                );
            }

            await context.SaveChangesAsync();

            using var request = Authorized(
                HttpMethod.Get,
                "/api/team-permissions/access-activity?page=2&pageSize=20",
                seeded.OwnerJwt
            );
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            Assert.Equal(20, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(2, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(5, body.GetProperty("items").GetArrayLength());
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
                locB.Id,
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
            int LocationBId,
            int OwnerMembershipId,
            int AdminMembershipId,
            int StaffMembershipId
        );
    }
}
