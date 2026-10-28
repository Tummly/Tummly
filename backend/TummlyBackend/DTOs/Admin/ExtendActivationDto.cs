using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Admin
{
    public class ExtendActivationDto
    {
        /// <summary>
        /// Pilot extension length. Only 14 or 30 are accepted.
        /// </summary>
        public int Days { get; set; }

        /// <summary>
        /// Same key on a retry replays the last extension. Stored on the billing account.
        /// </summary>
        [MaxLength(64)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Restaurant to extend. Must belong to the operator in the route.
        /// When omitted, the operator's lowest-Id Pilot restaurant is extended.
        /// </summary>
        public int? RestaurantId { get; set; }

        /// <summary>
        /// Required. Trimmed on the server and stored in the audit detail.
        /// Must be 5 to 500 characters after trimming.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Required. The extension count the operator saw. A different stored count
        /// is refused as <c>stale_pilot_version</c>. A missing value is also refused.
        /// </summary>
        public int? ExpectedExtensionCount { get; set; }
    }
}
