namespace TummlyBackend.Models
{
    public sealed record AssistantHomeKpiEvidence(
        int FeedbackSubmitted,
        int FeedbackSubmittedPrevious,
        int GuestsJoined,
        int GuestsJoinedPrevious,
        int QrScans,
        int QrScansPrevious
    )
    {
        public static AssistantHomeKpiEvidence Empty { get; } =
            new(0, 0, 0, 0, 0, 0);

        public bool IsEmpty =>
            FeedbackSubmitted == 0 && GuestsJoined == 0 && QrScans == 0;
    }
}
