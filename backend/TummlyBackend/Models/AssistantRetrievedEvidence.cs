namespace TummlyBackend.Models
{
    public sealed record AssistantRetrievedEvidence(
        AssistantFeedbackEvidence Feedback,
        AssistantOffersEvidence Offers,
        AssistantCampaignsEvidence Campaigns,
        AssistantCaptureEvidence Capture,
        AssistantHomeKpiEvidence Home
    )
    {
        public static AssistantRetrievedEvidence Empty { get; } =
            new(
                AssistantFeedbackEvidence.Empty,
                AssistantOffersEvidence.Empty,
                AssistantCampaignsEvidence.Empty,
                AssistantCaptureEvidence.Empty,
                AssistantHomeKpiEvidence.Empty
            );

        public static AssistantRetrievedEvidence FromFeedback(
            AssistantFeedbackEvidence feedback
        )
            => new(
                feedback,
                AssistantOffersEvidence.Empty,
                AssistantCampaignsEvidence.Empty,
                AssistantCaptureEvidence.Empty,
                AssistantHomeKpiEvidence.Empty
            );

        public bool IsEmpty =>
            Feedback.IsEmpty
            && Offers.IsEmpty
            && Campaigns.IsEmpty
            && !Capture.HasSnapshotFacts
            && Home.IsEmpty;
    }
}
