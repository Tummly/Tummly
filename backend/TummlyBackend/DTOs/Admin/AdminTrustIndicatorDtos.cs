namespace TummlyBackend.DTOs.Admin
{
    /// <summary>
    /// Fields an authorised editor may change on the draft. Also the PUT body.
    /// Title, item number and evidence class are fixed by the seed.
    /// </summary>
    public sealed record TrustIndicatorEditableValues
    {
        public bool Enabled { get; init; }

        public string? CtaType { get; init; }

        public string? CtaTarget { get; init; }

        public string? EvidenceUrl { get; init; }

        public string? ImageUrl { get; init; }

        public string? IconKey { get; init; }

        public string? AltText { get; init; }

        public string? Tooltip { get; init; }

        public string? Status { get; init; }

        public string? StatusLabel { get; init; }

        public string? CtaLabel { get; init; }

        public string? ImageRightsNote { get; init; }

        public string? Issuer { get; init; }

        public string? Scope { get; init; }

        public DateOnly? IssuedOn { get; init; }

        public DateOnly? ExpiresOn { get; init; }

        public DateOnly? ReviewOn { get; init; }
    }

    public sealed record TrustIndicatorItemDto(
        int ItemNumber,
        string Title,
        string EvidenceClass,
        TrustIndicatorEditableValues Values,
        int? LastEditedByAdminUserId,
        DateTime? LastEditedAtUtc
    );

    public sealed record TrustIndicatorReviewDto(
        int? LastEditedByAdminUserId,
        DateTime? LastEditedAtUtc,
        int? SubmittedByAdminUserId,
        DateTime? SubmittedAtUtc
    );

    public sealed record TrustIndicatorPublicationDto(
        int Version,
        DateTime PublishedAtUtc,
        int PublishedByAdminUserId,
        int ApprovedByAdminUserId,
        int? SubmittedByAdminUserId,
        DateTime? SubmittedAtUtc,
        string Reason,
        IReadOnlyList<TrustIndicatorItemDto> Items
    );

    /// <summary>The viewing admin and whether they may publish. Drives the publish control.</summary>
    public sealed record TrustIndicatorViewerDto(int AdminUserId, bool CanApprove);

    public sealed record AdminTrustIndicatorsViewDto(
        IReadOnlyList<TrustIndicatorItemDto> Draft,
        TrustIndicatorReviewDto Review,
        TrustIndicatorPublicationDto? Published,
        TrustIndicatorViewerDto Viewer
    );

    public sealed class AdminTrustIndicatorPublishRequest
    {
        public string? Reason { get; set; }
    }

    /// <summary>Public read model. Evidence fields are shown only when published and live.</summary>
    public sealed record PublicTrustIndicatorDto(
        int ItemNumber,
        string Title,
        string EvidenceClass,
        string? IconKey,
        string? ImageUrl,
        string AltText,
        string Tooltip,
        string CtaType,
        string? CtaTarget,
        string? EvidenceUrl,
        string? Issuer,
        DateOnly? IssuedOn,
        DateOnly? ExpiresOn,
        string? Scope
    );
}
