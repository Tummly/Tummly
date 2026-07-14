using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class ActivationNotificationBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ActivationNotificationBackgroundService> _logger;

        public ActivationNotificationBackgroundService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<ActivationNotificationBackgroundService> logger
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
                        "Activation notification job failed"
                    );
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var producer =
                scope.ServiceProvider.GetRequiredService<IActivationNotificationProducer>();

            var batch = await producer.ProcessAsync(
                DateTime.UtcNow,
                stoppingToken
            );

            if (batch.Produced > 0 || batch.Failed > 0)
            {
                _logger.LogInformation(
                    "Activation notification job finished: produced={Produced} failed={Failed}",
                    batch.Produced,
                    batch.Failed
                );
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
