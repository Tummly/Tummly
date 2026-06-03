using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        /*
         =========================================
         RESTAURANT NAME
         =========================================
        */

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
            = string.Empty;

        /*
         =========================================
         ACCOUNT TYPE
         Single = 1 location
         Multi = 2-5 OR 6+
         =========================================
        */

        public string AccountType { get; set; }
            = "Single";

        /*
         =========================================
         OWNER USER
         =========================================
        */

        public int OwnerUserId { get; set; }

        public User OwnerUser { get; set; }

        /*
         =========================================
         CREATED DATE
         =========================================
        */




        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        /*
         =========================================
         LOCATIONS
         =========================================
        */

        public ICollection<RestaurantLocation>
            Locations
        { get; set; }


            = new List<RestaurantLocation>();
        public GuestLoopSetup? GuestLoopSetup { get; set; }

        public string? BusinessCategory { get; set; }

        public string? BusinessLink { get; set; }

        public string? PublicPhoneNumber { get; set; }

    }



}