using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Thin host adapter — lifetime only. Work lives in <see cref="IFeedbackClassificationWork"/>.
    /// </summary>
    public sealed class FeedbackClassificationBackgroundService
        : BackgroundService
    {
        private readonly IFeedbackClassificationWork _work;

        public FeedbackClassificationBackgroundService(
            IFeedbackClassificationWork work
        )
        {
            _work = work;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _work.RunAsync(stoppingToken);
    }
}
