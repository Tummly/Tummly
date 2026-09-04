namespace TummlyBackend.Models
{
    /// <summary>
    /// Aggregate metrics bag fed into Weekly brief generation (ticket 02).
    /// Counts and Detected Tag rollups only — no guest PII, no feedback comment bodies.
    /// Ticket 03 loads these for the closed week coverage window.
    /// </summary>
    /// <remarks>
    /// Field list:
    /// <list type="bullet">
    /// <item><description><see cref="GuestsJoined"/> — capture: guests joined in the closed week.</description></item>
    /// <item><description><see cref="QrScanEvents"/> — capture: QR scan events in the closed week.</description></item>
    /// <item><description><see cref="FeedbackCount"/> — feedback: total submissions in the closed week.</description></item>
    /// <item><description><see cref="PositiveFeedbackCount"/> — feedback: positive sentiment count.</description></item>
    /// <item><description><see cref="NeutralFeedbackCount"/> — feedback: neutral sentiment count.</description></item>
    /// <item><description><see cref="NegativeFeedbackCount"/> — feedback: negative sentiment count.</description></item>
    /// <item><description><see cref="NeedsAttentionCount"/> — feedback: Needs attention count in the closed week.</description></item>
    /// <item><description><see cref="DetectedTagCounts"/> — feedback: Detected Tag rollups (label → count); never raw comment text.</description></item>
    /// <item><description><see cref="ActiveOffers"/> — offers: Active catalog offers at the location (echo / presence).</description></item>
    /// <item><description><see cref="ClaimsInWeek"/> — offers: claims in the closed week.</description></item>
    /// <item><description><see cref="RedemptionsInWeek"/> — offers: redemptions in the closed week.</description></item>
    /// <item><description><see cref="CampaignsSentInWeek"/> — campaigns: campaigns sent in the closed week.</description></item>
    /// <item><description><see cref="CampaignRecipientsReached"/> — campaigns: recipients reached in the closed week.</description></item>
    /// <item><description><see cref="UnsubscribesInWeek"/> — campaigns/privacy: Guest marketing unsubscribed activity in the closed week.</description></item>
    /// </list>
    /// </remarks>
    public sealed record WeeklyBriefMetrics(
        int GuestsJoined,
        int QrScanEvents,
        int FeedbackCount,
        int PositiveFeedbackCount,
        int NeutralFeedbackCount,
        int NegativeFeedbackCount,
        int NeedsAttentionCount,
        IReadOnlyDictionary<string, int> DetectedTagCounts,
        int ActiveOffers,
        int ClaimsInWeek,
        int RedemptionsInWeek,
        int CampaignsSentInWeek,
        int CampaignRecipientsReached,
        int UnsubscribesInWeek
    );

    /// <summary>
    /// Input for Weekly brief Structured Outputs (Azure or Fake). Ticket 03 wires the caller.
    /// Aggregate metrics only — no guest PII or feedback comment bodies.
    /// </summary>
    public sealed record WeeklyBriefProviderInput(
        string LocationName,
        string WeekKey,
        DateTime CoverageStartUtc,
        DateTime CoverageEndUtcExclusive,
        WeeklyBriefMetrics Metrics
    );

    /// <summary>
    /// Result of Weekly brief provider (Azure or Fake).
    /// </summary>
    public abstract record WeeklyBriefProviderResult
    {
        private WeeklyBriefProviderResult()
        {
        }

        public sealed record Succeeded(
            WeeklyBriefBody Body,
            WeeklyBriefEnrichment? Enrichment
        ) : WeeklyBriefProviderResult;

        public sealed record Failed(bool Retryable = true)
            : WeeklyBriefProviderResult;
    }

    /// <summary>
    /// Result of <c>IWeeklyBriefGenerateService.GenerateAsync</c>.
    /// </summary>
    public abstract record WeeklyBriefGenerateResult
    {
        private WeeklyBriefGenerateResult()
        {
        }

        /// <param name="Created">
        /// True when this call persisted a new row; false when an existing row was returned.
        /// </param>
        public sealed record Succeeded(WeeklyBrief Brief, bool Created)
            : WeeklyBriefGenerateResult;

        public sealed record Failed(string Message, bool Retryable = true)
            : WeeklyBriefGenerateResult;
    }
}
