using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Thin host adapter — lifetime only. Work lives in
    /// <see cref="IGuestResponseEmailDeliveryWork"/>.
    /// </summary>
    public sealed class GuestResponseEmailDeliveryBackgroundService
        : BackgroundService
    {
        private readonly IGuestResponseEmailDeliveryWork _work;

        public GuestResponseEmailDeliveryBackgroundService(
            IGuestResponseEmailDeliveryWork work
        )
        {
            _work = work;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _work.RunAsync(stoppingToken);
    }
}
