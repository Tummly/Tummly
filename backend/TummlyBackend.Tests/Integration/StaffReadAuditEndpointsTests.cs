using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class StaffReadAuditEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StaffReadAuditEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ActivationDownload_WritesOneReadAuditRow_ForAdmin()
        {
            var seeded = await SeedOperatorWithActivationCodeAsync();
            var targetId = seeded.OperatorUserId.ToString();
            var before = await CountAuditAsync(
                AdminAuditActions.OperatorActivationAssetRead,
                targetId
            );

            using var request = Authorized(
                $"/api/admin/operators/{seeded.OperatorUserId}/activation-download",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var after = await CountAuditAsync(
                AdminAuditActions.OperatorActivationAssetRead,
                targetId
            );
            Assert.Equal(before + 1, after);
            await AssertLatestAuditAsync(
                AdminAuditActions.OperatorActivationAssetRead,
                targetId,
                AdminAuditTargetTypes.OperatorUser,
                seeded.AdminEmail,
                seeded.AdminId
            );
        }

        [Fact]
        public async Task ActivationDownload_UnknownOperator_WritesNoAuditRow()
        {
            var seeded = await SeedOperatorWithActivationCodeAsync();
            var before = await CountAuditAsync(
                AdminAuditActions.OperatorActivationAssetRead,
                "999999"
            );

            using var request = Authorized(
                "/api/admin/operators/999999/activation-download",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                before,
                await CountAuditAsync(
                    AdminAuditActions.OperatorActivationAssetRead,
                    "999999"
                )
            );
        }

        [Fact]
        public async Task SupportQueriesList_WritesOneReadAuditRow_ForSupport()
        {
            var support = await SeedStaffAsync("Support");
            var before = await CountAuditAsync(
                AdminAuditActions.SupportQueriesRead,
                "list"
            );

            using var request = Authorized("/api/support/queries", support.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                before + 1,
                await CountAuditAsync(AdminAuditActions.SupportQueriesRead, "list")
            );
            await AssertLatestAuditAsync(
                AdminAuditActions.SupportQueriesRead,
                "list",
                AdminAuditTargetTypes.HelpCentreQuery,
                support.Email,
                support.Id
            );
        }

        [Fact]
        public async Task SupportQueryDetail_WritesReadAuditRow_ForSupport()
        {
            var support = await SeedStaffAsync("Support");
            var queryId = await SeedHelpCentreQueryAsync();
            var targetId = queryId.ToString();
            var before = await CountAuditAsync(
                AdminAuditActions.SupportQueryRead,
                targetId
            );

            using var request = Authorized(
                $"/api/support/queries/{queryId}",
                support.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                before + 1,
                await CountAuditAsync(AdminAuditActions.SupportQueryRead, targetId)
            );
            await AssertLatestAuditAsync(
                AdminAuditActions.SupportQueryRead,
                targetId,
                AdminAuditTargetTypes.HelpCentreQuery,
                support.Email,
                support.Id
            );
        }

        [Fact]
        public async Task SupportQueryDetail_Unknown_WritesNoAuditRow()
        {
            var support = await SeedStaffAsync("Support");
            var before = await CountAuditAsync(
                AdminAuditActions.SupportQueryRead,
                "999999"
            );

            using var request = Authorized("/api/support/queries/999999", support.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                before,
                await CountAuditAsync(AdminAuditActions.SupportQueryRead, "999999")
            );
        }

        [Fact]
        public async Task TrialRequestsList_WritesOneReadAuditRow_WithoutProspectPii_ForAdmin()
        {
            var admin = await SeedStaffAsync("Admin");
            var prospectEmail = $"prospect-{Guid.NewGuid():N}@example.com";
            var prospectName = $"Prospect {Guid.NewGuid():N}";
            await SeedTrialRequestAsync(prospectName, prospectEmail);

            using var request = Authorized("/api/admin/trial-requests", admin.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rows = await ReadRowsForActorAsync(
                AdminAuditActions.TrialRequestsRead,
                admin.Email
            );
            var row = Assert.Single(rows);
            Assert.Equal(AdminAuditTargetTypes.TrialRequest, row.TargetType);
            Assert.Equal("list", row.TargetId);
            Assert.Equal(admin.Id, row.ActorAdminUserId);
            Assert.True(row.Succeeded);
            Assert.Contains("\"role\":\"Admin\"", row.DetailJson);
            Assert.DoesNotContain(prospectEmail, row.DetailJson ?? string.Empty);
            Assert.DoesNotContain(prospectName, row.DetailJson ?? string.Empty);
        }

        [Fact]
        public async Task PilotAccountsList_WritesOneReadAuditRow_WithoutOperatorPii_ForAdmin()
        {
            var admin = await SeedStaffAsync("Admin");

            using var request = Authorized("/api/admin/pilot-accounts", admin.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rows = await ReadRowsForActorAsync(
                AdminAuditActions.OperatorPilotAccountsRead,
                admin.Email
            );
            var row = Assert.Single(rows);
            Assert.Equal(AdminAuditTargetTypes.OperatorUser, row.TargetType);
            Assert.Equal("list", row.TargetId);
            Assert.Equal(admin.Id, row.ActorAdminUserId);
            Assert.Contains("\"role\":\"Admin\"", row.DetailJson);
            Assert.DoesNotContain("@", row.DetailJson ?? string.Empty);
        }

        [Fact]
        public async Task AuditEventsList_WritesOneReadAuditRow_ForAdmin()
        {
            var admin = await SeedStaffAsync("Admin");

            using var request = Authorized(
                "/api/admin/audit-events?action=trial.approve&take=5",
                admin.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rows = await ReadRowsForActorAsync(
                AdminAuditActions.AdminAuditEventsRead,
                admin.Email
            );
            var row = Assert.Single(rows);
            Assert.Equal(AdminAuditTargetTypes.AdminAuditEvent, row.TargetType);
            Assert.Equal("list", row.TargetId);
            Assert.Equal(admin.Id, row.ActorAdminUserId);
            Assert.Contains("\"action\":\"trial.approve\"", row.DetailJson);
        }

        [Fact]
        public async Task ShopOrdersList_WritesOneReadAuditRow_WithoutSearchText_ForAdmin()
        {
            var admin = await SeedStaffAsync("Admin");

            using var request = Authorized(
                "/api/admin/shop-orders?q=Jane%20Customer&restaurantId=999999",
                admin.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rows = await ReadRowsForActorAsync(
                AdminAuditActions.ShopOrdersRead,
                admin.Email
            );
            var row = Assert.Single(rows);
            Assert.Equal(AdminAuditTargetTypes.ShopOrder, row.TargetType);
            Assert.Equal("list", row.TargetId);
            Assert.Equal(admin.Id, row.ActorAdminUserId);
            Assert.Contains("\"hasSearch\":true", row.DetailJson);
            Assert.Contains("\"restaurantId\":999999", row.DetailJson);
            Assert.DoesNotContain("Jane", row.DetailJson ?? string.Empty);
            Assert.DoesNotContain("Customer", row.DetailJson ?? string.Empty);
        }

        [Theory]
        [InlineData("/api/admin/trial-requests")]
        [InlineData("/api/admin/pilot-accounts")]
        [InlineData("/api/admin/audit-events")]
        [InlineData("/api/admin/shop-orders")]
        public async Task AdminReadList_ForSupport_Returns403_AndWritesNoReadAuditRow(
            string path
        )
        {
            var support = await SeedStaffAsync("Support");

            using var request = Authorized(path, support.Jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var rows = await ReadRowsForActorAsync(
                ReadActionFor(path),
                support.Email
            );
            Assert.Empty(rows);
        }

        private static HttpRequestMessage Authorized(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private async Task<int> CountAuditAsync(string action, string targetId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await context.AdminAuditEvents
                .AsNoTracking()
                .CountAsync(e => e.Action == action && e.TargetId == targetId);
        }

        private async Task AssertLatestAuditAsync(
            string action,
            string targetId,
            string targetType,
            string actorEmail,
            int actorStaffId
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var audit = await context.AdminAuditEvents
                .AsNoTracking()
                .Where(e => e.Action == action && e.TargetId == targetId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .FirstAsync();

            Assert.Equal(targetType, audit.TargetType);
            Assert.Equal(actorEmail, audit.ActorIdentity);
            Assert.Equal(actorStaffId, audit.ActorAdminUserId);
        }

        private async Task<SeededOperator> SeedOperatorWithActivationCodeAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var configuration = scope.ServiceProvider
                .GetRequiredService<IConfiguration>();

            var plainCode = ActivationCodeHelper.GeneratePlainCode();
            var operatorUser = new User
            {
                FullName = "Activation Read Operator",
                Email = $"activation-read-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900150",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
                ActivationCodeHash = ActivationCodeHelper.HashCode(plainCode),
                ActivationCodeEncrypted = ActivationCodeProtectionHelper.Encrypt(
                    plainCode,
                    configuration["JwtSettings:Secret"]!
                ),
            };
            context.Users.Add(operatorUser);
            await context.SaveChangesAsync();

            var admin = await SeedAdminRowAsync(context, "Admin");
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            return new SeededOperator(
                operatorUser.Id,
                jwtService.GenerateAdminToken(admin),
                admin.Email,
                admin.Id
            );
        }

        private async Task<SeededStaff> SeedStaffAsync(string role)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var staff = await SeedAdminRowAsync(context, role);
            return new SeededStaff(
                staff.Id,
                staff.Email,
                jwtService.GenerateAdminToken(staff)
            );
        }

        private static async Task<Admin> SeedAdminRowAsync(
            ApplicationDbContext context,
            string role
        )
        {
            var admin = new Admin
            {
                FullName = $"Read Audit {role}",
                Email = $"read-audit-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();
            return admin;
        }

        private async Task<List<AdminAuditEvent>> ReadRowsForActorAsync(
            string action,
            string actorIdentity
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await context.AdminAuditEvents
                .AsNoTracking()
                .Where(e => e.Action == action && e.ActorIdentity == actorIdentity)
                .ToListAsync();
        }

        private static string ReadActionFor(string path)
        {
            return path switch
            {
                "/api/admin/trial-requests" => AdminAuditActions.TrialRequestsRead,
                "/api/admin/pilot-accounts" => AdminAuditActions.OperatorPilotAccountsRead,
                "/api/admin/audit-events" => AdminAuditActions.AdminAuditEventsRead,
                "/api/admin/shop-orders" => AdminAuditActions.ShopOrdersRead,
                _ => throw new ArgumentOutOfRangeException(nameof(path), path, null),
            };
        }

        private async Task SeedTrialRequestAsync(string fullName, string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.TrialRequests.Add(new TrialRequest
            {
                BusinessName = "Read Audit Cafe",
                BusinessCategory = "cafe",
                Locations = "1",
                FullName = fullName,
                Email = email,
                Mobile = "07700900160",
                MainLocation = "1 High Street",
                TownCity = "Leeds",
                Postcode = "LS1 1AA",
                Role = "Owner",
                Goal = "Grow",
                TermsAccepted = true,
                IsEmailVerified = true,
                IsApproved = false,
                Status = TrialRequestStatus.EmailVerified,
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        private async Task<int> SeedHelpCentreQueryAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var query = new HelpCentreQuery
            {
                Topic = HelpCentreQueryTopic.StartingWithTummly,
                SubmitterName = "Read Audit Submitter",
                SubmitterEmail = "read-audit-submitter@example.com",
                BusinessName = "Read Audit Venue",
                Status = HelpCentreQueryStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.HelpCentreQueries.Add(query);
            await context.SaveChangesAsync();
            return query.Id;
        }

        private sealed record SeededOperator(
            int OperatorUserId,
            string AdminJwt,
            string AdminEmail,
            int AdminId
        );

        private sealed record SeededStaff(int Id, string Email, string Jwt);
    }
}
