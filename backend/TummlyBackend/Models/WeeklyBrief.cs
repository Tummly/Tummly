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
