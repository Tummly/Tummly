using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class TeamInvitation
    {
        public const int LifetimeDays = 7;

        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string PermissionRole { get; set; } = string.Empty;

        public LocationScopeKind LocationScope { get; set; }
            = LocationScopeKind.AllLocations;

        [Required]
        public string NamedLocationIdsJson { get; set; } = "[]";

        [MaxLength(2000)]
        public string? Message { get; set; }

        public int InviterUserId { get; set; }

        public User InviterUser { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        [Required]
        [MaxLength(128)]
        public string OpaqueReference { get; set; } = string.Empty;

        public string? PendingPasswordHash { get; set; }
    }
}
