using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Hourly host adapter for the Weekly brief Monday generate job.
    /// Pattern: <see cref="ActivationNotificationBackgroundService"/>. No Hangfire.
    /// </summary>
    public sealed class WeeklyBriefMondayBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<WeeklyBriefMondayBackgroundService> _logger;

        public WeeklyBriefMondayBackgroundService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<WeeklyBriefMondayBackgroundService> logger
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
                    _logger.LogError(
                        ex,
                        "Weekly brief Monday job failed"
                    );
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var job =
                scope.ServiceProvider.GetRequiredService<IWeeklyBriefMondayJob>();

            var batch = await job.ProcessAsync(
                DateTime.UtcNow,
                stoppingToken
            );

            if (batch.Generated > 0 || batch.Failed > 0)
            {
                _logger.LogInformation(
                    "Weekly brief Monday job finished: generated={Generated} skipped={Skipped} failed={Failed}",
                    batch.Generated,
                    batch.Skipped,
                    batch.Failed
                );
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
