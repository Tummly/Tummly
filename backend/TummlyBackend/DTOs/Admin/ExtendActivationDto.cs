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
    }
}
