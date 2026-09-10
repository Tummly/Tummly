using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Durable, signature-verified Revolut webhook input. HTTP receipt only
    /// writes this row; a hosted worker applies the event with its own lifetime.
    /// </summary>
    public sealed class RevolutWebhookInboxItem
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string PayloadHash { get; set; } = string.Empty;

        public string RawBody { get; set; } = string.Empty;

        [MaxLength(16)]
        public string Status { get; set; } = RevolutWebhookInboxStatuses.Pending;

        public int AttemptCount { get; set; }

        public DateTime? ClaimedAtUtc { get; set; }

        public DateTime? RetryAfterUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        [MaxLength(2000)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class RevolutWebhookInboxStatuses
    {
        public const string Pending = "pending";

        public const string Completed = "completed";
    }
}
