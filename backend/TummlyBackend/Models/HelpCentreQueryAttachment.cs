using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class HelpCentreQueryAttachment
    {
        public int Id { get; set; }

        public int QueryId { get; set; }

        public HelpCentreQuery Query { get; set; } = null!;

        [Required]
        [MaxLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(127)]
        public string ContentType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        [Required]
        [MaxLength(500)]
        public string StorageKey { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
