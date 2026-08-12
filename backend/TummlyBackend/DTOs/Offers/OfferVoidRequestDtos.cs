namespace TummlyBackend.DTOs.Offers
{
    public sealed class CreateOfferVoidRequestBody
    {
        public int IssueId { get; set; }

        public int OfferId { get; set; }

        public int LocationId { get; set; }

        public string ReasonId { get; set; } = string.Empty;

        public string? Explanation { get; set; }

        public string CorrectionId { get; set; } = string.Empty;
    }

    public sealed class OfferVoidRequestDetailDto
    {
        public string RequestId { get; set; } = string.Empty;

        public string PassId { get; set; } = string.Empty;

        public int OfferId { get; set; }

        public int LocationId { get; set; }

        public string OfferTitle { get; set; } = string.Empty;

        public string GuestName { get; set; } = string.Empty;

        public string PassCodeMasked { get; set; } = string.Empty;

        public string CurrentStateText { get; set; } = string.Empty;

        public string ExpiresText { get; set; } = string.Empty;

        public string LocationName { get; set; } = string.Empty;

        public string LinkedCampaignText { get; set; } = string.Empty;

        public string RequestedByText { get; set; } = string.Empty;

        public string RequestedAtText { get; set; } = string.Empty;

        public string ReasonId { get; set; } = string.Empty;

        public string ReasonText { get; set; } = string.Empty;

        public string? Explanation { get; set; }

        public string CorrectionId { get; set; } = string.Empty;

        public string CorrectionText { get; set; } = string.Empty;
    }

    public sealed class OpenVoidAttentionOfferDto
    {
        public int OfferId { get; set; }

        public string OfferTitle { get; set; } = string.Empty;

        public int PendingCount { get; set; }
    }

    /// <summary>
    /// Offer Details Void requests tab row (ticket 41).
    /// </summary>
    public sealed class OfferDetailsVoidRequestListItemDto
    {
        public string RequestId { get; init; } = string.Empty;

        public DateTime RequestedAtUtc { get; init; }

        public string RequestedAtText { get; init; } = string.Empty;

        public string RequestedByText { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public string OfferPassText { get; init; } = string.Empty;

        public string ReasonId { get; init; } = string.Empty;

        public string ReasonText { get; init; } = string.Empty;

        public string? Explanation { get; init; }

        public string LocationName { get; init; } = string.Empty;

        public string CurrentStateText { get; init; } = string.Empty;

        public string CorrectionId { get; init; } = string.Empty;

        public string CorrectionText { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public string StatusLabel { get; init; } = string.Empty;

        public string PassId { get; init; } = string.Empty;

        public string PassCodeMasked { get; init; } = string.Empty;

        public string ExpiresText { get; init; } = string.Empty;

        public string LinkedCampaignText { get; init; } = string.Empty;

        public string OfferTitle { get; init; } = string.Empty;
    }

    public sealed class OfferDetailsVoidRequestsListDto
    {
        public IReadOnlyList<OfferDetailsVoidRequestListItemDto> Items { get; init; }
            = Array.Empty<OfferDetailsVoidRequestListItemDto>();
    }

    public enum OfferVoidCreateResultStatus
    {
        Created,
        PendingExists,
        NotRedeemed,
        NotFound,
        Invalid,
    }

    public sealed class OfferVoidCreateResult
    {
        public OfferVoidCreateResultStatus Status { get; init; }

        public string? RequestId { get; init; }
    }

    public enum OfferVoidOutcomeResultStatus
    {
        Ok,
        NotFound,
        NotPending,
        Failed,
    }

    public sealed class OfferVoidOutcomeResult
    {
        public OfferVoidOutcomeResultStatus Status { get; init; }
    }

    public sealed class NotifyVoidSubmitterBody
    {
        public string? Outcome { get; set; }
    }
}
