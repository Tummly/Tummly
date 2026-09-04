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
        /// Workspace-week identity: <c>{weekStartsOn}:{yyyy-MM-dd}</c> for the
        /// coverage start local date (see <c>WeeklyBriefWeekKey</c>). Legacy
        /// Monday ISO keys <c>yyyy-Www</c> may still exist.
        /// </summary>
        [Required]
        [MaxLength(32)]
        public string WeekKey { get; set; } = string.Empty;

        public WeeklyBriefStatus Status { get; set; }
            = WeeklyBriefStatus.Succeeded;

        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>
        /// Structured brief body JSON (
        /// <see cref="WeeklyBriefBody"/> / <c>WeeklyBriefStructuredOutput</c> schema v1).
        /// </summary>
        [Required]
        public string BodyJson { get; set; } = string.Empty;

        /// <summary>
        /// Echoed aggregate metrics JSON (
        /// <see cref="WeeklyBriefMetrics"/>) fed into / returned with generation.
        /// </summary>
        [Required]
        public string MetricsJson { get; set; } = string.Empty;

        /// <summary>
        /// Optional error payload when a failed attempt is retained.
        /// Prefer omitting the row until success instead.
        /// </summary>
        public string? ErrorInfo { get; set; }

        /// <summary>
        /// When an operator marked this location+week brief as reviewed (annotation).
        /// Null until first mark. Re-mark refreshes this timestamp.
        /// </summary>
        public DateTime? ReviewedAtUtc { get; set; }

        /// <summary>
        /// User who last marked this brief as reviewed. Null until first mark.
        /// </summary>
        public int? ReviewedByUserId { get; set; }

        public User? ReviewedByUser { get; set; }
    }

    public enum WeeklyBriefStatus
    {
        Succeeded = 0,
        Failed = 1,
    }

    public static class WeeklyBriefStatusExtensions
    {
        public const string SucceededWire = "succeeded";
        public const string FailedWire = "failed";

        public static string ToWireString(this WeeklyBriefStatus status) =>
            status switch
            {
                WeeklyBriefStatus.Succeeded => SucceededWire,
                WeeklyBriefStatus.Failed => FailedWire,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unknown Weekly brief status."
                ),
            };

        public static WeeklyBriefStatus FromWireString(string stored)
        {
            if (!TryFromWireString(stored, out var status))
            {
                if (string.IsNullOrWhiteSpace(stored))
                {
                    throw new ArgumentException(
                        "Weekly brief status is required.",
                        nameof(stored)
                    );
                }

                throw new ArgumentOutOfRangeException(
                    nameof(stored),
                    stored,
                    "Unknown Weekly brief status."
                );
            }

            return status;
        }

        public static bool TryFromWireString(
            string? stored,
            out WeeklyBriefStatus status
        )
        {
            status = default;
            if (string.IsNullOrWhiteSpace(stored))
            {
                return false;
            }

            switch (stored.Trim())
            {
                case SucceededWire:
                    status = WeeklyBriefStatus.Succeeded;
                    return true;
                case FailedWire:
                    status = WeeklyBriefStatus.Failed;
                    return true;
                default:
                    return false;
            }
        }
    }
}
