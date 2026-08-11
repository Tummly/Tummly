using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Failed staff Check / Redeem attempt attributed to a catalog offer.
    /// Write path is ticket 25; metrics read counts AttemptedAtUtc in window.
    /// </summary>
    public class OfferRedeemFailedAttempt
    {
        public int Id { get; set; }

        public int CatalogOfferId { get; set; }

        public CatalogOffer? CatalogOffer { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public DateTime AttemptedAtUtc { get; set; }

        [MaxLength(32)]
        public string? ClaimCode { get; set; }

        [MaxLength(200)]
        public string? Reason { get; set; }
    }
}
