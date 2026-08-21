using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pluggable Weekly brief Structured Outputs provider.
    /// Production: Azure OpenAI. Tests/CI: Fake.
    /// </summary>
    public interface IWeeklyBriefProvider
    {
        Task<WeeklyBriefProviderResult> GenerateAsync(
            WeeklyBriefProviderInput input,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>
    /// Internal generate seam for Monday job, lazy Home, and one-time backfill.
    /// Idempotent: existing row for location + week key is returned without overwrite.
    /// Free call — no AI credit debit.
    /// </summary>
    public interface IWeeklyBriefGenerateService
    {
        Task<WeeklyBriefGenerateResult> GenerateAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken = default
        );
    }
}
