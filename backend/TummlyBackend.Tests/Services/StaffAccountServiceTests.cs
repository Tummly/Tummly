using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class StaffAccountServiceTests : IDisposable
    {
        private const string CurrentPassword = "Current-Password-42";
        private const string NewPassword = "Brand-New-Password-7";

        private readonly ApplicationDbContext _context;
        private readonly AdjustableTimeProvider _clock;
        private readonly StaffAccountService _sut;

        public StaffAccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 10, 28, 9, 0, 0, TimeSpan.Zero));
            _sut = new StaffAccountService(
                _context,
                new AdminAuditService(_context, _clock),
                new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()
                )
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task ChangeOwnPassword_WithCorrectCurrent_StoresNewHashAndAudits()
        {
            var staff = await AddStaffAsync("support@example-staff.com", "Support");

            var outcome = await _sut.ChangeOwnPasswordAsync(staff.Id, CurrentPassword, NewPassword);

            Assert.True(outcome.Succeeded);
            var stored = await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == staff.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, stored.PasswordHash));
            Assert.False(BCrypt.Net.BCrypt.Verify(CurrentPassword, stored.PasswordHash));

            var audit = await _context.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditActions.StaffPasswordChange);
            Assert.Equal("staff_account", audit.TargetType);
            Assert.Equal(staff.Id.ToString(), audit.TargetId);
            Assert.Equal(staff.Email, audit.ActorIdentity);
            Assert.Equal(staff.Id, audit.ActorAdminUserId);
        }

        [Fact]
        public async Task ChangeOwnPassword_AuditDetail_NeverHoldsAPasswordOrHash()
        {
            var staff = await AddStaffAsync("admin-two@example-staff.com", "Admin");

            await _sut.ChangeOwnPasswordAsync(staff.Id, CurrentPassword, NewPassword);

            var audits = await _context.AdminAuditEvents.ToListAsync();
            Assert.NotEmpty(audits);
            foreach (var audit in audits)
            {
                var text = (audit.DetailJson ?? string.Empty) + audit.ActorIdentity + audit.TargetId;
                Assert.DoesNotContain(CurrentPassword, text);
                Assert.DoesNotContain(NewPassword, text);
                Assert.DoesNotContain("$2", text);
            }
        }

        [Fact]
        public async Task ChangeOwnPassword_WrongCurrent_ReturnsCurrentPasswordInvalid_AndNeverCountsTowardLock()
        {
            var staff = await AddStaffAsync("support-lock@example-staff.com", "Support");
            var before = (await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == staff.Id)).PasswordHash;

            for (var attempt = 0; attempt < StaffAccountService.MaxWrongCurrentPasswords; attempt++)
            {
                var outcome = await _sut.ChangeOwnPasswordAsync(staff.Id, "Wrong-Current-Password", NewPassword);

                Assert.False(outcome.Succeeded);
                Assert.Equal("current_password_invalid", outcome.Code);
            }

            // Throttled now, even with the right current password; the login lock is untouched.
            var throttled = await _sut.ChangeOwnPasswordAsync(staff.Id, CurrentPassword, NewPassword);
            Assert.Equal("too_many_attempts", throttled.Code);

            var stored = await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == staff.Id);
            Assert.Equal(before, stored.PasswordHash);
            Assert.Equal(0, stored.FailedLoginAttempts);
            Assert.False(stored.IsLocked);
            Assert.Empty(await _context.AdminAuditEvents.ToListAsync());
        }

        [Fact]
        public async Task ChangeOwnPassword_SameAsCurrent_IsRefused()
        {
            var staff = await AddStaffAsync("same-pass@example-staff.com", "Support");

            var outcome = await _sut.ChangeOwnPasswordAsync(staff.Id, CurrentPassword, CurrentPassword);

            Assert.False(outcome.Succeeded);
            Assert.Equal("new_password_same_as_current", outcome.Code);
        }

        [Fact]
        public async Task ChangeOwnPassword_DevelopmentDefault_IsRefused()
        {
            var staff = await AddStaffAsync("support-default@example-staff.com", "Support");

            var outcome = await _sut.ChangeOwnPasswordAsync(staff.Id, CurrentPassword, StaffSeedPolicy.DevelopmentSupportPassword);

            Assert.False(outcome.Succeeded);
            Assert.Equal(StaffPasswordPolicy.DevelopmentDefault, outcome.Code);
        }

        [Fact]
        public async Task ChangeOwnPassword_TooShort_IsRefusedWithoutChangingHash()
        {
            var staff = await AddStaffAsync("short-pass@example-staff.com", "Admin");
            var before = (await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == staff.Id)).PasswordHash;

            var outcome = await _sut.ChangeOwnPasswordAsync(staff.Id, CurrentPassword, "short-1");

            Assert.False(outcome.Succeeded);
            Assert.Equal(StaffPasswordPolicy.TooShort, outcome.Code);
            Assert.Equal(before, (await _context.Admins.AsNoTracking().SingleAsync(a => a.Id == staff.Id)).PasswordHash);
        }

        [Fact]
        public async Task ChangeOwnPassword_UnknownStaff_ReturnsNotFound()
        {
            var outcome = await _sut.ChangeOwnPasswordAsync(987654, CurrentPassword, NewPassword);

            Assert.False(outcome.Succeeded);
            Assert.Equal("staff_not_found", outcome.Code);
        }

        private async Task<Admin> AddStaffAsync(string email, string role)
        {
            var admin = new Admin
            {
                FullName = "Staff Member",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(CurrentPassword),
                Role = role,
                IsActive = true,
                IsLocked = false,
                FailedLoginAttempts = 0,
                CreatedAt = _clock.GetUtcNow().UtcDateTime,
            };
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
            return admin;
        }
    }
}
