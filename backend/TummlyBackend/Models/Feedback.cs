using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        /*
         =========================================
         LOCATION RELATION (per-location feedback — ADR-0003)
         =========================================
        */

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        /*
         =========================================
         GUEST FIELDS (3 required — ADR-0003)
         =========================================
        */

        [Required]
        [MaxLength(150)]
        public string GuestName { get; set; }
            = string.Empty;

        [Required]
        [MaxLength(100)]
        public string GuestContact { get; set; }
            = string.Empty;

        public ContactType ContactType { get; set; }
            = ContactType.Unknown;

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; }
            = string.Empty;

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
