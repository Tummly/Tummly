using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Unique webhook claim keyed by Revolut <c>(event, object_id)</c>
    /// (lock 04 / ticket 15). Insert before side effects in the same
    /// transaction as apply.
    /// </summary>
    public class RevolutWebhookEventClaim
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Event { get; set; } = string.Empty;

        [MaxLength(128)]
        public string ObjectId { get; set; } = string.Empty;

        /// <summary>
        /// Outcome of this claim: <c>recorded</c>, <c>skipped_terminal</c>,
        /// <c>applied</c> (ticket 16+), or later skip reasons.
        /// </summary>
        [MaxLength(64)]
        public string Disposition { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class RevolutWebhookClaimDispositions
    {
        public const string Recorded = "recorded";

        public const string SkippedTerminal = "skipped_terminal";

        public const string Applied = "applied";
    }
}
