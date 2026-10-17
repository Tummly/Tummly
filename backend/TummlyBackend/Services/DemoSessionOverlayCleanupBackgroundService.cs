namespace TummlyBackend.Services
{
    /// <summary>
    /// Purges expired public-demo session overlay rows (§15 TTL).
    /// </summary>
    public sealed class DemoSessionOverlayCleanupBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DemoSessionOverlayCleanupBackgroundService> _logger;

        public DemoSessionOverlayCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<DemoSessionOverlayCleanupBackgroundService> logger
        )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var overlay = scope.ServiceProvider
                        .GetRequiredService<DemoSessionOverlayService>();
                    var removed = await overlay.PurgeExpiredAsync(stoppingToken);
                    if (removed > 0)
                    {
                        _logger.LogInformation(
                            "Purged {Count} expired demo session overlay rows",
                            removed
                        );
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Demo session overlay purge failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
