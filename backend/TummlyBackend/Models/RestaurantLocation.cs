using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class RestaurantLocation
    {
        public int Id { get; set; }

        /*
         =========================================
         LOCATION NAME
         =========================================
        */

        [Required]
        [MaxLength(200)]
        public string LocationName { get; set; }
            = string.Empty;

        /*
         =========================================
         ADDRESS
         =========================================
        */

        [MaxLength(500)]
        public string Address { get; set; }
            = string.Empty;

        /*
         =========================================
         POSTCODE
         =========================================
        */

        public string? Postcode { get; set; }
         
        /*
         =========================================
         RESTAURANT RELATION
         =========================================
        */


        public int RestaurantId { get; set; }

        public Restaurant? Restaurant { get; set; }

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
             = DateTime.UtcNow;


        public string? LocationPhone { get; set; }

        public string? LocalContact { get; set; }

        public bool IncludeInRollout { get; set; }
    }
}