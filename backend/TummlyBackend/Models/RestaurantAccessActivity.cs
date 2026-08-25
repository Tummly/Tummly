using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class AccessActivityKinds
    {
        public const string InvitationSent = "invitation-sent";
        public const string InvitationResent = "invitation-resent";
        public const string InvitationRevoked = "invitation-revoked";
        public const string InvitationAccepted = "invitation-accepted";
        public const string RoleChanged = "role-changed";
        public const string LocationScopeChanged = "location-scope-changed";
        public const string MemberDeactivated = "member-deactivated";
        public const string MemberReactivated = "member-reactivated";
        public const string MemberRemoved = "member-removed";
        public const string PermissionCellChanged = "permission-cell-changed";
    }

    public class RestaurantAccessActivity
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        public int ActorUserId { get; set; }

        [MaxLength(150)]
        public string? ActorDisplayName { get; set; }

        public int? TargetUserId { get; set; }

        [MaxLength(150)]
        public string? TargetDisplayName { get; set; }

        [MaxLength(200)]
        public string? TargetEmail { get; set; }

        public int? TargetMembershipId { get; set; }

        [Required]
        [MaxLength(40)]
        public string Kind { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? FromValue { get; set; }

        [MaxLength(400)]
        public string? ToValue { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
