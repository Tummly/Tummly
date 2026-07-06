using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class HelpCentreQuery
    {
        public int Id { get; set; }

        public HelpCentreQueryTopic Topic { get; set; }

        [Required]
        [MaxLength(150)]
        public string SubmitterName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string SubmitterEmail { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public User? User { get; set; }

        public int? RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public HelpCentreQueryStatus Status { get; set; }
            = HelpCentreQueryStatus.New;

        [MaxLength(2000)]
        public string? EscalationNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HelpCentreQueryMessage> Messages { get; set; }
            = new List<HelpCentreQueryMessage>();
    }
}
