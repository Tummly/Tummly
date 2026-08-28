using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Cached billed AI draft outcome keyed by operator <c>Idempotency-Key</c>.
    /// TTL 24 hours (ticket 05 / 21).
    /// </summary>
    public class AiActionIdempotencyRecord
    {
        [Key]
        public Guid Id { get; set; }

        public int RestaurantId { get; set; }

        public BillingAccount BillingAccount { get; set; } = null!;

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [MaxLength(64)]
        public string PackKey { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Subject { get; set; }

        [MaxLength(16)]
        public string Channel { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
