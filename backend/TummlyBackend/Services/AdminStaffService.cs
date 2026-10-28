using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminStaffService : IAdminStaffService
    {
        private const int ReasonMinLength = 5;
        private const int ReasonMaxLength = 500;
        private const int FullNameMaxLength = 150;
        private const int EmailMaxLength = 254;
        private const string AdminRole = "Admin";

        private static readonly string[] StaffRoles = [AdminRole, "Support"];

        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;
        private readonly TimeProvider _clock;

        public AdminStaffService(
            ApplicationDbContext context,
            IAdminAuditService audit,
            TimeProvider clock
        )
        {
            _context = context;
            _audit = audit;
            _clock = clock;
        }

        public async Task<IReadOnlyList<AdminStaffMemberDto>> ListAsync(
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.Admins
                .AsNoTracking()
                .OrderBy(row => row.Id)
                .ToListAsync(cancellationToken);

            return rows.Select(ToDto).ToList();
        }

        public async Task<StaffOutcome<AdminStaffMemberDto>> CreateAsync(
            int actorAdminId,
            AdminStaffCreateRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var fullName = request.FullName?.Trim() ?? string.Empty;
            if (fullName.Length is < 1 or > FullNameMaxLength)
            {
                return Fail("full_name_invalid", $"Enter a full name of 1 to {FullNameMaxLength} characters.");
            }

            var email = NormalizeEmail(request.Email);
            if (email == null)
            {
                return Fail("email_invalid", "Enter a valid email address.");
            }

            var role = request.Role;
            if (role == null || Array.IndexOf(StaffRoles, role) < 0)
            {
                return Fail("role_invalid", "Choose the Admin or Support role.");
            }

            var cleanReason = NormalizeReason(request.Reason);
            if (cleanReason == null)
            {
                return ReasonInvalid();
            }

            var ruleCode = StaffPasswordPolicy.Validate(request.InitialPassword, email);
            if (ruleCode != null)
            {
                return Fail(ruleCode, StaffPasswordPolicy.Message(ruleCode));
            }

            if (await EmailInUseAsync(email, cancellationToken))
            {
                return Fail("email_in_use", "This email is already used by a staff or operator account.");
            }

            var admin = new Admin
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.InitialPassword!),
                Role = role,
                IsActive = true,
                IsLocked = false,
                FailedLoginAttempts = 0,
                CreatedAt = _clock.GetUtcNow().UtcDateTime,
            };

            // The audit row needs the identity value, so the insert and the
            // audit row share one transaction on relational providers.
            await using var transaction =
                _context.Database.IsRelational()
                    ? await _context.Database.BeginTransactionAsync(cancellationToken)
                    : null;

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync(cancellationToken);

            await AppendAuditAsync(
                AdminAuditActions.StaffCreate,
                actorAdminId,
                admin,
                cleanReason,
                before: null,
                after: Snapshot(admin),
                cancellationToken
            );
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(admin));
        }

        public async Task<StaffOutcome<AdminStaffMemberDto>> DisableAsync(
            int actorAdminId,
            int targetId,
            string? reason,
            CancellationToken cancellationToken = default
        )
        {
            var cleanReason = NormalizeReason(reason);
            if (cleanReason == null)
            {
                return ReasonInvalid();
            }

            var target = await FindTargetAsync(targetId, cancellationToken);
            if (target == null)
            {
                return NotFound();
            }

            if (target.Id == actorAdminId)
            {
                return Fail("cannot_disable_self", "You cannot disable your own staff account.");
            }

            if (!target.IsActive)
            {
                return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
            }

            if (target.Role == AdminRole
                && !await UsableAdminOtherThanAsync(target.Id, cancellationToken))
            {
                return Fail("last_active_admin", "At least one other active Admin must remain before this Admin can be disabled.");
            }

            var before = Snapshot(target);
            target.IsActive = false;

            await SaveWithAuditAsync(
                AdminAuditActions.StaffDisable,
                actorAdminId,
                target,
                cleanReason,
                before,
                cancellationToken
            );

            return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
        }

        public async Task<StaffOutcome<AdminStaffMemberDto>> EnableAsync(
            int actorAdminId,
            int targetId,
            string? reason,
            CancellationToken cancellationToken = default
        )
        {
            var cleanReason = NormalizeReason(reason);
            if (cleanReason == null)
            {
                return ReasonInvalid();
            }

            var target = await FindTargetAsync(targetId, cancellationToken);
            if (target == null)
            {
                return NotFound();
            }

            if (target.IsActive)
            {
                return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
            }

            var before = Snapshot(target);
            target.IsActive = true;

            await SaveWithAuditAsync(
                AdminAuditActions.StaffEnable,
                actorAdminId,
                target,
                cleanReason,
                before,
                cancellationToken
            );

            return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
        }

        public async Task<StaffOutcome<AdminStaffMemberDto>> UnlockAsync(
            int actorAdminId,
            int targetId,
            string? reason,
            CancellationToken cancellationToken = default
        )
        {
            var cleanReason = NormalizeReason(reason);
            if (cleanReason == null)
            {
                return ReasonInvalid();
            }

            var target = await FindTargetAsync(targetId, cancellationToken);
            if (target == null)
            {
                return NotFound();
            }

            if (!target.IsLocked && target.FailedLoginAttempts == 0)
            {
                return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
            }

            var before = Snapshot(target);
            target.IsLocked = false;
            target.FailedLoginAttempts = 0;

            await SaveWithAuditAsync(
                AdminAuditActions.StaffUnlock,
                actorAdminId,
                target,
                cleanReason,
                before,
                cancellationToken
            );

            return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
        }

        public async Task<StaffOutcome<AdminStaffMemberDto>> ResetPasswordAsync(
            int actorAdminId,
            int targetId,
            string? newPassword,
            string? reason,
            CancellationToken cancellationToken = default
        )
        {
            var cleanReason = NormalizeReason(reason);
            if (cleanReason == null)
            {
                return ReasonInvalid();
            }

            var target = await FindTargetAsync(targetId, cancellationToken);
            if (target == null)
            {
                return NotFound();
            }

            if (target.Id == actorAdminId)
            {
                return Fail(
                    "cannot_reset_own_password",
                    "Use Change password to set your own password."
                );
            }

            var ruleCode = StaffPasswordPolicy.Validate(newPassword, target.Email);
            if (ruleCode != null)
            {
                return Fail(ruleCode, StaffPasswordPolicy.Message(ruleCode));
            }

            var before = Snapshot(target);
            target.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword!);
            // A reset also clears the lock, so the new password works at once.
            target.IsLocked = false;
            target.FailedLoginAttempts = 0;

            await SaveWithAuditAsync(
                AdminAuditActions.StaffPasswordReset,
                actorAdminId,
                target,
                cleanReason,
                before,
                cancellationToken
            );

            return StaffOutcome<AdminStaffMemberDto>.Ok(ToDto(target));
        }

        private async Task<Admin?> FindTargetAsync(int targetId, CancellationToken cancellationToken) =>
            await _context.Admins.FirstOrDefaultAsync(row => row.Id == targetId, cancellationToken);

        /// <summary>
        /// True when another Admin can still sign in: active, unlocked and not the target.
        /// A locked or disabled Admin does not count as a fallback.
        /// </summary>
        private Task<bool> UsableAdminOtherThanAsync(int targetId, CancellationToken cancellationToken) =>
            _context.Admins.AnyAsync(
                row => row.Id != targetId
                    && row.Role == AdminRole
                    && row.IsActive
                    && !row.IsLocked,
                cancellationToken
            );

        private async Task<bool> EmailInUseAsync(string email, CancellationToken cancellationToken)
        {
            // Users is checked too: UniversalLoginAsync tries staff first, so a
            // staff row with an operator's email would take over that sign-in.
            return await _context.Admins.AnyAsync(row => row.Email.ToLower() == email, cancellationToken)
                || await _context.Users.AnyAsync(row => row.Email.ToLower() == email, cancellationToken);
        }

        private async Task SaveWithAuditAsync(
            string action,
            int actorAdminId,
            Admin target,
            string reason,
            object before,
            CancellationToken cancellationToken
        )
        {
            await AppendAuditAsync(
                action,
                actorAdminId,
                target,
                reason,
                before,
                Snapshot(target),
                cancellationToken
            );
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task AppendAuditAsync(
            string action,
            int actorAdminId,
            Admin target,
            string reason,
            object? before,
            object after,
            CancellationToken cancellationToken
        )
        {
            // Never add the password, its hash or a raw token to this detail.
            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: action,
                    ActorIdentity: await ActorIdentityAsync(actorAdminId, cancellationToken),
                    TargetType: AdminAuditTargetTypes.StaffAccount,
                    TargetId: target.Id.ToString(),
                    ActorAdminUserId: actorAdminId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            role = target.Role,
                            reason,
                            before,
                            after,
                        }
                    )
                )
            );
        }

        private async Task<string> ActorIdentityAsync(int adminId, CancellationToken cancellationToken)
        {
            var email = await _context.Admins
                .AsNoTracking()
                .Where(row => row.Id == adminId)
                .Select(row => row.Email)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(email) ? $"admin:{adminId}" : email;
        }

        private static string? NormalizeReason(string? reason)
        {
            var trimmed = reason?.Trim() ?? string.Empty;
            return trimmed.Length is >= ReasonMinLength and <= ReasonMaxLength
                ? trimmed
                : null;
        }

        private static StaffOutcome<AdminStaffMemberDto> ReasonInvalid() => Fail(
            "reason_invalid",
            $"Enter a reason of {ReasonMinLength} to {ReasonMaxLength} characters."
        );

        private static string? NormalizeEmail(string? raw)
        {
            var normalized = raw?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized) || normalized.Length > EmailMaxLength)
            {
                return null;
            }

            return new EmailAddressAttribute().IsValid(normalized) ? normalized : null;
        }

        private static object Snapshot(Admin admin) => new
        {
            isActive = admin.IsActive,
            isLocked = admin.IsLocked,
        };

        private static AdminStaffMemberDto ToDto(Admin admin) => new(
            admin.Id,
            admin.FullName,
            admin.Email,
            admin.Role,
            admin.IsActive,
            admin.IsLocked,
            admin.FailedLoginAttempts,
            admin.CreatedAt,
            StaffSeedPolicy.UsesDevelopmentDefaultPassword(admin.Email, admin.PasswordHash)
        );

        private static StaffOutcome<AdminStaffMemberDto> Fail(string code, string message) =>
            StaffOutcome<AdminStaffMemberDto>.Fail(code, message);

        private static StaffOutcome<AdminStaffMemberDto> NotFound() =>
            StaffOutcome<AdminStaffMemberDto>.Fail("staff_not_found", "Staff account was not found.");
    }
}
