using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Onboarding
{
    public class CreateRestaurantSetupDto
    {
        /*
         =========================================
         RESTAURANT
         =========================================
        */

        [Required]
        public string RestaurantName { get; set; }
            = string.Empty;

        /*
         =========================================
         LOCATION
         =========================================
        */

        [Required]
        public string LocationName { get; set; }
            = string.Empty;

        public string Address { get; set; }
            = string.Empty;

        public string Postcode { get; set; }
            = string.Empty;

        /*
         =========================================
         BUSINESS
         =========================================
        */

        public string? Phone { get; set; }

        public string? BusinessLink { get; set; }

        public string? BusinessCategory { get; set; }
    }
}