namespace TummlyBackend.Models
{
    public sealed record AssistantRetrievedEvidence(
        AssistantFeedbackEvidence Feedback,
        AssistantOffersEvidence Offers,
        AssistantCampaignsEvidence Campaigns,
        AssistantCaptureEvidence Capture,
        AssistantHomeKpiEvidence Home,
        AssistantGuestsEvidence Guests
    )
    {
        public static AssistantRetrievedEvidence Empty { get; } =
            new(
                AssistantFeedbackEvidence.Empty,
                AssistantOffersEvidence.Empty,
                AssistantCampaignsEvidence.Empty,
                AssistantCaptureEvidence.Empty,
                AssistantHomeKpiEvidence.Empty,
                AssistantGuestsEvidence.Empty
            );

        public static AssistantRetrievedEvidence FromFeedback(
            AssistantFeedbackEvidence feedback
        )
            => new(
                feedback,
                AssistantOffersEvidence.Empty,
                AssistantCampaignsEvidence.Empty,
                AssistantCaptureEvidence.Empty,
                AssistantHomeKpiEvidence.Empty,
                AssistantGuestsEvidence.Empty
            );

        // Location Guest rows are current-state. They do not fill empty
        // windowed evidence. List-guest asks still read Guests.
        public bool IsEmpty =>
            Feedback.IsEmpty
            && Offers.IsEmpty
            && Campaigns.IsEmpty
            && !Capture.HasSnapshotFacts
            && Home.IsEmpty;
    }
}
