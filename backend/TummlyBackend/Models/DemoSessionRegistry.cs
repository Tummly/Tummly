using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Public product-demo visitor session registry for TTL cleanup (§15).
    /// </summary>
    public class DemoSessionRegistry
    {
        [Key]
        public Guid SessionId { get; set; }

        public int OwnerUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }
    }
}
