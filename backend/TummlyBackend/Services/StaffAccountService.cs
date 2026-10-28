using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class StaffAccountService : IStaffAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;

        public StaffAccountService(
            ApplicationDbContext context,
            IAdminAuditService audit
        )
        {
            _context = context;
            _audit = audit;
        }

        /// <summary>
        /// Staff change their own password. A wrong current password is a plain
        /// 400 and does not touch FailedLoginAttempts, so it cannot lock the account.
        /// Neither the password nor its hash is written to the audit row.
        /// </summary>
        public async Task<StaffOutcome<bool>> ChangeOwnPasswordAsync(
            int adminId,
            string? currentPassword,
            string? newPassword,
            CancellationToken cancellationToken = default
        )
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(row => row.Id == adminId, cancellationToken);

            if (admin == null)
            {
                return StaffOutcome<bool>.Fail(
                    "staff_not_found",
                    "Staff account was not found."
                );
            }

            if (
                string.IsNullOrEmpty(currentPassword)
                || !BCrypt.Net.BCrypt.Verify(currentPassword, admin.PasswordHash)
            )
            {
                return StaffOutcome<bool>.Fail(
                    "current_password_invalid",
                    "The current password is not correct."
                );
            }

            var ruleCode = StaffPasswordPolicy.Validate(newPassword, admin.Email);
            if (ruleCode != null)
            {
                return StaffOutcome<bool>.Fail(ruleCode, StaffPasswordPolicy.Message(ruleCode));
            }

            if (string.Equals(newPassword, currentPassword, StringComparison.Ordinal))
            {
                return StaffOutcome<bool>.Fail(
                    "new_password_same_as_current",
                    "Choose a password that is different from the current one."
                );
            }

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword!);

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.StaffPasswordChange,
                    ActorIdentity: admin.Email,
                    TargetType: AdminAuditTargetTypes.StaffAccount,
                    TargetId: admin.Id.ToString(),
                    ActorAdminUserId: admin.Id,
                    DetailJson: JsonSerializer.Serialize(new { role = admin.Role })
                )
            );

            await _context.SaveChangesAsync(cancellationToken);

            return StaffOutcome<bool>.Ok(true);
        }
    }
}
