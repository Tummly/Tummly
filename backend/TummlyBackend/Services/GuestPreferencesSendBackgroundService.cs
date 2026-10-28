namespace TummlyBackend.Services
{
    /// <summary>
    /// Thin host adapter. The work lives in <see cref="GuestPreferencesSendWorker"/>.
    /// </summary>
    public sealed class GuestPreferencesSendBackgroundService : BackgroundService
    {
        private readonly GuestPreferencesSendWorker _worker;

        public GuestPreferencesSendBackgroundService(GuestPreferencesSendWorker worker)
        {
            _worker = worker;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            _worker.RunAsync(stoppingToken);
    }
}
