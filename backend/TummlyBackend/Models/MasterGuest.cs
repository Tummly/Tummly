using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class MasterGuest
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant? Restaurant { get; set; }

        /// <summary>Display email channel; set when ContactType is Email.</summary>
        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>Normalized email identity key (trim + lower).</summary>
        [MaxLength(100)]
        public string? NormalizedEmail { get; set; }

        /// <summary>Display mobile channel; set when ContactType is Phone.</summary>
        [MaxLength(100)]
        public string? Mobile { get; set; }

        /// <summary>Normalized phone identity key (digits only).</summary>
        [MaxLength(100)]
        public string? NormalizedPhone { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public ICollection<LocationGuest> LocationGuests { get; set; }
            = new List<LocationGuest>();
    }
}
