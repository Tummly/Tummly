using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class AssistantMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public AssistantConversation Conversation { get; set; } = null!;

        public AssistantMessageRole Role { get; set; }

        public AssistantMessageClass? Class { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public string? ActionsJson { get; set; }

        public int? OwnedLocationId { get; set; }

        [MaxLength(32)]
        public string? ScopeKind { get; set; }

        [MaxLength(200)]
        public string? OwnedLocationName { get; set; }

        [MaxLength(32)]
        public string? ReportingPeriodKind { get; set; }

        [MaxLength(32)]
        public string? ReportingPeriodPresetId { get; set; }

        [MaxLength(10)]
        public string? ReportingPeriodStartDate { get; set; }

        [MaxLength(10)]
        public string? ReportingPeriodEndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
