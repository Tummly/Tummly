using Microsoft.Extensions.Logging;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// One-shot ops command: generate the current closed-week Weekly brief
    /// for every Owned location that lacks a succeeded row. Reuses the
    /// Monday job seam (generate + <c>weekly-brief-ready</c> notify).
    /// Ticket 08.
    /// </summary>
    public static class WeeklyBriefOneTimeGenerateCommand
    {
        public const string ArgumentName = "--generate-weekly-briefs";

        public static bool IsRequested(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (string.Equals(arg, ArgumentName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<int> ExecuteAsync(
            IWeeklyBriefMondayJob job,
            DateTime utcNow,
            ILogger logger,
            CancellationToken cancellationToken = default
        )
        {
            var batch = await job.ProcessAsync(utcNow, cancellationToken);

            logger.LogInformation(
                "Weekly brief one-time generate finished: generated={Generated} skipped={Skipped} failed={Failed}",
                batch.Generated,
                batch.Skipped,
                batch.Failed
            );

            return batch.Failed > 0 ? 1 : 0;
        }

        public static async Task<int> ExecuteAsync(
            IServiceProvider services,
            DateTime utcNow,
            ILogger logger,
            CancellationToken cancellationToken = default
        )
        {
            using var scope = services.CreateScope();
            var job = scope.ServiceProvider
                .GetRequiredService<IWeeklyBriefMondayJob>();

            return await ExecuteAsync(
                job,
                utcNow,
                logger,
                cancellationToken
            );
        }
    }
}
