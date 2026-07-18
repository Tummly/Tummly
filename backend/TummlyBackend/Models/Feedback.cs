using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        /*
         =========================================
         LOCATION RELATION (per-location feedback — ADR-0003)
         =========================================
        */

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        /*
         =========================================
         GUEST FIELDS (3 required — ADR-0003)
         =========================================
        */

        [Required]
        [MaxLength(150)]
        public string GuestName { get; set; }
            = string.Empty;

        [Required]
        [MaxLength(100)]
        public string GuestContact { get; set; }
            = string.Empty;

        public ContactType ContactType { get; set; }
            = ContactType.Unknown;

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; }
            = string.Empty;

        /*
         =========================================
         AI CLASSIFICATION (Pending → Succeeded | Failed)
         =========================================
        */

        public ClassificationStatus ClassificationStatus { get; set; }
            = ClassificationStatus.Pending;

        /// <summary>Set only when ClassificationStatus is Succeeded.</summary>
        public FeedbackSentiment? Sentiment { get; set; }

        /// <summary>
        /// JSON array of DetectedTag keys when Succeeded (may be <c>[]</c>).
        /// Null while Pending or Failed — never invent tags.
        /// </summary>
        [MaxLength(500)]
        public string? DetectedTagsJson { get; set; }

        /// <summary>
        /// Soft-claim lease stamp for durable classification work (ADR-0010).
        /// Null when unclaimed or after a terminal write. Not a product lifecycle status.
        /// </summary>
        public DateTime? ClassificationClaimedAt { get; set; }

        /// <summary>
        /// How many times this Pending row has been soft-claimed.
        /// Distinct from provider HTTP <c>MaxAttempts</c>. Exhaustion → Failed.
        /// </summary>
        public int ClassificationClaimAttempts { get; set; }

        /// <summary>
        /// When true, delayed auto-requeue may reopen Failed → Pending (ADR-0012).
        /// Implementation detail — not a product lifecycle status.
        /// </summary>
        public bool ClassificationRetryable { get; set; }

        /// <summary>
        /// Earliest UTC time a retryable Failed may reopen to Pending.
        /// </summary>
        public DateTime? ClassificationRetryAfter { get; set; }

        /// <summary>
        /// How many delayed Failed→Pending reopen cycles have completed.
        /// Cap is <c>FeedbackClassificationSettings.MaxDelayedReopens</c>.
        /// </summary>
        public int ClassificationDelayedReopenCount { get; set; }

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}

