using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>No-op analytics sink for tests that do not assert events.</summary>
    public sealed class NoOpCampaignProductAnalytics : ICampaignProductAnalytics
    {
        public static NoOpCampaignProductAnalytics Instance { get; } = new();

        public void TrackScheduleCommit(int campaignId, string scheduleMode)
        {
        }

        public void TrackSendStart(int campaignId)
        {
        }

        public void TrackSendTerminal(int campaignId, string terminalStatus)
        {
        }

        public void TrackSendTest(int locationId)
        {
        }
    }
}
