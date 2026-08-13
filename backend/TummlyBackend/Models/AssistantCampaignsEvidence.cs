namespace TummlyBackend.Models
{
    public sealed record AssistantCampaignsEvidence(
        int ListTotalCount,
        int ListSampleCount,
        int InFlightScheduled,
        int InFlightSending,
        int MessagesSentAccepted,
        IReadOnlyList<AssistantCampaignListRow> Rows,
        IReadOnlyList<AssistantCampaignEligibilityRow> Eligibility,
        IReadOnlyList<AssistantCampaignDetailRow> Details
    )
    {
        public static AssistantCampaignsEvidence Empty { get; } =
            new(0, 0, 0, 0, 0, [], [], []);

        public bool IsEmpty =>
            ListTotalCount == 0
            && InFlightScheduled == 0
            && InFlightSending == 0
            && MessagesSentAccepted == 0;

        public bool HasCampaignFacts => !IsEmpty;

        public bool DisclosesSample => ListTotalCount > ListSampleCount;
    }

    public sealed record AssistantCampaignListRow(
        int Id,
        string Name,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int? OfferId
    );

    public sealed record AssistantCampaignEligibilityRow(
        int CampaignId,
        string AudienceKey,
        bool Evaluable,
        int? Matched,
        int? CurrentlyEligible,
        int? Excluded
    );

    public sealed record AssistantCampaignDetailRow(
        int Id,
        string Name,
        string Status,
        string? MessageSubject,
        string? MessageBody,
        string? AudienceKey,
        string? Channel
    );
}
