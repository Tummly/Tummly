using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// No-op dunning Pay (unit tests and hosts without Revolut).
    /// </summary>
    public sealed class NoOpRevolutDunningPayAdapter : IRevolutDunningPayAdapter
    {
        public static NoOpRevolutDunningPayAdapter Instance { get; } = new();

        public Task HandleDayStepAsync(
            int restaurantId,
            int dayStep,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task TryPayOutstandingAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
