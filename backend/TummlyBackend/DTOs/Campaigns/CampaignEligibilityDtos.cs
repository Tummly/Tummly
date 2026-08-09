namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>
    /// Campaign eligibility estimate for one audience + location (Audience step).
    /// </summary>
    public sealed class CampaignEligibilityDto
    {
        public required string AudienceKey { get; init; }

        /// <summary>
        /// False when MVP cannot evaluate membership honestly (unevaluable audiences).
        /// Counts are null when false.
        /// </summary>
        public required bool Evaluable { get; init; }

        public int? Matched { get; init; }

        public int? CurrentlyEligible { get; init; }

        public int? Excluded { get; init; }

        public int? EmailEligible { get; init; }

        public int? SmsEligible { get; init; }

        /// <summary>
        /// Aggregated primary-reason rollup for Excluded matched guests.
        /// Empty when not evaluable or Excluded is 0.
        /// </summary>
        public required IReadOnlyList<CampaignExcludedReasonCountDto> ExcludedReasons
        {
            get;
            init;
        }

        /// <summary>
        /// Stage-1 check-set id — extend without renaming Currently eligible.
        /// </summary>
        public required string CheckSetVersion { get; init; }

        public required DateTime EvaluatedAt { get; init; }
    }

    public sealed class CampaignExcludedReasonCountDto
    {
        /// <summary>
        /// Primary reason code: account, soft-lock, opt-out, suppression,
        /// invalid-contact, channel.
        /// </summary>
        public required string Reason { get; init; }

        public required int Count { get; init; }
    }
}
