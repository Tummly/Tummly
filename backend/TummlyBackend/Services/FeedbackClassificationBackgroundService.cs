using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class FeedbackClassificationBackgroundService
        : BackgroundService
    {
        private readonly FeedbackClassificationQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<FeedbackClassificationBackgroundService> _logger;

        public FeedbackClassificationBackgroundService(
            FeedbackClassificationQueue queue,
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<FeedbackClassificationBackgroundService> logger
        )
        {
            _queue = queue;
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

            await foreach (
                var feedbackId in _queue.Reader.ReadAllAsync(stoppingToken)
            )
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider
                        .GetRequiredService<IFeedbackClassificationProcessor>();

                    await processor.ProcessAsync(feedbackId, stoppingToken);
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
                        "Failed to classify Feedback {FeedbackId}",
                        feedbackId
                    );
                }
            }
        }
    }
}
