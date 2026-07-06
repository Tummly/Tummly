using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class HelpCentreQueryMessage
    {
        public int Id { get; set; }

        public int QueryId { get; set; }

        public HelpCentreQuery Query { get; set; } = null!;

        public HelpCentreQueryAuthorKind AuthorKind { get; set; }

        public int? AuthorUserId { get; set; }

        public int? AuthorStaffId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
