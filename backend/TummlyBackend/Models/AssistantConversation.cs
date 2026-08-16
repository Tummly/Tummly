using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class AssistantConversation
    {
        public int Id { get; set; }

        public int OwnerUserId { get; set; }

        public User OwnerUser { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int OwnedLocationId { get; set; }

        public RestaurantLocation OwnedLocation { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string OwnedLocationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string ReportingPeriodKind { get; set; } = "preset";

        [MaxLength(32)]
        public string? ReportingPeriodPresetId { get; set; }

        [MaxLength(10)]
        public string? ReportingPeriodStartDate { get; set; }

        [MaxLength(10)]
        public string? ReportingPeriodEndDate { get; set; }

        public bool IsArchived { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// JSON array of Owned location ids from the last Compare turn.
        /// Null when the next question should use saved Analysis scope.
        /// </summary>
        public string? LastCompareLocationIdsJson { get; set; }

        public string? DraftInterviewJson { get; set; }

        public ICollection<AssistantMessage> Messages { get; set; }
            = new List<AssistantMessage>();
    }
}
