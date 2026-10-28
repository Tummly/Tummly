using TummlyBackend.Models;

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

        /// <summary>
        /// Why the audience cannot be counted when Evaluable is false and the audience
        /// is blocked by Campaign data (for example offer-not-redeemed without an Offer).
        /// Null for unevaluable audiences and for evaluable audiences.
        /// </summary>
        public string? UnavailableReason { get; init; }

        /// <summary>
        /// Recent redeemers look-back in days (for example 30 for "Last 30 days").
        /// Null for audiences that do not use a period.
        /// </summary>
        public int? LookbackDays { get; init; }

        /// <summary>IANA timezone that anchors the look-back. Null when no period applies.</summary>
        public string? LookbackTimeZone { get; init; }
    }

    /// <summary>
    /// Campaign-owned inputs for audience filters that need more than the audience key.
    /// Null fields mean the Campaign has no value: offer-not-redeemed is unavailable
    /// without an Offer, and recent-redeemers falls back to the default period.
    /// </summary>
    public sealed record CampaignAudienceScope(
        int? OfferId = null,
        int? RecentRedeemerLookbackDays = null,
        string? RecentRedeemerTimeZone = null
    )
    {
        public static CampaignAudienceScope None { get; } = new();

        public static CampaignAudienceScope ForCampaign(Campaign campaign) =>
            new(campaign.OfferId, campaign.AudienceLookbackDays, campaign.AudienceLookbackTimeZone);
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
