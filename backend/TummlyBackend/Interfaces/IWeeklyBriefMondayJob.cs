using TummlyBackend.Helpers;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Monday hourly job seam: select Owned locations that need a closed-week
    /// brief, call generate, notify on first-write success. Ticket 05.
    /// Ticket 08 one-shot ops command reuses this same batch.
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
    /// Produces <c>weekly-brief-ready</c> after a successful first-write generate
    /// (Monday job, lazy Home, or one-time backfill). Dedupe key is location + week.
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
