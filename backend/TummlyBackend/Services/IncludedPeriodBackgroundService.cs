using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Hourly host adapter for the included-period job (Annual slices 2–12 and expiry catch-up).
    /// Pattern: <see cref="ActivationNotificationBackgroundService"/>.
    /// </summary>
    public sealed class IncludedPeriodBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<IncludedPeriodBackgroundService> _logger;

        public IncludedPeriodBackgroundService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<IncludedPeriodBackgroundService> logger
        )
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken
        )
        {
            if (_environment.IsEnvironment("Testing"))
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (
                    stoppingToken.IsCancellationRequested
                )
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Included-period job failed");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var job =
                scope.ServiceProvider.GetRequiredService<IIncludedPeriodJob>();

            var batch = await job.ProcessAsync(
                DateTime.UtcNow,
                stoppingToken
            );

            if (batch.Minted > 0 || batch.Failed > 0)
            {
                _logger.LogInformation(
                    "Included-period job finished: processed={Processed} minted={Minted} failed={Failed}",
                    batch.Processed,
                    batch.Minted,
                    batch.Failed
                );
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
