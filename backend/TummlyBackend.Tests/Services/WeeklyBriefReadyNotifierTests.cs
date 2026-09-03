using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam under test: <see cref="IWeeklyBriefReadyNotifier.NotifyGeneratedAsync"/>
    /// (ticket 07). Preference and dedupe are observed through
    /// <see cref="IOperatorNotificationsService"/>, not notifier internals.
    /// </summary>
    public class WeeklyBriefReadyNotifierTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OperatorNotificationsService _notifications;
        private readonly WeeklyBriefReadyNotifier _notifier;

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

        public WeeklyBriefReadyNotifierTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _notifications = new OperatorNotificationsService(
                _context,
                new NullNotificationRealtimePublisher()
            );
            _notifier = new WeeklyBriefReadyNotifier(_context, _notifications);
        }

        [Fact]
        public async Task NotifyGeneratedAsync_ProducesWeeklyBriefReady_WithReportsWeeklyBriefCta()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            var list = await _notifications.ListAsync(seed.UserId);
            var notice = Assert.Single(list);
            Assert.Equal("weekly-brief-ready", notice.Type);
            Assert.Equal("weekly-brief-reminders", notice.Category);
            Assert.Equal(
                $"{seed.LocationId}:2026-W33",
                notice.DedupeKey
            );
            Assert.Equal("View weekly brief", notice.CtaLabel);
            Assert.Equal(
                $"/single-dashboard/reports/weekly-brief?location={seed.LocationId}",
                notice.CtaHref
            );
        }

        [Fact]
        public async Task NotifyGeneratedAsync_UsesMultiDashboardReportsWeeklyBriefPath_WhenAccountIsMulti()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Multi");

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            var list = await _notifications.ListAsync(seed.UserId);
            Assert.Equal(
                $"/multi-dashboard/reports/weekly-brief?location={seed.LocationId}",
                Assert.Single(list).CtaHref
            );
        }

        [Fact]
        public async Task NotifyGeneratedAsync_Dedupes_WhenJobLazyAndBackfillAllSucceed()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);
            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);
            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            Assert.Equal(1, await _context.Notifications.CountAsync());
        }

        [Fact]
        public async Task NotifyGeneratedAsync_NoOps_WhenWeeklyBriefRemindersPreferenceOff()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");
            await _notifications.SetPreferencesAsync(
                seed.UserId,
                new NotificationPreferencesDto { WeeklyBriefReminders = false }
            );

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            Assert.Empty(_context.Notifications);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<(int UserId, int LocationId)> SeedOwnedLocationAsync(
            string accountType
        )
        {
            var user = new User
            {
                Email = $"op-{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Op",
                Role = "Owner",
                AccountType = accountType,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Notify Restaurant",
                AccountType = accountType,
                OwnerUserId = user.Id,
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

            return (user.Id, location.Id);
        }
    }
}
