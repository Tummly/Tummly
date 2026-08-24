using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam under test: <see cref="WeeklyBriefOneTimeGenerateCommand"/>
    /// (ticket 08). One-shot ops command for the closed week across all
    /// Owned locations. Reuses <see cref="IWeeklyBriefMondayJob"/> so
    /// generate and <c>weekly-brief-ready</c> stay on the same path as
    /// the Monday job.
    /// </summary>
    public class WeeklyBriefOneTimeGenerateCommandTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeWeeklyBriefProvider _provider;
        private readonly WeeklyBriefGenerateService _generate;
        private readonly RecordingWeeklyBriefReadyNotifier _notifier;
        private readonly WeeklyBriefMondayJob _job;

        /// <summary>
        /// London Monday 2026-08-17 00:00 BST — closed week becomes monday:2026-08-10.
        /// </summary>
        private static readonly DateTime LondonMondayMidnightUtc = new(
            2026,
            8,
            16,
            23,
            0,
            0,
            DateTimeKind.Utc
        );

        public WeeklyBriefOneTimeGenerateCommandTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _provider = new FakeWeeklyBriefProvider();
            _provider.UseDefaultFixtures();
            _generate = new WeeklyBriefGenerateService(
                _context,
                _provider,
                NullLogger<WeeklyBriefGenerateService>.Instance
            );
            _notifier = new RecordingWeeklyBriefReadyNotifier();
            _job = new WeeklyBriefMondayJob(
                _context,
                _generate,
                _notifier,
                NullLogger<WeeklyBriefMondayJob>.Instance
            );
        }

        [Fact]
        public async Task ExecuteAsync_GeneratesClosedWeekBrief_WhenLocationHasNoRow()
        {
            var locationId = await SeedLocationAsync("Harbour Kitchen");

            var exitCode = await WeeklyBriefOneTimeGenerateCommand.ExecuteAsync(
                _job,
                LondonMondayMidnightUtc,
                NullLogger.Instance
            );

            Assert.Equal(0, exitCode);
            Assert.Equal(1, _provider.CallCount);
            Assert.Equal(
                1,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId
                    && row.WeekKey == "monday:2026-08-10"
                    && row.Status == WeeklyBriefStatus.Succeeded
                )
            );
            Assert.Single(_notifier.Calls);
            Assert.Equal(locationId, _notifier.Calls[0].LocationId);
            Assert.Equal("monday:2026-08-10", _notifier.Calls[0].WeekKey);
        }

        [Fact]
        public async Task ExecuteAsync_SkipsLocation_WhenClosedWeekRowAlreadyExists()
        {
            var locationId = await SeedLocationAsync("Harbour Kitchen");
            const string sentinelBody = "{\"headline\":\"keep-me\"}";
            await SeedSucceededBriefAsync(
                locationId,
                weekKey: "monday:2026-08-10",
                bodyJson: sentinelBody
            );

            var exitCode = await WeeklyBriefOneTimeGenerateCommand.ExecuteAsync(
                _job,
                LondonMondayMidnightUtc,
                NullLogger.Instance
            );

            Assert.Equal(0, exitCode);
            Assert.Equal(0, _provider.CallCount);
            Assert.Empty(_notifier.Calls);
            var row = await _context.WeeklyBriefs.SingleAsync(brief =>
                brief.LocationId == locationId && brief.WeekKey == "monday:2026-08-10"
            );
            Assert.Equal(sentinelBody, row.BodyJson);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsFailureExitCode_WhenOneLocationFails()
        {
            var failingId = await SeedLocationAsync("Fails");
            var succeedingId = await SeedLocationAsync("Succeeds");
            var mixed = new MixedGenerateService(_generate, failingId);
            var job = new WeeklyBriefMondayJob(
                _context,
                mixed,
                _notifier,
                NullLogger<WeeklyBriefMondayJob>.Instance
            );

            var exitCode = await WeeklyBriefOneTimeGenerateCommand.ExecuteAsync(
                job,
                LondonMondayMidnightUtc,
                NullLogger.Instance
            );

            Assert.Equal(1, exitCode);
            Assert.True(
                await _context.WeeklyBriefs.AnyAsync(row =>
                    row.LocationId == succeedingId && row.WeekKey == "monday:2026-08-10"
                )
            );
            Assert.False(
                await _context.WeeklyBriefs.AnyAsync(row =>
                    row.LocationId == failingId
                )
            );
        }

        [Fact]
        public void IsRequested_IsTrue_OnlyForGenerateFlag()
        {
            Assert.True(
                WeeklyBriefOneTimeGenerateCommand.IsRequested(
                    ["--generate-weekly-briefs"]
                )
            );
            Assert.True(
                WeeklyBriefOneTimeGenerateCommand.IsRequested(
                    ["--urls", "http://localhost", "--generate-weekly-briefs"]
                )
            );
            Assert.False(WeeklyBriefOneTimeGenerateCommand.IsRequested([]));
            Assert.False(
                WeeklyBriefOneTimeGenerateCommand.IsRequested(
                    ["--generate-weekly-brief"]
                )
            );
        }

        [Fact]
        public async Task RunOnceAfterDeployAsync_WhenMarkerSet_SkipsGenerate()
        {
            await SeedLocationAsync("Harbour Kitchen");
            _context.DataMigrationMarkers.Add(
                new DataMigrationMarker
                {
                    Id = DataMigrationMarkerIds.WeeklyBriefClosedWeekBackfill,
                    CompletedAt = LondonMondayMidnightUtc,
                }
            );
            await _context.SaveChangesAsync();

            var completed =
                await WeeklyBriefOneTimeGenerateCommand.RunOnceAfterDeployAsync(
                    _context,
                    _job,
                    NullLogger.Instance,
                    LondonMondayMidnightUtc
                );

            Assert.True(completed);
            Assert.Equal(0, _provider.CallCount);
            Assert.Empty(_notifier.Calls);
        }

        [Fact]
        public async Task RunOnceAfterDeployAsync_WhenNotComplete_GeneratesThenSetsMarker()
        {
            var locationId = await SeedLocationAsync("Harbour Kitchen");

            var completed =
                await WeeklyBriefOneTimeGenerateCommand.RunOnceAfterDeployAsync(
                    _context,
                    _job,
                    NullLogger.Instance,
                    LondonMondayMidnightUtc
                );

            Assert.True(completed);
            Assert.Equal(1, _provider.CallCount);
            Assert.Single(_notifier.Calls);
            Assert.Equal(locationId, _notifier.Calls[0].LocationId);
            Assert.True(
                await _context.DataMigrationMarkers.AnyAsync(row =>
                    row.Id
                    == DataMigrationMarkerIds.WeeklyBriefClosedWeekBackfill
                )
            );
        }

        [Fact]
        public async Task RunOnceAfterDeployAsync_WhenGenerateFails_DoesNotSetMarker()
        {
            var failingId = await SeedLocationAsync("Fails");
            var mixed = new MixedGenerateService(_generate, failingId);
            var job = new WeeklyBriefMondayJob(
                _context,
                mixed,
                _notifier,
                NullLogger<WeeklyBriefMondayJob>.Instance
            );

            var completed =
                await WeeklyBriefOneTimeGenerateCommand.RunOnceAfterDeployAsync(
                    _context,
                    job,
                    NullLogger.Instance,
                    LondonMondayMidnightUtc
                );

            Assert.False(completed);
            Assert.False(
                await _context.DataMigrationMarkers.AnyAsync(row =>
                    row.Id
                    == DataMigrationMarkerIds.WeeklyBriefClosedWeekBackfill
                )
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedLocationAsync(string locationName)
        {
            var restaurant = new Restaurant
            {
                Name = "Weekly Brief One-Time Restaurant",
                AccountType = "Single",
                OwnerUserId = 7,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = "1 Harbour Way",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task SeedSucceededBriefAsync(
            int locationId,
            string weekKey,
            string bodyJson
        )
        {
            _context.WeeklyBriefs.Add(
                new WeeklyBrief
                {
                    LocationId = locationId,
                    WeekKey = weekKey,
                    Status = WeeklyBriefStatus.Succeeded,
                    GeneratedAtUtc = LondonMondayMidnightUtc,
                    BodyJson = bodyJson,
                    MetricsJson = "{}",
                }
            );
            await _context.SaveChangesAsync();
        }

        private sealed class MixedGenerateService : IWeeklyBriefGenerateService
        {
            private readonly IWeeklyBriefGenerateService _inner;
            private readonly int _failLocationId;

            public MixedGenerateService(
                IWeeklyBriefGenerateService inner,
                int failLocationId
            )
            {
                _inner = inner;
                _failLocationId = failLocationId;
            }

            public Task<WeeklyBriefGenerateResult> GenerateAsync(
                int locationId,
                WeeklyBriefClosedWeek closedWeek,
                CancellationToken cancellationToken = default
            )
            {
                if (locationId == _failLocationId)
                {
                    return Task.FromResult<WeeklyBriefGenerateResult>(
                        new WeeklyBriefGenerateResult.Failed(
                            "forced fail",
                            Retryable: true
                        )
                    );
                }

                return _inner.GenerateAsync(
                    locationId,
                    closedWeek,
                    cancellationToken
                );
            }
        }

        private sealed class RecordingWeeklyBriefReadyNotifier
            : IWeeklyBriefReadyNotifier
        {
            public List<(int LocationId, string WeekKey)> Calls { get; } = [];

            public Task NotifyGeneratedAsync(
                int locationId,
                WeeklyBriefClosedWeek closedWeek,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((locationId, closedWeek.WeekKey));
                return Task.CompletedTask;
            }
        }
    }
}
