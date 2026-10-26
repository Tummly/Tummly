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
    }
}
