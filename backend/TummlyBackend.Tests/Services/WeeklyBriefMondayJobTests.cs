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
    /// Seam under test: <see cref="IWeeklyBriefMondayJob.ProcessAsync"/>
    /// (ticket 05). Generate is a collaborator; persistence observed via
    /// <see cref="ApplicationDbContext.WeeklyBriefs"/>.
    /// </summary>
    public class WeeklyBriefMondayJobTests : IDisposable
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

        public WeeklyBriefMondayJobTests()
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
        public async Task ProcessAsync_GeneratesBrief_WhenMondayStartedAndRowMissing()
        {
            var locationId = await SeedLocationAsync("Harbour Kitchen");

            var batch = await _job.ProcessAsync(LondonMondayMidnightUtc);

            Assert.Equal(1, batch.Generated);
            Assert.Equal(0, batch.Skipped);
            Assert.Equal(0, batch.Failed);
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
        public async Task ProcessAsync_SkipsExistingSucceededRow_WithoutOverwrite()
        {
            var locationId = await SeedLocationAsync("Harbour Kitchen");
            const string sentinelBody = "{\"headline\":\"keep-me\"}";
            await SeedSucceededBriefAsync(
                locationId,
                weekKey: "monday:2026-08-10",
                bodyJson: sentinelBody
            );

            var batch = await _job.ProcessAsync(LondonMondayMidnightUtc);

            Assert.Equal(0, batch.Generated);
            Assert.Equal(1, batch.Skipped);
            Assert.Equal(0, batch.Failed);
            Assert.Equal(0, _provider.CallCount);
            Assert.Empty(_notifier.Calls);
            var row = await _context.WeeklyBriefs.SingleAsync(brief =>
                brief.LocationId == locationId && brief.WeekKey == "monday:2026-08-10"
            );
            Assert.Equal(sentinelBody, row.BodyJson);
        }

        [Fact]
        public async Task ProcessAsync_SelectsOnlyLocationsNeedingGenerate()
        {
            var needsGenerate = await SeedLocationAsync("Needs Generate");
            var alreadyReady = await SeedLocationAsync("Already Ready");
            await SeedSucceededBriefAsync(
                alreadyReady,
                weekKey: "monday:2026-08-10",
                bodyJson: "{\"headline\":\"existing\"}"
            );

            var batch = await _job.ProcessAsync(LondonMondayMidnightUtc);

            Assert.Equal(1, batch.Generated);
            Assert.Equal(1, batch.Skipped);
            Assert.Equal(0, batch.Failed);
            Assert.Equal(1, _provider.CallCount);
            Assert.True(
                await _context.WeeklyBriefs.AnyAsync(row =>
                    row.LocationId == needsGenerate && row.WeekKey == "monday:2026-08-10"
                )
            );
            Assert.Single(_notifier.Calls);
            Assert.Equal(needsGenerate, _notifier.Calls[0].LocationId);
        }

        [Fact]
        public async Task ProcessAsync_JustBeforeMonday_SkipsWhenNotGenerateDay()
        {
            var locationId = await SeedLocationAsync("Harbour Kitchen");
            var justBeforeMonday = new DateTime(
                2026,
                8,
                16,
                22,
                59,
                59,
                DateTimeKind.Utc
            );

            var batch = await _job.ProcessAsync(justBeforeMonday);

            Assert.Equal(0, batch.Generated);
            Assert.Equal(1, batch.Skipped);
            Assert.Equal(0, _provider.CallCount);
            Assert.Empty(_notifier.Calls);
            Assert.Equal(
                0,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId
                )
            );
        }

        [Fact]
        public async Task ProcessAsync_FridayStart_GeneratesOnFriday_NotMonday()
        {
            var mondayOnly = await SeedLocationAsync(
                "Monday Venue",
                weekStartsOn: "monday"
            );
            var fridayVenue = await SeedLocationAsync(
                "Friday Venue",
                weekStartsOn: "friday"
            );
            var fridayMidnight = new DateTime(
                2026,
                8,
                20,
                23,
                0,
                0,
                DateTimeKind.Utc
            );

            var batch = await _job.ProcessAsync(fridayMidnight);

            Assert.Equal(1, batch.Generated);
            Assert.Equal(1, batch.Skipped);
            Assert.Equal(
                0,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == mondayOnly
                )
            );
            Assert.Equal(
                1,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == fridayVenue
                    && row.WeekKey == "friday:2026-08-14"
                    && row.Status == WeeklyBriefStatus.Succeeded
                )
            );
        }

        [Fact]
        public async Task ProcessAsync_SkipsExistingSucceededRow_WithoutOverwrite_FridayKey()
        {
            var locationId = await SeedLocationAsync(
                "Friday Venue",
                weekStartsOn: "friday"
            );
            const string sentinelBody = "{\"headline\":\"keep-friday\"}";
            await SeedSucceededBriefAsync(
                locationId,
                weekKey: "friday:2026-08-14",
                bodyJson: sentinelBody
            );
            var fridayMidnight = new DateTime(
                2026,
                8,
                20,
                23,
                0,
                0,
                DateTimeKind.Utc
            );

            var batch = await _job.ProcessAsync(fridayMidnight);

            Assert.Equal(0, batch.Generated);
            Assert.Equal(1, batch.Skipped);
            Assert.Equal(0, _provider.CallCount);
            var row = await _context.WeeklyBriefs.SingleAsync(brief =>
                brief.LocationId == locationId && brief.WeekKey == "friday:2026-08-14"
            );
            Assert.Equal(sentinelBody, row.BodyJson);
        }

        [Fact]
        public async Task ProcessAsync_ContinuesBatch_WhenOneLocationFails()
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

            var batch = await job.ProcessAsync(LondonMondayMidnightUtc);

            Assert.Equal(1, batch.Generated);
            Assert.Equal(1, batch.Failed);
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

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedLocationAsync(
            string locationName,
            string? weekStartsOn = null
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Job Restaurant",
                AccountType = "Single",
                OwnerUserId = 7,
                WeekStartsOn = weekStartsOn,
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
