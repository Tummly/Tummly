using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class LocationGuest
    {
        public int Id { get; set; }

        public int MasterGuestId { get; set; }

        public MasterGuest? MasterGuest { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }
            = string.Empty;

        /// <summary>
        /// Durable Location Guest offers opt-out for this Owned location.
        /// </summary>
        public bool OffersOptOut { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public ICollection<Feedback> Feedbacks { get; set; }
            = new List<Feedback>();
    }
}
