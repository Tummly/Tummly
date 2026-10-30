using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public enum TrustEvidenceClass
    {
        VerifiedPublicRecord,
        VerifiedSelfAssessment,
        Roadmap,
        OwnControlQaPending,
    }

    /// <summary>Canonical per-item status (CONTRACT §1). Public status is derived, never stored.</summary>
    public enum TrustIndicatorStatus
    {
        VerifiedPublicRecord,
        VerifiedCredential,
        Roadmap,
        QaPending,
        QaPassed,
        UnderReview,
    }

    public enum TrustCtaType
    {
        None,
        ExternalProof,
        InternalPage,
        CookieSettings,
    }

    /// <summary>
    /// One of the 15 trust indicators. ItemNumber and evidence class are fixed by the seed;
    /// the editable columns hold the draft until a publication copies them.
    /// </summary>
    public class TrustIndicator
    {
        [Key]
        public int Id { get; set; }

        public int ItemNumber { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public TrustEvidenceClass EvidenceClass { get; set; }

        [MaxLength(64)]
        public string? IconKey { get; set; }

        [MaxLength(300)]
        public string AltText { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Tooltip { get; set; } = string.Empty;

        public TrustIndicatorStatus Status { get; set; } = TrustIndicatorStatus.QaPending;

        [MaxLength(60)]
        public string StatusLabel { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(300)]
        public string? ImageRightsNote { get; set; }

        public TrustCtaType CtaType { get; set; }

        [MaxLength(60)]
        public string? CtaLabel { get; set; }

        [MaxLength(500)]
        public string? CtaTarget { get; set; }

        public bool Enabled { get; set; }

        [MaxLength(500)]
        public string? EvidenceUrl { get; set; }

        [MaxLength(200)]
        public string? Issuer { get; set; }

        public DateOnly? IssuedOn { get; set; }

        public DateOnly? ExpiresOn { get; set; }

        public DateOnly? ReviewOn { get; set; }

        [MaxLength(300)]
        public string? Scope { get; set; }

        public int? LastEditedByAdminUserId { get; set; }

        public DateTime? LastEditedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Single row (Id = 1) for the draft as a whole: who edited it last and who submitted it for review.
    /// </summary>
    public class TrustIndicatorDraftState
    {
        [Key]
        public int Id { get; set; }

        public int? LastEditedByAdminUserId { get; set; }

        public DateTime? LastEditedAtUtc { get; set; }

        public int? SubmittedByAdminUserId { get; set; }

        public DateTime? SubmittedAtUtc { get; set; }
    }

    /// <summary>
    /// One published version. The snapshot is the full 15-item list at publish time and is never edited.
    /// </summary>
    public class TrustIndicatorPublication
    {
        [Key]
        public int Id { get; set; }

        public int Version { get; set; }

        public DateTime PublishedAtUtc { get; set; }

        public int PublishedByAdminUserId { get; set; }

        public int ApprovedByAdminUserId { get; set; }

        public int? SubmittedByAdminUserId { get; set; }

        public DateTime? SubmittedAtUtc { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public string SnapshotJson { get; set; } = string.Empty;
    }
}
