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
        /// JSON array of DetectedIssue keys when Succeeded (may be <c>[]</c>).
        /// Null while Pending or Failed — never invent issues.
        /// </summary>
        [MaxLength(500)]
        public string? DetectedIssuesJson { get; set; }

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

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}

