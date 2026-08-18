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
        /// Durable Location Guest marketing preference for this Owned location.
        /// </summary>
        public LocationGuestMarketingPreference MarketingPreference { get; set; }
            = LocationGuestMarketingPreference.Allowed;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public ICollection<Feedback> Feedbacks { get; set; }
            = new List<Feedback>();

        public ICollection<LocationGuestTag> GuestTags { get; set; }
            = new List<LocationGuestTag>();

        public ICollection<LocationGuestNote> Notes { get; set; }
            = new List<LocationGuestNote>();
    }
}
