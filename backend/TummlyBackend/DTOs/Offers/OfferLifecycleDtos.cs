namespace TummlyBackend.DTOs.Offers
{
    /// <summary>
    /// Offer Details Claims tab row (ticket 40).
    /// </summary>
    public sealed class OfferDetailsClaimListItemDto
    {
        public string Id { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public int? GuestId { get; init; }

        public string ClaimCode { get; init; } = string.Empty;

        public DateTime? ClaimedAtUtc { get; init; }

        public DateTime IssuedAtUtc { get; init; }

        public string Source { get; init; } = string.Empty;

        public string SourceLabel { get; init; } = string.Empty;

        public string? CampaignName { get; init; }

        public string LocationName { get; init; } = string.Empty;

        public DateTime ExpiryAtUtc { get; init; }

        public string Status { get; init; } = string.Empty;

        public string StatusLabel { get; init; } = string.Empty;

        public string PassCodeMasked { get; init; } = string.Empty;

        public string OfferTitle { get; init; } = string.Empty;

        public string? LinkedCampaignText { get; init; }
    }

    /// <summary>
    /// Offer Details Redemptions tab row (ticket 40).
    /// </summary>
    public sealed class OfferDetailsRedemptionListItemDto
    {
        public string Id { get; init; } = string.Empty;

        /// <summary>redeemed | failed</summary>
        public string Kind { get; init; } = string.Empty;

        public DateTime DateTimeUtc { get; init; }

        public string GuestName { get; init; } = string.Empty;

        public int? GuestId { get; init; }

        public string PassReferenceText { get; init; } = string.Empty;

        public string PassId { get; init; } = string.Empty;

        public string PassCodeMasked { get; init; } = string.Empty;

        public string LocationName { get; init; } = string.Empty;

        public string? StaffMemberText { get; init; }

        public string Outcome { get; init; } = string.Empty;

        public string OutcomeLabel { get; init; } = string.Empty;

        public string? Reason { get; init; }

        public string? ReasonLabel { get; init; }

        public string OfferVersionLabel { get; init; } = string.Empty;

        public DateTime? ExpiresAtUtc { get; init; }

        public string? LinkedCampaignText { get; init; }

        public string OfferTitle { get; init; } = string.Empty;
    }

    public sealed class OfferDetailsClaimsListDto
    {
        public IReadOnlyList<OfferDetailsClaimListItemDto> Items { get; init; }
            = Array.Empty<OfferDetailsClaimListItemDto>();
    }

    public sealed class OfferDetailsRedemptionsListDto
    {
        public IReadOnlyList<OfferDetailsRedemptionListItemDto> Items { get; init; }
            = Array.Empty<OfferDetailsRedemptionListItemDto>();
    }
}
