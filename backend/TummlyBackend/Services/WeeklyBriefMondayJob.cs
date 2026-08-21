using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Hourly Monday job: for each Owned location, if the closed prior week
    /// in the location timezone has no succeeded brief, call generate.
    /// Notify on first-write success (stub until ticket 07).
    /// Per-location failures are logged; the batch continues.
    /// </summary>
    public sealed class WeeklyBriefMondayJob : IWeeklyBriefMondayJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeeklyBriefGenerateService _generate;
        private readonly IWeeklyBriefReadyNotifier _notifier;
        private readonly ILogger<WeeklyBriefMondayJob> _logger;

        public WeeklyBriefMondayJob(
            ApplicationDbContext context,
            IWeeklyBriefGenerateService generate,
            IWeeklyBriefReadyNotifier notifier,
            ILogger<WeeklyBriefMondayJob> logger
        )
        {
            _context = context;
            _generate = generate;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<WeeklyBriefMondayBatchResult> ProcessAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default
        )
        {
            var locationIds = await _context.RestaurantLocations
                .AsNoTracking()
                .Select(location => location.Id)
                .ToListAsync(cancellationToken);

            var closedWeek = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow
            );

            var generated = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var locationId in locationIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var alreadyReady = await _context.WeeklyBriefs
                        .AsNoTracking()
                        .AnyAsync(
                            row =>
                                row.LocationId == locationId
                                && row.WeekKey == closedWeek.WeekKey
                                && row.Status == WeeklyBriefStatus.Succeeded,
                            cancellationToken
                        );

                    if (alreadyReady)
                    {
                        skipped++;
                        continue;
                    }

                    var result = await _generate.GenerateAsync(
                        locationId,
                        closedWeek,
                        cancellationToken
                    );

                    if (result is WeeklyBriefGenerateResult.Succeeded succeeded)
                    {
                        if (succeeded.Created)
                        {
                            await _notifier.NotifyGeneratedAsync(
                                locationId,
                                closedWeek,
                                cancellationToken
                            );
                            generated++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                    else
                    {
                        failed++;
                        _logger.LogWarning(
                            "Weekly brief generate failed for location {LocationId} week {WeekKey}",
                            locationId,
                            closedWeek.WeekKey
                        );
                    }
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested
                )
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(
                        ex,
                        "Weekly brief Monday job failed for location {LocationId}",
                        locationId
                    );
                }
            }

            return new WeeklyBriefMondayBatchResult
            {
                Generated = generated,
                Skipped = skipped,
                Failed = failed,
            };
        }
    }
}
