using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Hourly host adapter for Custom plan cycle renewal + dunning advance.
    /// Pattern: <see cref="IncludedPeriodBackgroundService"/>.
    /// </summary>
    public sealed class CustomPlanCycleRenewalBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<CustomPlanCycleRenewalBackgroundService> _logger;

        public CustomPlanCycleRenewalBackgroundService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<CustomPlanCycleRenewalBackgroundService> logger
        )
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                    _logger.LogError(ex, "Custom plan cycle renewal job failed");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var job =
                scope.ServiceProvider.GetRequiredService<ICustomPlanCycleRenewalJob>();

            var batch = await job.ProcessAsync(DateTime.UtcNow, stoppingToken);

            if (batch.OrdersOpened > 0 || batch.Failed > 0)
            {
                _logger.LogInformation(
                    "Custom plan cycle renewal finished: processed={Processed} opened={Opened} failed={Failed}",
                    batch.Processed,
                    batch.OrdersOpened,
                    batch.Failed
                );
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
