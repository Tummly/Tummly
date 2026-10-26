using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only guest-response fact recorded on Confirm Send. Activity uses
    /// the fact as source of truth. Email-channel rows also track
    /// <see cref="EmailDeliveryStatus"/> for Guest response email delivery
    /// (ADR-0026). SMS stays fact-only (NotApplicable).
    /// AuthorDisplayName and MaskedDestination are snapshotted at create.
    /// </summary>
    public class FeedbackGuestResponse
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public FeedbackGuestResponseChannel Channel { get; set; }

        public FeedbackRecoveryIntent Intent { get; set; }

        [Required]
        [MaxLength(200)]
        public string MaskedDestination { get; set; }
            = string.Empty;

        [MaxLength(300)]
        public string? Subject { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Body { get; set; }
            = string.Empty;

        [MaxLength(80)]
        public string? Purpose { get; set; }

        [MaxLength(80)]
        public string? Tone { get; set; }

        [MaxLength(1000)]
        public string? IncludeNotes { get; set; }

        public int? AuthorUserId { get; set; }

        public User? AuthorUser { get; set; }

        [Required]
        [MaxLength(150)]
        public string AuthorDisplayName { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        /// <summary>
        /// Durable Guest response email delivery state. Not operator-visible.
        /// Default NotApplicable covers historical stub rows and SMS.
        /// </summary>
        public GuestResponseEmailDeliveryStatus EmailDeliveryStatus { get; set; }
            = GuestResponseEmailDeliveryStatus.NotApplicable;

        /// <summary>
        /// Soft-claim lease stamp for Pending delivery work. Null when
        /// unclaimed or after Accepted / failed attempt release.
        /// </summary>
        public DateTime? EmailDeliveryClaimedAt { get; set; }

        /// <summary>How many times this Pending row has been soft-claimed.</summary>
        public int EmailDeliveryAttemptCount { get; set; }

        /// <summary>Earliest UTC time a failed Pending may be claimed again.</summary>
        public DateTime? EmailDeliveryRetryAfter { get; set; }

        /// <summary>UTC when Resend accepted the mail (Accepted only).</summary>
        public DateTime? EmailDeliveredAt { get; set; }

        /// <summary>
        /// Ledger hold identity for Recovery SMS (ticket 22). Empty for Email.
        /// </summary>
        [MaxLength(128)]
        public string? BillingReservationRef { get; set; }

        /// <summary>
        /// Provider message id (Resend email id or Twilio SID). Null until the
        /// provider accepts the reply, or when no id is returned.
        /// </summary>
        [MaxLength(128)]
        public string? ProviderMessageId { get; set; }

        /// <summary>Operator-visible delivery state for this reply.</summary>
        public GuestReplyDeliveryState DeliveryState { get; set; }
            = GuestReplyDeliveryState.Pending;

        /// <summary>UTC when <see cref="DeliveryState"/> last changed.</summary>
        public DateTime? DeliveryStateAtUtc { get; set; }
    }
}
