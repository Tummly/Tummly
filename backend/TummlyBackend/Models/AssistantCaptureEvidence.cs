namespace TummlyBackend.Models
{
    public sealed record AssistantCaptureEvidence(
        int QrScans,
        int QrScansPrevious,
        int FeedbackSubmitted,
        int FeedbackSubmittedPrevious,
        int MarketingOptIns,
        int MarketingOptInsPrevious,
        IReadOnlyList<AssistantCaptureQrRow> QrRows
    )
    {
        public static AssistantCaptureEvidence Empty { get; } =
            new(0, 0, 0, 0, 0, 0, []);

        public bool HasSnapshotFacts =>
            QrRows.Count > 0
            || QrScans > 0
            || FeedbackSubmitted > 0
            || MarketingOptIns > 0;
    }

    public sealed record AssistantCaptureQrRow(
        int QrCodeId,
        string QrType,
        string Status,
        int QrScans,
        int FeedbackSubmitted,
        int MarketingOptIns
    );
}
