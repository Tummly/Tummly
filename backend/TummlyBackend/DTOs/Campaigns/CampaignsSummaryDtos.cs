namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>
    /// Overview Campaign summary sibling facts (ticket 29).
    /// </summary>
    public sealed class CampaignsSummaryDto
    {
        /// <summary>Scheduled campaigns at the owned location (no date window).</summary>
        public required int CampaignsInFlightScheduled { get; init; }

        /// <summary>Sending campaigns at the owned location (no date window).</summary>
        public required int CampaignsInFlightSending { get; init; }

        /// <summary>
        /// Submitted/accepted outbound messages in the overview window
        /// (Email channel first).
        /// </summary>
        public required int MessagesSentAccepted { get; init; }

        /// <summary>Email-channel accepted count (same as MessagesSentAccepted for MVP).</summary>
        public required int MessagesSentAcceptedEmail { get; init; }
    }

    /// <summary>
    /// Query for overview summary — date window applies to messages only.
    /// </summary>
    public sealed class CampaignsSummaryQuery
    {
        public required int LocationId { get; init; }

        /// <summary>UTC inclusive start; null = all-time messages.</summary>
        public DateTime? OverviewDateFrom { get; init; }

        /// <summary>UTC exclusive end; null = all-time messages.</summary>
        public DateTime? OverviewDateTo { get; init; }
    }
}
