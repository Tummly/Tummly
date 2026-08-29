using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Tracks Recovery SMS Confirm/Send idempotency and open holds (ticket 22).
    /// </summary>
    public class RecoverySmsSendIdempotency
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        public int FeedbackId { get; set; }

        public Feedback Feedback { get; set; } = null!;

        [Required]
        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string ReservationRef { get; set; } = string.Empty;

        public int ReservedUnits { get; set; }

        public DateTime ReservedAtUtc { get; set; }

        public DateTime HoldExpiresAtUtc { get; set; }

        public int? CompletedGuestResponseId { get; set; }

        public FeedbackGuestResponse? CompletedGuestResponse { get; set; }

        public DateTime? CompletedAtUtc { get; set; }
    }
}
