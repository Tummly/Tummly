using System.Security.Claims;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Builds the audit row for a staff read. Admin and Support both sign in
    /// from the Admins table, so the staff id fits ActorAdminUserId.
    /// </summary>
    public static class StaffReadAudit
    {
        public static AdminAuditAppendRequest Build(
            ClaimsPrincipal user,
            string action,
            string targetType,
            string targetId,
            string? detailJson = null
        )
        {
            int? staffId = int.TryParse(
                user.FindFirstValue(ClaimTypes.NameIdentifier),
                out var parsed
            )
                ? parsed
                : null;

            var identity = user.FindFirstValue(ClaimTypes.Email)
                ?? user.Identity?.Name
                ?? (staffId is int id ? $"admin:{id}" : "unidentified");

            return new AdminAuditAppendRequest(
                Action: action,
                ActorIdentity: identity,
                TargetType: targetType,
                TargetId: targetId,
                ActorAdminUserId: staffId,
                DetailJson: detailJson
            );
        }
    }
}
