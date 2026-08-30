using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Cached Assistant billed-AI HTTP outcome for Idempotency-Key replay (24h).
    /// </summary>
    public class AssistantAiActionOutcome
    {
        [Key]
        public Guid Id { get; set; }

        public int RestaurantId { get; set; }

        public BillingAccount BillingAccount { get; set; } = null!;

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public string ResponseJson { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }

    public static class AssistantAiBillingRules
    {
        public const int CompletedAnswerUnits = 1;

        public static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);
    }
}
