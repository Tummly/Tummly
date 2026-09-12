using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class PrintReadyQrMaterialsBackgroundService
        : BackgroundService
    {
        private readonly IPrintReadyQrMaterialsWork _work;

        public PrintReadyQrMaterialsBackgroundService(
            IPrintReadyQrMaterialsWork work
        )
        {
            _work = work;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _work.RunAsync(stoppingToken);
    }
}
