using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Daily host adapter for guest-retention purge after restaurant retention eligibility.
    /// Pattern: <see cref="IncludedPeriodBackgroundService"/>.
    /// </summary>
    public sealed class GuestRetentionPurgeBackgroundService : BackgroundService
    {
        public static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<GuestRetentionPurgeBackgroundService> _logger;

        public GuestRetentionPurgeBackgroundService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<GuestRetentionPurgeBackgroundService> logger
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
                    _logger.LogError(ex, "Guest-retention purge job failed");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var purge =
                scope.ServiceProvider.GetRequiredService<IGuestRetentionPurgeService>();

            var batch = await purge.ProcessOnceAsync(
                DateTime.UtcNow,
                stoppingToken
            );

            if (batch.Purged > 0 || batch.Failed > 0)
            {
                _logger.LogInformation(
                    "Guest-retention purge finished: eligible={EligibleFound} purged={Purged} failed={Failed} guestsDeleted={GuestsDeleted}",
                    batch.EligibleFound,
                    batch.Purged,
                    batch.Failed,
                    batch.GuestsDeleted
                );
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
