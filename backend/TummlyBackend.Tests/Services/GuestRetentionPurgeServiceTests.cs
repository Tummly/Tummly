using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class GuestRetentionPurgeServiceTests : IDisposable
    {
        private static readonly DateTime NowUtc = new(
            2026,
            9,
            21,
            12,
            0,
            0,
            DateTimeKind.Utc
        );

        private readonly ApplicationDbContext _context;
        private readonly IGuestRetentionPurgeService _purge;
        private readonly IAdminAuditService _audit;

        public GuestRetentionPurgeServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            _audit = new AdminAuditService(_context, TimeProvider.System);
            _purge = new GuestRetentionPurgeService(
                _context,
                _audit,
                NullLogger<GuestRetentionPurgeService>.Instance
            );
        }

        [Fact]
        public async Task ProcessOnce_DeletesGuests_Stamps_AndAudits_WhenDue()
        {
            var seeded = await SeedRestaurantAsync(
                dormantEnteredAt: NowUtc.AddDays(-90),
                guestCount: 2
            );

            var result = await _purge.ProcessOnceAsync(NowUtc);

            Assert.Equal(1, result.EligibleFound);
            Assert.Equal(1, result.Purged);
            Assert.Equal(0, result.Failed);
            Assert.Equal(2, result.GuestsDeleted);

            Assert.False(
                await _context.LocationGuests.AnyAsync(lg =>
                    lg.RestaurantLocationId == seeded.LocationId
                )
            );
            Assert.False(
                await _context.MasterGuests.AnyAsync(m =>
                    m.RestaurantId == seeded.RestaurantId
                )
            );

            var ba = await _context.BillingAccounts.SingleAsync(a =>
                a.RestaurantId == seeded.RestaurantId
            );
            Assert.Equal(NowUtc, ba.GuestRetentionPurgedAtUtc);

            var audit = await _context.AdminAuditEvents.SingleAsync();
            Assert.Equal(AdminAuditActions.RetentionGuestPurge, audit.Action);
            Assert.Equal("system:retention", audit.ActorIdentity);
            Assert.Equal(AdminAuditTargetTypes.Restaurant, audit.TargetType);
            Assert.Equal(seeded.RestaurantId.ToString(), audit.TargetId);
            Assert.Equal(seeded.RestaurantId, audit.RestaurantId);
            Assert.Equal("""{"guestsDeleted":2}""", audit.DetailJson);
        }

        [Fact]
        public async Task ProcessOnce_StampsEmptyRestaurant()
        {
            var seeded = await SeedRestaurantAsync(
                dormantEnteredAt: NowUtc.AddDays(-90),
                guestCount: 0
            );

            var result = await _purge.ProcessOnceAsync(NowUtc);

            Assert.Equal(1, result.EligibleFound);
            Assert.Equal(1, result.Purged);
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.GuestsDeleted);

            var ba = await _context.BillingAccounts.SingleAsync(a =>
                a.RestaurantId == seeded.RestaurantId
            );
            Assert.Equal(NowUtc, ba.GuestRetentionPurgedAtUtc);

            var audit = await _context.AdminAuditEvents.SingleAsync();
            Assert.Equal(AdminAuditActions.RetentionGuestPurge, audit.Action);
            Assert.Equal("""{"guestsDeleted":0}""", audit.DetailJson);
        }

        [Fact]
        public async Task ProcessOnce_Skips_WhenNotDue()
        {
            var seeded = await SeedRestaurantAsync(
                dormantEnteredAt: NowUtc.AddDays(-10),
                guestCount: 1
            );

            var result = await _purge.ProcessOnceAsync(NowUtc);

            Assert.Equal(0, result.EligibleFound);
            Assert.Equal(0, result.Purged);
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.GuestsDeleted);

            Assert.True(
                await _context.LocationGuests.AnyAsync(lg =>
                    lg.RestaurantLocationId == seeded.LocationId
                )
            );
            Assert.Null(
                (
                    await _context.BillingAccounts.SingleAsync(a =>
                        a.RestaurantId == seeded.RestaurantId
                    )
                ).GuestRetentionPurgedAtUtc
            );
            Assert.Empty(_context.AdminAuditEvents);
        }

        [Fact]
        public async Task ProcessOnce_Skips_WhenAlreadyStamped()
        {
            var stampedAt = NowUtc.AddDays(-1);
            var seeded = await SeedRestaurantAsync(
                dormantEnteredAt: NowUtc.AddDays(-100),
                guestCount: 1,
                guestRetentionPurgedAtUtc: stampedAt
            );

            var result = await _purge.ProcessOnceAsync(NowUtc);

            Assert.Equal(0, result.EligibleFound);
            Assert.Equal(0, result.Purged);
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.GuestsDeleted);

            Assert.True(
                await _context.LocationGuests.AnyAsync(lg =>
                    lg.RestaurantLocationId == seeded.LocationId
                )
            );
            Assert.Equal(
                stampedAt,
                (
                    await _context.BillingAccounts.SingleAsync(a =>
                        a.RestaurantId == seeded.RestaurantId
                    )
                ).GuestRetentionPurgedAtUtc
            );
            Assert.Empty(_context.AdminAuditEvents);
        }

        /// <summary>
        /// Fail-closed: Append runs after the in-memory stamp set but before
        /// SaveChanges. A throw on Append means SaveChanges never runs, so
        /// GuestRetentionPurgedAtUtc and retention.guest_purge stay unset.
        /// </summary>
        [Fact]
        public async Task ProcessOnce_WhenAuditAppendThrows_LeavesStampNullAndWritesNoAudit()
        {
            var seeded = await SeedRestaurantAsync(
                dormantEnteredAt: NowUtc.AddDays(-90),
                guestCount: 1
            );

            var purge = new GuestRetentionPurgeService(
                _context,
                new ThrowingAdminAuditService(),
                NullLogger<GuestRetentionPurgeService>.Instance
            );

            var result = await purge.ProcessOnceAsync(NowUtc);

            Assert.Equal(1, result.EligibleFound);
            Assert.Equal(0, result.Purged);
            Assert.Equal(1, result.Failed);
            Assert.Equal(0, result.GuestsDeleted);

            Assert.Null(
                (
                    await _context.BillingAccounts.SingleAsync(a =>
                        a.RestaurantId == seeded.RestaurantId
                    )
                ).GuestRetentionPurgedAtUtc
            );
            Assert.Empty(_context.AdminAuditEvents);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class ThrowingAdminAuditService : IAdminAuditService
        {
            public void Append(AdminAuditAppendRequest request) =>
                throw new InvalidOperationException("forced audit failure");

            public Task<AdminAuditListResult> ListAsync(
                AdminAuditListQuery query,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();
        }

        private async Task<SeededRestaurant> SeedRestaurantAsync(
            DateTime dormantEnteredAt,
            int guestCount,
            DateTime? guestRetentionPurgedAtUtc = null
        )
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = NowUtc,
                ActivatedAt = NowUtc,
                ActivationExpiresAt = NowUtc.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = NowUtc,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Dormant,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    StarterKitState = StarterKitStates.Unused,
                    DormantEnteredAt = dormantEnteredAt,
                    GuestRetentionPurgedAtUtc = guestRetentionPurgedAtUtc,
                }
            );

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = NowUtc,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            for (var i = 0; i < guestCount; i++)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = $"guest{i}@example.com",
                    NormalizedEmail = $"guest{i}@example.com",
                    CreatedAt = NowUtc,
                };
                _context.MasterGuests.Add(master);
                await _context.SaveChangesAsync();

                _context.LocationGuests.Add(
                    new LocationGuest
                    {
                        MasterGuestId = master.Id,
                        RestaurantLocationId = location.Id,
                        Name = $"Guest {i}",
                        CreatedAt = NowUtc,
                    }
                );
            }

            await _context.SaveChangesAsync();

            return new SeededRestaurant(restaurant.Id, location.Id);
        }

        private sealed record SeededRestaurant(int RestaurantId, int LocationId);
    }
}
