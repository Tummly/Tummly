using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Ticket 05 stub — ticket 07 wires <c>weekly-brief-ready</c> ProduceAsync.
    /// </summary>
    public sealed class NoOpWeeklyBriefReadyNotifier : IWeeklyBriefReadyNotifier
    {
        public Task NotifyGeneratedAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
