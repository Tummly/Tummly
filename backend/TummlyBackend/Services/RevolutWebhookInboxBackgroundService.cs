using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class RevolutWebhookInboxBackgroundService : BackgroundService
    {
        private readonly IRevolutWebhookInboxWork _work;

        public RevolutWebhookInboxBackgroundService(
            IRevolutWebhookInboxWork work
        )
        {
            _work = work;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _work.RunAsync(stoppingToken);
    }
}
