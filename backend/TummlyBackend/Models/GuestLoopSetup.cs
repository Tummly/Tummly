using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class GuestLoopSetup
    {
        public int Id { get; set; }

        /*
         =========================================
         RESTAURANT LINK
         =========================================
        */

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; }

        /*
         =========================================
         QR MATERIALS & AUTOMATION
         =========================================
        */

        public bool SendPhysicalQrMaterials { get; set; }
            = false;

        public bool AutoSendReviewRequests { get; set; }
            = true;

        /*
         =========================================
         FRONTEND STEP 3 INTEGRATION FIELDS
         =========================================
        */

        public string? Touchpoints { get; set; } // Save as comma-separated or JSON string

        public string? FeedbackTags { get; set; } // Save as comma-separated or JSON string

        public string? ThankYouMessage { get; set; }

        [MaxLength(150)]
        public string? OfferHeadline { get; set; }

        public string? OfferDetails { get; set; }

        [MaxLength(50)]
        public string? OfferExpiry { get; set; }

        [MaxLength(50)]
        public string? OfferRedemption { get; set; }

        [MaxLength(50)]
        public string? OfferUsageLimit { get; set; }

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}