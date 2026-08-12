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
