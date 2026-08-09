using Microsoft.Extensions.Logging;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Emits minimal Campaign product events as structured logs until a GA /
    /// server analytics sink is wired.
    /// </summary>
    public sealed class LoggingCampaignProductAnalytics : ICampaignProductAnalytics
    {
        private readonly ILogger<LoggingCampaignProductAnalytics> _logger;

        public LoggingCampaignProductAnalytics(
            ILogger<LoggingCampaignProductAnalytics> logger
        )
        {
            _logger = logger;
        }

        public void TrackScheduleCommit(int campaignId, string scheduleMode)
        {
            _logger.LogInformation(
                "Campaign analytics {Event} campaignId={CampaignId} mode={Mode}",
                CampaignProductAnalyticsEvents.ScheduleCommit,
                campaignId,
                scheduleMode
            );
        }

        public void TrackSendStart(int campaignId)
        {
            _logger.LogInformation(
                "Campaign analytics {Event} campaignId={CampaignId}",
                CampaignProductAnalyticsEvents.SendStart,
                campaignId
            );
        }

        public void TrackSendTerminal(int campaignId, string terminalStatus)
        {
            _logger.LogInformation(
                "Campaign analytics {Event} campaignId={CampaignId} status={Status}",
                CampaignProductAnalyticsEvents.SendTerminal,
                campaignId,
                terminalStatus
            );
        }

        public void TrackSendTest(int locationId)
        {
            _logger.LogInformation(
                "Campaign analytics {Event} locationId={LocationId}",
                CampaignProductAnalyticsEvents.SendTest,
                locationId
            );
        }
    }
}
