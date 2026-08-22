using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam under test: <see cref="IWeeklyBriefGenerateService.GenerateAsync"/>
    /// (ticket 03). Provider Fake is a collaborator; persistence observed via
    /// the generate result and <see cref="ApplicationDbContext.WeeklyBriefs"/>.
    /// </summary>
    public class WeeklyBriefGenerateServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeWeeklyBriefProvider _provider;
        private readonly WeeklyBriefGenerateService _service;

        private static readonly WeeklyBriefClosedWeek ClosedWeek = new(
            WeekKey: "2026-W33",
            CoverageStartUtc: new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
            CoverageEndUtcExclusive: new DateTime(
                2026,
                8,
                16,
                23,
                0,
                0,
                DateTimeKind.Utc
            )
        );

        public WeeklyBriefGenerateServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _provider = new FakeWeeklyBriefProvider();
            _service = new WeeklyBriefGenerateService(
                _context,
                _provider,
                NullLogger<WeeklyBriefGenerateService>.Instance
            );
        }

        [Fact]
        public async Task GenerateAsync_FirstCall_PersistsSucceededRow()
        {
            var locationId = await SeedLocationAsync();
            _provider.UseDefaultFixtures();

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var ok = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(result);
            Assert.True(ok.Created);
            Assert.Equal(WeeklyBriefStatus.Succeeded, ok.Brief.Status);
            Assert.Equal("2026-W33", ok.Brief.WeekKey);
            Assert.False(string.IsNullOrWhiteSpace(ok.Brief.BodyJson));
            Assert.False(string.IsNullOrWhiteSpace(ok.Brief.MetricsJson));
            Assert.Equal(1, _provider.CallCount);
            Assert.Equal(
                1,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId && row.WeekKey == "2026-W33"
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_SecondCall_ReturnsExistingWithoutProvider()
        {
            var locationId = await SeedLocationAsync();
            _provider.UseDefaultFixtures();

            var first = await _service.GenerateAsync(locationId, ClosedWeek);
            var firstOk = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(first);
            Assert.True(firstOk.Created);
            _provider.ResetCallCount();

            var second = await _service.GenerateAsync(locationId, ClosedWeek);

            var secondOk = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(
                second
            );
            Assert.False(secondOk.Created);
            Assert.Equal(firstOk.Brief.Id, secondOk.Brief.Id);
            Assert.Equal(firstOk.Brief.BodyJson, secondOk.Brief.BodyJson);
            Assert.Equal(0, _provider.CallCount);
            Assert.Equal(
                1,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId && row.WeekKey == "2026-W33"
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_ProviderFail_LeavesNoReadyRow()
        {
            var locationId = await SeedLocationAsync();
            _provider.Fail(retryable: true);

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var failed = Assert.IsType<WeeklyBriefGenerateResult.Failed>(result);
            Assert.True(failed.Retryable);
            Assert.Equal(1, _provider.CallCount);
            Assert.Equal(
                0,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId && row.WeekKey == "2026-W33"
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_DoesNotDebitAiCredits()
        {
            // Free call: generate service has no credit / billing collaborator.
            // Guard: type surface must not reference campaign billing reserve.
            var ctor = typeof(WeeklyBriefGenerateService).GetConstructors().Single();
            Assert.DoesNotContain(
                ctor.GetParameters(),
                parameter =>
                    parameter.ParameterType.Name.Contains(
                        "Billing",
                        StringComparison.Ordinal
                    )
                    || parameter.ParameterType.Name.Contains(
                        "Credit",
                        StringComparison.Ordinal
                    )
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Test Restaurant",
                AccountType = "Single",
                OwnerUserId = 7,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Harbour Kitchen",
                Address = "1 Harbour Way",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }
    }
}
