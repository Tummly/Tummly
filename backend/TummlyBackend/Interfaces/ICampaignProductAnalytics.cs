namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Minimal Campaign product-analytics seam (ticket 32). Full draft §26
    /// catalogue stays deferred. Implementations must not receive PII.
    /// </summary>
    public interface ICampaignProductAnalytics
    {
        /// <summary>Successful schedule / send-now commit.</summary>
        void TrackScheduleCommit(int campaignId, string scheduleMode);

        /// <summary>Fire path passed cannot-start gates and began send work.</summary>
        void TrackSendStart(int campaignId);

        /// <summary>
        /// Terminal send status: <c>sent</c>, <c>partially-sent</c>, or <c>failed</c>.
        /// </summary>
        void TrackSendTerminal(int campaignId, string terminalStatus);

        /// <summary>
        /// Successful Campaign send test (transactional; no credit burn).
        /// </summary>
        void TrackSendTest(int locationId);
    }

    /// <summary>Stable event names for logging / future GA mapping.</summary>
    public static class CampaignProductAnalyticsEvents
    {
        public const string ScheduleCommit = "campaign_schedule_commit";
        public const string SendStart = "campaign_send_start";
        public const string SendTerminal = "campaign_send_terminal";
        public const string SendTest = "campaign_send_test";
    }
}
