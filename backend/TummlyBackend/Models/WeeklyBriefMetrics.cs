namespace TummlyBackend.Models
{
    /// <summary>
    /// Aggregate metrics bag fed into Weekly brief generation (ticket 02).
    /// Counts and theme rollups only — no guest PII, no feedback comment bodies.
    /// Ticket 03 loads these for the closed week coverage window.
    /// </summary>
    /// <remarks>
    /// Field list:
    /// <list type="bullet">
    /// <item><description><see cref="GuestsJoined"/> — capture: guests joined in the closed week.</description></item>
    /// <item><description><see cref="CaptureEvents"/> — capture: capture / QR scan events in the closed week.</description></item>
    /// <item><description><see cref="FeedbackCount"/> — feedback: total submissions in the closed week.</description></item>
    /// <item><description><see cref="PositiveFeedbackCount"/> — feedback: positive sentiment count.</description></item>
    /// <item><description><see cref="NeutralFeedbackCount"/> — feedback: neutral sentiment count.</description></item>
    /// <item><description><see cref="NegativeFeedbackCount"/> — feedback: negative sentiment count.</description></item>
    /// <item><description><see cref="NeedsAttentionCount"/> — feedback: Needs attention count in the closed week.</description></item>
    /// <item><description><see cref="FeedbackThemeCounts"/> — feedback: Detected Tag theme rollups (label → count); never raw comment text.</description></item>
    /// <item><description><see cref="ActiveOffers"/> — offers: Active catalog offers at the location (echo / presence).</description></item>
    /// <item><description><see cref="ClaimsInWeek"/> — offers: claims in the closed week.</description></item>
    /// <item><description><see cref="RedemptionsInWeek"/> — offers: redemptions in the closed week.</description></item>
    /// <item><description><see cref="CampaignsSentInWeek"/> — campaigns: campaigns sent in the closed week.</description></item>
    /// <item><description><see cref="CampaignRecipientsReached"/> — campaigns: recipients reached in the closed week.</description></item>
    /// </list>
    /// </remarks>
    public sealed record WeeklyBriefMetrics(
        int GuestsJoined,
        int CaptureEvents,
        int FeedbackCount,
        int PositiveFeedbackCount,
        int NeutralFeedbackCount,
        int NegativeFeedbackCount,
        int NeedsAttentionCount,
        IReadOnlyDictionary<string, int> FeedbackThemeCounts,
        int ActiveOffers,
        int ClaimsInWeek,
        int RedemptionsInWeek,
        int CampaignsSentInWeek,
        int CampaignRecipientsReached
    )
    {
        public static WeeklyBriefMetrics Empty { get; } =
            new(
                GuestsJoined: 0,
                CaptureEvents: 0,
                FeedbackCount: 0,
                PositiveFeedbackCount: 0,
                NeutralFeedbackCount: 0,
                NegativeFeedbackCount: 0,
                NeedsAttentionCount: 0,
                FeedbackThemeCounts: new Dictionary<string, int>(),
                ActiveOffers: 0,
                ClaimsInWeek: 0,
                RedemptionsInWeek: 0,
                CampaignsSentInWeek: 0,
                CampaignRecipientsReached: 0
            );
    }
}
