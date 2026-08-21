using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Durable Weekly brief row for one Owned location + closed ISO week.
    /// Prefer no row until a successful generate; absence means not yet generated.
    /// Successful rows are immutable (no overwrite after success).
    /// </summary>
    public class WeeklyBrief
    {
        public int Id { get; set; }

        /// <summary>Owned location (<see cref="RestaurantLocation.Id"/>).</summary>
        public int LocationId { get; set; }

        public RestaurantLocation? Location { get; set; }

        /// <summary>
        /// ISO week key <c>yyyy-Www</c> for the closed prior Mon–Sun week in the
        /// location timezone (see <c>WeeklyBriefWeekKey</c>).
        /// </summary>
        [Required]
        [MaxLength(16)]
        public string WeekKey { get; set; } = string.Empty;

        public WeeklyBriefStatus Status { get; set; }
            = WeeklyBriefStatus.Succeeded;

        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>Structured brief body JSON (schema owned by later tickets).</summary>
        [Required]
        public string BodyJson { get; set; } = string.Empty;

        /// <summary>Echoed aggregate metrics JSON fed into / returned with generation.</summary>
        [Required]
        public string MetricsJson { get; set; } = string.Empty;

        /// <summary>
        /// Optional error payload when a failed attempt is retained.
        /// Prefer omitting the row until success instead.
        /// </summary>
        public string? ErrorInfo { get; set; }
    }

    public enum WeeklyBriefStatus
    {
        Succeeded = 0,
        Failed = 1,
    }
}
