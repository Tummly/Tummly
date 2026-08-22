using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

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

        /// <summary>
        /// When true, the API runs <see cref="RunOnceAfterDeployAsync"/> after
        /// database init succeeds. QA deploy sets this so the next revision
        /// generates missing closed-week briefs once.
        /// </summary>
        public const string OneTimeGenerateOnStartupConfigKey =
            "WeeklyBrief:OneTimeGenerateOnStartup";

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

        /// <summary>
        /// Idempotent post-deploy backfill. Skips when the watermark row
        /// exists. Writes the watermark only after a batch with zero
        /// location failures so a partial run retries on the next start.
        /// </summary>
        public static async Task<bool> RunOnceAfterDeployAsync(
            ApplicationDbContext context,
            IWeeklyBriefMondayJob job,
            ILogger logger,
            DateTime utcNow,
            CancellationToken cancellationToken = default
        )
        {
            var alreadyComplete = await context.DataMigrationMarkers
                .AsNoTracking()
                .AnyAsync(
                    marker =>
                        marker.Id
                        == DataMigrationMarkerIds.WeeklyBriefClosedWeekBackfill,
                    cancellationToken
                );

            if (alreadyComplete)
            {
                logger.LogInformation(
                    "Weekly brief one-time generate already complete; skipping"
                );
                return true;
            }

            var exitCode = await ExecuteAsync(
                job,
                utcNow,
                logger,
                cancellationToken
            );

            if (exitCode != 0)
            {
                logger.LogWarning(
                    "Weekly brief one-time generate had location failures; watermark not written"
                );
                return false;
            }

            context.DataMigrationMarkers.Add(
                new DataMigrationMarker
                {
                    Id = DataMigrationMarkerIds.WeeklyBriefClosedWeekBackfill,
                    CompletedAt = utcNow,
                }
            );
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
