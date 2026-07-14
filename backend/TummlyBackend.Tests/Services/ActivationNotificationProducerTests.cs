using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ActivationNotificationProducerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OperatorNotificationsService _notifications;
        private readonly ActivationNotificationProducer _producer;

        public ActivationNotificationProducerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _notifications = new OperatorNotificationsService(
                _context,
                new NullNotificationRealtimePublisher()
            );
            _producer = new ActivationNotificationProducer(
                _context,
                _notifications
            );
        }

        [Fact]
        public async Task ProcessAsync_Produces15DayNotice_WhenCeilDaysRemainingIs15()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var expiresAt = now.AddDays(14).AddHours(6); // ceil → 15
            var user = await SeedActivatedUserAsync(expiresAt);

            var batch = await _producer.ProcessAsync(now);

            Assert.Equal(1, batch.Produced);
            var list = await _notifications.ListAsync(user.Id);
            Assert.Single(list);
            Assert.Equal("activation-ending-15-days", list[0].Type);
            Assert.Null(list[0].Capability);
            Assert.Equal("account-notices", list[0].Category);
            Assert.Equal(expiresAt.ToString("O"), list[0].DedupeKey);
        }

        [Fact]
        public async Task ProcessAsync_Produces5DayNotice_WhenCeilDaysRemainingIs5()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var expiresAt = now.AddDays(4).AddHours(1); // ceil → 5
            var user = await SeedActivatedUserAsync(expiresAt);

            var batch = await _producer.ProcessAsync(now);

            Assert.Equal(1, batch.Produced);
            var list = await _notifications.ListAsync(user.Id);
            Assert.Equal("activation-ending-5-days", list[0].Type);
            Assert.Equal(expiresAt.ToString("O"), list[0].DedupeKey);
        }

        [Fact]
        public async Task ProcessAsync_ProducesExpiredNotice_WhenActivationExpiresAtPassed()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var expiresAt = now.AddMinutes(-1);
            var user = await SeedActivatedUserAsync(expiresAt);

            var batch = await _producer.ProcessAsync(now);

            Assert.Equal(1, batch.Produced);
            var list = await _notifications.ListAsync(user.Id);
            Assert.Equal("activation-expired", list[0].Type);
            Assert.Equal(expiresAt.ToString("O"), list[0].DedupeKey);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotProduce_WhenDaysRemainingNotAtThreshold()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            await SeedActivatedUserAsync(now.AddDays(20));

            var batch = await _producer.ProcessAsync(now);

            Assert.Equal(0, batch.Produced);
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task ProcessAsync_DedupesSameThresholdForSameActivationWindow()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var expiresAt = now.AddDays(15);
            await SeedActivatedUserAsync(expiresAt);

            await _producer.ProcessAsync(now);
            var second = await _producer.ProcessAsync(now);

            Assert.Equal(0, second.Produced);
            Assert.Equal(1, await _context.Notifications.CountAsync());
        }

        [Fact]
        public async Task ProcessAsync_ProducesAgain_WhenActivationWindowExtended()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var firstExpires = now.AddDays(14).AddHours(1); // ceil → 15
            var user = await SeedActivatedUserAsync(firstExpires);

            await _producer.ProcessAsync(now);

            var extendedExpires = now.AddDays(14).AddHours(12); // ceil → 15, new window
            user.ActivationExpiresAt = extendedExpires;
            await _context.SaveChangesAsync();

            var second = await _producer.ProcessAsync(now);

            Assert.Equal(1, second.Produced);
            Assert.Equal(2, await _context.Notifications.CountAsync());
            var keys = await _context.Notifications
                .Select(n => n.DedupeKey)
                .OrderBy(k => k)
                .ToListAsync();
            Assert.Contains(firstExpires.ToString("O"), keys);
            Assert.Contains(extendedExpires.ToString("O"), keys);
        }

        [Fact]
        public async Task ProcessAsync_NoOps_WhenAccountNoticesPreferenceOff()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var user = await SeedActivatedUserAsync(now.AddDays(15));
            await _notifications.SetPreferencesAsync(
                user.Id,
                new NotificationPreferencesDto { AccountNotices = false }
            );

            var batch = await _producer.ProcessAsync(now);

            Assert.Equal(0, batch.Produced);
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task ProcessAsync_SkipsUsersWithoutActivatedAt()
        {
            var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            _context.Users.Add(new User
            {
                Email = "pending@example.com",
                PasswordHash = "x",
                FullName = "Pending",
                Role = "Owner",
                CreatedAt = now,
                ActivatedAt = null,
                ActivationExpiresAt = now.AddDays(15)
            });
            await _context.SaveChangesAsync();

            var batch = await _producer.ProcessAsync(now);

            Assert.Equal(0, batch.Produced);
        }

        private async Task<User> SeedActivatedUserAsync(DateTime expiresAt)
        {
            var user = new User
            {
                Email = $"op-{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Op",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = expiresAt.AddDays(-30),
                ActivationExpiresAt = expiresAt
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
