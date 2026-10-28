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
    public sealed class AdminStaffEndpointsTests : IDisposable
    {
        private const string ExistingPassword = "Existing-Password-42";
        private const string NewPassword = "Brand-New-Password-7";

        private readonly TummlyWebApplicationFactory _factory = new();
        private readonly HttpClient _client;

        public AdminStaffEndpointsTests()
        {
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Fact]
        public async Task StaffList_WithoutToken_Returns401()
        {
            using var response = await _client.GetAsync("/api/admin/staff");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task StaffList_AsOperatorOwner_Returns403()
        {
            var owner = GenerateOperatorToken("owner@restaurant.example", "Owner");

            using var response = await SendAsync(HttpMethod.Get, "/api/admin/staff", owner);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task StaffList_AsSupport_Returns403()
        {
            var staff = await SeedStaffAsync();

            using var response = await SendAsync(HttpMethod.Get, "/api/admin/staff", staff.SupportJwt);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task StaffCreate_AsSupport_Returns403()
        {
            var staff = await SeedStaffAsync();

            using var response = await SendAsync(
                HttpMethod.Post,
                "/api/admin/staff",
                staff.SupportJwt,
                CreateBody("rota-two@tummly.com")
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task StaffCreate_AsAdmin_CreatesSupportAccount_AndDuplicateEmailReturns409()
        {
            var staff = await SeedStaffAsync();

            using var created = await SendAsync(
                HttpMethod.Post,
                "/api/admin/staff",
                staff.AdminJwt,
                CreateBody("rota-two@tummly.com")
            );
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var body = await created.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("rota-two@tummly.com", body.GetProperty("item").GetProperty("email").GetString());
            Assert.False(body.GetProperty("item").TryGetProperty("passwordHash", out _));

            using var duplicate = await SendAsync(
                HttpMethod.Post,
                "/api/admin/staff",
                staff.AdminJwt,
                CreateBody("ROTA-TWO@tummly.com")
            );
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            var denied = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("email_in_use", denied.GetProperty("code").GetString());
        }

        [Fact]
        public async Task StaffDisable_Self_Returns409()
        {
            var staff = await SeedStaffAsync();

            using var response = await SendAsync(
                HttpMethod.Post,
                $"/api/admin/staff/{staff.AdminId}/disable",
                staff.AdminJwt,
                new { reason = "Going on leave" }
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("cannot_disable_self", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task DisabledStaffToken_Returns401OnTheNextAdminRequest()
        {
            var staff = await SeedStaffAsync();

            using var disable = await SendAsync(
                HttpMethod.Post,
                $"/api/admin/staff/{staff.SecondAdminId}/disable",
                staff.AdminJwt,
                new { reason = "Left the company" }
            );
            Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

            using var stillDenied = await SendAsync(HttpMethod.Get, "/api/admin/staff", staff.SecondAdminJwt);
            Assert.Equal(HttpStatusCode.Unauthorized, stillDenied.StatusCode);

            using var support = await SendAsync(HttpMethod.Get, "/api/admin/trust-indicators", staff.SupportJwt);
            Assert.Equal(HttpStatusCode.Forbidden, support.StatusCode);
        }

        [Fact]
        public async Task LockedStaffToken_Returns401OnTheNextAdminRequest()
        {
            var staff = await SeedStaffAsync();
            await SetLockedAsync(staff.SupportId, locked: true);

            using var response = await SendAsync(HttpMethod.Get, "/api/admin/trust-indicators", staff.SupportJwt);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task StaffToken_ForDeletedAccount_Returns401()
        {
            var staff = await SeedStaffAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Admins.Remove(await context.Admins.SingleAsync(a => a.Id == staff.SupportId));
                await context.SaveChangesAsync();
            }

            using var response = await SendAsync(HttpMethod.Get, "/api/admin/trust-indicators", staff.SupportJwt);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task OperatorToken_IsNotCheckedAgainstStaffRows()
        {
            // No Admins row exists for this operator id. The token must still
            // pass authentication and fail only on the role check (403, not 401).
            var operatorToken = GenerateOperatorToken("operator-user@restaurant.example", "Owner");

            using var response = await SendAsync(HttpMethod.Get, "/api/admin/trust-indicators", operatorToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMyPassword_AsSupport_Returns200_AndWrongCurrentReturns400()
        {
            var staff = await SeedStaffAsync();

            using var wrong = await SendAsync(
                HttpMethod.Post,
                "/api/staff/me/password",
                staff.SupportJwt,
                new { currentPassword = "Not-The-Password-1", newPassword = NewPassword }
            );
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
            var wrongBody = await wrong.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("current_password_invalid", wrongBody.GetProperty("code").GetString());

            using var ok = await SendAsync(
                HttpMethod.Post,
                "/api/staff/me/password",
                staff.SupportJwt,
                new { currentPassword = ExistingPassword, newPassword = NewPassword }
            );
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await context.Admins.AsNoTracking().SingleAsync(a => a.Id == staff.SupportId);
            Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, stored.PasswordHash));
        }

        [Fact]
        public async Task ChangeMyPassword_WithoutToken_Returns401()
        {
            using var response = await _client.PostAsJsonAsync(
                "/api/staff/me/password",
                new { currentPassword = ExistingPassword, newPassword = NewPassword }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static object CreateBody(string email) => new
        {
            fullName = "Rota Two",
            email,
            role = "Support",
            initialPassword = NewPassword,
            reason = "Joined the support rota",
        };

        private string GenerateOperatorToken(string email, string role)
        {
            using var scope = _factory.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            return jwtService.GenerateToken("424242", email, role);
        }

        private async Task SetLockedAsync(int id, bool locked)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var row = await context.Admins.SingleAsync(a => a.Id == id);
            row.IsLocked = locked;
            row.FailedLoginAttempts = locked ? 5 : 0;
            await context.SaveChangesAsync();
        }

        private async Task<StaffTokens> SeedStaffAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var admin = NewStaff("boss-" + Guid.NewGuid().ToString("N") + "@tummly.com", "Admin");
            var secondAdmin = NewStaff("second-" + Guid.NewGuid().ToString("N") + "@tummly.com", "Admin");
            var support = NewStaff("support-" + Guid.NewGuid().ToString("N") + "@tummly.com", "Support");
            context.Admins.AddRange(admin, secondAdmin, support);
            await context.SaveChangesAsync();

            return new StaffTokens(
                AdminId: admin.Id,
                AdminJwt: jwtService.GenerateAdminToken(admin),
                SecondAdminId: secondAdmin.Id,
                SecondAdminJwt: jwtService.GenerateAdminToken(secondAdmin),
                SupportId: support.Id,
                SupportJwt: jwtService.GenerateAdminToken(support)
            );
        }

        private static Admin NewStaff(string email, string role) => new()
        {
            FullName = "Staff Member",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ExistingPassword),
            Role = role,
            IsActive = true,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
        };

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string url,
            string jwt,
            object? body = null
        )
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }
            return await _client.SendAsync(request);
        }

        private sealed record StaffTokens(
            int AdminId,
            string AdminJwt,
            int SecondAdminId,
            string SecondAdminJwt,
            int SupportId,
            string SupportJwt
        );
    }
}
