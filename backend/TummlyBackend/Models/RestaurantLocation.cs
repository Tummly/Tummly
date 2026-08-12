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

        /*
         =========================================
         OPERATOR HOME CHECKLIST ACKNOWLEDGEMENTS
         =========================================
        */

        public DateTime? GuestFormPreviewedAt { get; set; }

        public DateTime? QrPlacementGuideViewedAt { get; set; }

        public DateTime? LogoUploadedAt { get; set; }

        /*
         =========================================
         CAPTURE LOCATION STATUS
         =========================================
        */

        /// <summary>
        /// Whether Capture is enabled for this Owned location as a whole.
        /// Default Active for existing rows.
        /// </summary>
        public CaptureLocationStatus CaptureLocationStatus { get; set; }
            = CaptureLocationStatus.Active;

        /// <summary>
        /// JSON array of QR code ids to restore when Activate location capture
        /// runs (codes that were Active when Pause location capture ran).
        /// Null/empty when none remembered.
        /// </summary>
        public string? CaptureLocationPauseRestoreQrCodeIdsJson { get; set; }

        /*
         =========================================
         GUEST FORM THANK-YOU CATALOG ATTACH
         =========================================
        */

        /// <summary>
        /// Optional Offers catalog definition attached to Guest form thank-you
        /// for this Owned location. Null = no thank-you Issue on submit.
        /// Persisted even if the offer later becomes non-Active; issue path
        /// treats non-Active as null.
        /// </summary>
        public int? ThankYouCatalogOfferId { get; set; }

        public CatalogOffer? ThankYouCatalogOffer { get; set; }
    }
}