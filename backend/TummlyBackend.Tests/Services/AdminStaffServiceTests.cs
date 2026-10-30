using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class AdminStaffServiceTests : IDisposable
    {
        private const string ExistingPassword = "Existing-Password-42";
        private const string ValidPassword = "Fresh-Staff-Password-9";
        private const string ValidReason = "Joined the support rota";

        private readonly ApplicationDbContext _context;
        private readonly AdjustableTimeProvider _clock;
        private readonly AdminStaffService _sut;

        public AdminStaffServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 10, 28, 9, 0, 0, TimeSpan.Zero));
            _sut = new AdminStaffService(
                _context,
                new AdminAuditService(_context, _clock),
                _clock
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task Create_WithValidInput_AddsActiveStaffAndAuditsWithoutPassword()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");

            var outcome = await _sut.CreateAsync(actor.Id, new AdminStaffCreateRequest(
                FullName: "  New Person ",
                Email: "  New.Person@Tummly.com ",
                Role: "Support",
                InitialPassword: ValidPassword,
                Reason: ValidReason));

            Assert.True(outcome.Succeeded);
            Assert.Equal("new.person@tummly.com", outcome.Value!.Email);
            Assert.Equal("New Person", outcome.Value.FullName);
            Assert.True(outcome.Value.IsActive);
            Assert.False(outcome.Value.IsLocked);

            var created = await _context.Admins.AsNoTracking().SingleAsync(a => a.Email == "new.person@tummly.com");
            Assert.Equal("Support", created.Role);
            Assert.True(BCrypt.Net.BCrypt.Verify(ValidPassword, created.PasswordHash));

            var audit = await _context.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditActions.StaffCreate);
            Assert.Equal(actor.Email, audit.ActorIdentity);
            Assert.Equal(actor.Id, audit.ActorAdminUserId);
            Assert.Equal(AdminAuditTargetTypes.StaffAccount, audit.TargetType);
            Assert.Equal(created.Id.ToString(), audit.TargetId);
            AssertNoSecrets(audit, ValidPassword, created.PasswordHash);
        }

        [Fact]
        public async Task Create_EmailAlreadyInAdmins_IgnoringCase_Returns409EmailInUse()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");

            var outcome = await _sut.CreateAsync(actor.Id, Request(email: "BOSS@tummly.com"));

            Assert.False(outcome.Succeeded);
            Assert.Equal("email_in_use", outcome.Code);
            Assert.Equal(1, await _context.Admins.CountAsync());
        }

        [Fact]
        public async Task Create_EmailAlreadyInOperatorUsers_Returns409EmailInUse()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            _context.Users.Add(new User
            {
                FullName = "Operator Owner",
                Email = "owner@restaurant.example",
                PhoneNumber = "07000000000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _clock.GetUtcNow().UtcDateTime,
            });
            await _context.SaveChangesAsync();

            var outcome = await _sut.CreateAsync(actor.Id, Request(email: "Owner@Restaurant.example"));

            Assert.False(outcome.Succeeded);
            Assert.Equal("email_in_use", outcome.Code);
            Assert.Equal(1, await _context.Admins.CountAsync());
        }

        [Theory]
        [InlineData("short-1")]
        [InlineData("Admin@123")]
        public async Task Create_WeakOrDevelopmentDefaultPassword_IsRefused(string password)
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");

            var outcome = await _sut.CreateAsync(actor.Id, Request(password: password));

            Assert.False(outcome.Succeeded);
            Assert.Contains(outcome.Code, new[] { StaffPasswordPolicy.TooShort, StaffPasswordPolicy.DevelopmentDefault });
            Assert.Equal(1, await _context.Admins.CountAsync());
        }

        [Fact]
        public async Task Create_PasswordEqualToEmail_IsRefused()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var email = "mirror-password@tummly.com";

            var outcome = await _sut.CreateAsync(actor.Id, Request(email: email, password: email));

            Assert.False(outcome.Succeeded);
            Assert.Equal(StaffPasswordPolicy.MatchesEmail, outcome.Code);
        }

        [Fact]
        public async Task Create_RoleOtherThanAdminOrSupport_IsRefused()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");

            var outcome = await _sut.CreateAsync(actor.Id, Request(role: "Finance"));

            Assert.False(outcome.Succeeded);
            Assert.Equal("role_invalid", outcome.Code);
        }

        [Theory]
        [InlineData("")]
        [InlineData("abcd")]
        public async Task Writes_ReasonShorterThanFiveCharacters_AreRefused(string reason)
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Admin");

            var outcome = await _sut.DisableAsync(actor.Id, target.Id, reason);

            Assert.False(outcome.Succeeded);
            Assert.Equal("reason_invalid", outcome.Code);
            Assert.True((await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == target.Id)).IsActive);
        }

        [Fact]
        public async Task Writes_ReasonLongerThan500Characters_IsRefused()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Admin");

            var outcome = await _sut.DisableAsync(actor.Id, target.Id, new string('r', 501));

            Assert.False(outcome.Succeeded);
            Assert.Equal("reason_invalid", outcome.Code);
        }

        [Fact]
        public async Task Disable_Self_Returns409CannotDisableSelf()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            await AddStaffAsync("second@tummly.com", "Admin");

            var outcome = await _sut.DisableAsync(actor.Id, actor.Id, ValidReason);

            Assert.False(outcome.Succeeded);
            Assert.Equal("cannot_disable_self", outcome.Code);
            Assert.True((await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == actor.Id)).IsActive);
        }

        [Fact]
        public async Task Disable_LastUsableAdmin_Returns409LastActiveAdmin()
        {
            // The actor here is a Support row, so the only Admin left after the
            // disable would be the target itself. The guard must refuse it.
            var support = await AddStaffAsync("support-actor@tummly.com", "Support");
            var lastAdmin = await AddStaffAsync("only-admin@tummly.com", "Admin");
            await AddStaffAsync("retired-admin@tummly.com", "Admin", active: false);

            var outcome = await _sut.DisableAsync(support.Id, lastAdmin.Id, ValidReason);

            Assert.False(outcome.Succeeded);
            Assert.Equal("last_active_admin", outcome.Code);
            Assert.True((await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == lastAdmin.Id)).IsActive);
        }

        [Fact]
        public async Task Disable_LockedOtherAdminDoesNotCountAsAvailable_Returns409LastActiveAdmin()
        {
            var support = await AddStaffAsync("support-actor@tummly.com", "Support");
            var target = await AddStaffAsync("target@tummly.com", "Admin");
            await AddStaffAsync("locked-admin@tummly.com", "Admin", locked: true);

            var outcome = await _sut.DisableAsync(support.Id, target.Id, ValidReason);

            Assert.False(outcome.Succeeded);
            Assert.Equal("last_active_admin", outcome.Code);
        }

        [Fact]
        public async Task Disable_OtherAdminWhenAnotherAdminRemains_DisablesAndAudits()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Admin");

            var outcome = await _sut.DisableAsync(actor.Id, target.Id, ValidReason);

            Assert.True(outcome.Succeeded);
            Assert.False(outcome.Value!.IsActive);
            Assert.False((await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == target.Id)).IsActive);

            var audit = await _context.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditActions.StaffDisable);
            Assert.Equal(target.Id.ToString(), audit.TargetId);
            Assert.Equal(actor.Email, audit.ActorIdentity);
            using var detail = System.Text.Json.JsonDocument.Parse(audit.DetailJson!);
            Assert.Equal(ValidReason, detail.RootElement.GetProperty("reason").GetString());
            Assert.Equal(
                true,
                detail.RootElement.GetProperty("before").GetProperty("isActive").GetBoolean()
            );
            Assert.False(detail.RootElement.GetProperty("after").GetProperty("isActive").GetBoolean());
        }

        [Fact]
        public async Task AdminStaff_DisableClearsApproverFlagAndAudits()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Admin");
            target.CanApproveTrustIndicators = true;
            await _context.SaveChangesAsync();

            var outcome = await _sut.DisableAsync(actor.Id, target.Id, ValidReason);

            Assert.True(outcome.Succeeded);
            Assert.False((await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == target.Id)).CanApproveTrustIndicators);
            var revoke = await _context.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditActions.TrustApproverRevoke);
            Assert.Equal(target.Id.ToString(), revoke.TargetId);
            Assert.Equal(actor.Id, revoke.ActorAdminUserId);
            Assert.Equal(actor.Email, revoke.ActorIdentity);
            Assert.Contains("\"before\":true,\"after\":false", revoke.DetailJson);
            Assert.Single(await _context.AdminAuditEvents.Where(e => e.Action == AdminAuditActions.StaffDisable).ToListAsync());
        }

        [Fact]
        public async Task Disable_WithoutApproverFlag_WritesNoRevokeRow()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Admin");

            Assert.True((await _sut.DisableAsync(actor.Id, target.Id, ValidReason)).Succeeded);

            Assert.False(await _context.AdminAuditEvents.AnyAsync(e => e.Action == AdminAuditActions.TrustApproverRevoke));
        }

        [Fact]
        public async Task Disable_AlreadyDisabled_IsNoOpWithoutAuditRow()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Support", active: false);

            var outcome = await _sut.DisableAsync(actor.Id, target.Id, ValidReason);

            Assert.True(outcome.Succeeded);
            Assert.False(await _context.AdminAuditEvents.AnyAsync(e => e.Action == AdminAuditActions.StaffDisable));
        }

        [Fact]
        public async Task Enable_ReactivatesAndAudits()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("support@tummly.com", "Support", active: false);

            var outcome = await _sut.EnableAsync(actor.Id, target.Id, ValidReason);

            Assert.True(outcome.Succeeded);
            Assert.True(outcome.Value!.IsActive);
            Assert.True((await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == target.Id)).IsActive);
            Assert.True(await _context.AdminAuditEvents.AnyAsync(e => e.Action == AdminAuditActions.StaffEnable));
        }

        [Fact]
        public async Task Unlock_ClearsLockAndAttemptsAndAudits()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("support@tummly.com", "Support", locked: true);

            var outcome = await _sut.UnlockAsync(actor.Id, target.Id, ValidReason);

            Assert.True(outcome.Succeeded);
            var stored = await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == target.Id);
            Assert.False(stored.IsLocked);
            Assert.Equal(0, stored.FailedLoginAttempts);
            var audit = await _context.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditActions.StaffUnlock);
            using var detail = System.Text.Json.JsonDocument.Parse(audit.DetailJson!);
            Assert.True(detail.RootElement.GetProperty("before").GetProperty("isLocked").GetBoolean());
            Assert.False(detail.RootElement.GetProperty("after").GetProperty("isLocked").GetBoolean());
        }

        [Fact]
        public async Task ResetPassword_SetsNewHash_UnlocksAndAuditsWithoutPassword()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("support@tummly.com", "Support", locked: true);

            var outcome = await _sut.ResetPasswordAsync(actor.Id, target.Id, ValidPassword, ValidReason);

            Assert.True(outcome.Succeeded);
            var stored = await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == target.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify(ValidPassword, stored.PasswordHash));
            Assert.False(stored.IsLocked);
            Assert.Equal(0, stored.FailedLoginAttempts);

            var audit = await _context.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditActions.StaffPasswordReset);
            Assert.Equal(target.Id.ToString(), audit.TargetId);
            AssertNoSecrets(audit, ValidPassword, stored.PasswordHash);
        }

        [Fact]
        public async Task ResetPassword_OwnAccount_Returns409CannotResetOwnPassword()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            await AddStaffAsync("second@tummly.com", "Admin");

            var outcome = await _sut.ResetPasswordAsync(actor.Id, actor.Id, ValidPassword, ValidReason);

            Assert.False(outcome.Succeeded);
            Assert.Equal("cannot_reset_own_password", outcome.Code);
        }

        [Fact]
        public async Task ResetPassword_DevelopmentDefault_IsRefused()
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");
            var target = await AddStaffAsync("second@tummly.com", "Admin");

            var outcome = await _sut.ResetPasswordAsync(actor.Id, target.Id, StaffSeedPolicy.DevelopmentAdminPassword, ValidReason);

            Assert.False(outcome.Succeeded);
            Assert.Equal(StaffPasswordPolicy.DevelopmentDefault, outcome.Code);
        }

        [Theory]
        [InlineData("disable")]
        [InlineData("enable")]
        [InlineData("unlock")]
        [InlineData("reset")]
        public async Task StaffWrites_UnknownTarget_Return404(string operation)
        {
            var actor = await AddStaffAsync("boss@tummly.com", "Admin");

            var outcome = operation switch
            {
                "disable" => await _sut.DisableAsync(actor.Id, 555555, ValidReason),
                "enable" => await _sut.EnableAsync(actor.Id, 555555, ValidReason),
                "unlock" => await _sut.UnlockAsync(actor.Id, 555555, ValidReason),
                _ => await _sut.ResetPasswordAsync(actor.Id, 555555, ValidPassword, ValidReason),
            };

            Assert.False(outcome.Succeeded);
            Assert.Equal("staff_not_found", outcome.Code);
        }

        [Fact]
        public async Task List_ReturnsRowsWithoutHashAndFlagsDevelopmentDefaults()
        {
            await AddStaffAsync("boss@tummly.com", "Admin");
            await AddStaffAsync(StaffSeedPolicy.DevelopmentSupportEmail, "Support", password: StaffSeedPolicy.DevelopmentSupportPassword);

            var rows = await _sut.ListAsync();

            Assert.Equal(2, rows.Count);
            Assert.False(rows.Single(r => r.Email == "boss@tummly.com").UsesDefaultPassword);
            Assert.True(rows.Single(r => r.Email == StaffSeedPolicy.DevelopmentSupportEmail).UsesDefaultPassword);
            Assert.DoesNotContain(
                typeof(AdminStaffMemberDto).GetProperties(),
                p => p.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase)
            );
        }

        private static AdminStaffCreateRequest Request(
            string email = "new-staff@tummly.com",
            string role = "Support",
            string password = ValidPassword
        ) => new(
            FullName: "New Staff",
            Email: email,
            Role: role,
            InitialPassword: password,
            Reason: ValidReason);

        private static void AssertNoSecrets(AdminAuditEvent audit, string password, string hash)
        {
            var text = (audit.DetailJson ?? string.Empty) + audit.ActorIdentity + audit.TargetId;
            Assert.DoesNotContain(password, text);
            Assert.DoesNotContain(hash, text);
            Assert.DoesNotContain("$2", text);
        }

        private async Task<Admin> AddStaffAsync(
            string email,
            string role,
            bool active = true,
            bool locked = false,
            string password = ExistingPassword
        )
        {
            var admin = new Admin
            {
                FullName = "Staff Member",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = active,
                IsLocked = locked,
                FailedLoginAttempts = locked ? 5 : 0,
                CreatedAt = _clock.GetUtcNow().UtcDateTime,
            };
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
            return admin;
        }
    }
}
