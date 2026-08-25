using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class RestaurantMembership
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        /// <summary>
        /// Restaurant permission role. Not User.Role.
        /// </summary>
        [Required]
        [MaxLength(40)]
        public string PermissionRole { get; set; } = PermissionRoles.Owner;

        public LocationScopeKind LocationScope { get; set; }
            = LocationScopeKind.AllLocations;

        /// <summary>
        /// JSON array of Owned location ids when LocationScope is NamedList.
        /// Empty array is illegal for a NamedList that an actor may save.
        /// </summary>
        [Required]
        public string NamedLocationIdsJson { get; set; } = "[]";

        public MembershipStatus Status { get; set; }
            = MembershipStatus.Active;
    }

    public enum MembershipStatus
    {
        Active = 0,
        Deactivated = 1,
    }

    public enum LocationScopeKind
    {
        AllLocations = 0,
        NamedList = 1,
    }

    public static class PermissionRoles
    {
        public const string Owner = "Owner";
        public const string Admin = "Admin";
        public const string AreaManager = "Area Manager";
        public const string LocationManager = "Location Manager";
        public const string Marketing = "Marketing";
        public const string Staff = "Staff";
        public const string BillingAdmin = "Billing Admin";
        public const string ReportingOnly = "Reporting Only";
    }
}
