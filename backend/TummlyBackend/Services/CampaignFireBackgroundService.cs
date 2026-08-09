using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Thin host adapter — lifetime only. Work lives in <see cref="ICampaignFireWork"/>.
    /// </summary>
    public sealed class CampaignFireBackgroundService : BackgroundService
    {
        private readonly ICampaignFireWork _work;

        public CampaignFireBackgroundService(ICampaignFireWork work)
        {
            _work = work;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _work.RunAsync(stoppingToken);
    }
}
