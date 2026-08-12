using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Operator request to correct a redeemed Offer issue (ticket 39 / 06).
    /// </summary>
    public class OfferVoidRequest
    {
        public int Id { get; set; }

        public int OfferIssueId { get; set; }

        public OfferIssue? OfferIssue { get; set; }

        public int CatalogOfferId { get; set; }

        public CatalogOffer? CatalogOffer { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public int RequestedByUserId { get; set; }

        public User? RequestedByUser { get; set; }

        public DateTime RequestedAtUtc { get; set; }

        /// <summary>RedeemedAtUtc snapshot at create for audit after restore.</summary>
        public DateTime OriginalRedeemedAtUtc { get; set; }

        [Required]
        [MaxLength(48)]
        public string ReasonId { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Explanation { get; set; }

        [Required]
        [MaxLength(32)]
        public string CorrectionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = OfferVoidRequestStatuses.Pending;

        public int? ResolvedByUserId { get; set; }

        public User? ResolvedByUser { get; set; }

        public DateTime? ResolvedAtUtc { get; set; }
    }

    public static class OfferVoidRequestStatuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
    }

    public static class OfferVoidRequestReasonIds
    {
        public const string RedeemedByMistake = "redeemed_by_mistake";
        public const string WrongOfferPass = "wrong_offer_pass";
        public const string DuplicateRedemption = "duplicate_redemption";
        public const string GuestDidNotReceive = "guest_did_not_receive";
        public const string IncorrectLocation = "incorrect_location";
        public const string Other = "other";

        public static readonly HashSet<string> All = new(StringComparer.Ordinal)
        {
            RedeemedByMistake,
            WrongOfferPass,
            DuplicateRedemption,
            GuestDidNotReceive,
            IncorrectLocation,
            Other,
        };
    }

    public static class OfferVoidRequestCorrectionIds
    {
        public const string KeepUnusable = "keep_unusable";
        public const string RestoreOneUse = "restore_one_use";

        public static readonly HashSet<string> All = new(StringComparer.Ordinal)
        {
            KeepUnusable,
            RestoreOneUse,
        };
    }
}
