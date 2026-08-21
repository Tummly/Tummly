using TummlyBackend.Helpers;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Monday hourly job seam: select Owned locations that need a closed-week
    /// brief, call generate, notify on first-write success. Ticket 05.
    /// </summary>
    public interface IWeeklyBriefMondayJob
    {
        Task<WeeklyBriefMondayBatchResult> ProcessAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class WeeklyBriefMondayBatchResult
    {
        public int Generated { get; init; }

        public int Skipped { get; init; }

        public int Failed { get; init; }
    }

    /// <summary>
    /// Stub until ticket 07 wires <c>weekly-brief-ready</c> ProduceAsync.
    /// Called only after a successful first-write generate.
    /// </summary>
    public interface IWeeklyBriefReadyNotifier
    {
        Task NotifyGeneratedAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken = default
        );
    }
}
