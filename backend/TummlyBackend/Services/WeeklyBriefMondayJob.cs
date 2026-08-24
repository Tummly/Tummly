using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Hourly job: for each Owned location on its Week-starts-on day, if the
    /// closed prior week in the location timezone has no succeeded brief, call
    /// generate. Notify on first-write success via <see cref="IWeeklyBriefReadyNotifier"/>.
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
            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Select(location => new
                {
                    location.Id,
                    WeekStartsOn = location.Restaurant != null
                        ? location.Restaurant.WeekStartsOn
                        : null,
                })
                .ToListAsync(cancellationToken);

            var generated = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var location in locations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (
                        !WeeklyBriefWeekKey.IsGenerateDay(
                            WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                            utcNow,
                            location.WeekStartsOn
                        )
                    )
                    {
                        skipped++;
                        continue;
                    }

                    var closedWeek = WeeklyBriefWeekKey.ForClosedPriorWeek(
                        WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                        utcNow,
                        location.WeekStartsOn
                    );

                    var alreadyReady = await _context.WeeklyBriefs
                        .AsNoTracking()
                        .AnyAsync(
                            row =>
                                row.LocationId == location.Id
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
                        location.Id,
                        closedWeek,
                        cancellationToken
                    );

                    if (result is WeeklyBriefGenerateResult.Succeeded succeeded)
                    {
                        if (succeeded.Created)
                        {
                            await _notifier.NotifyGeneratedAsync(
                                location.Id,
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
                            location.Id,
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
                        location.Id
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
