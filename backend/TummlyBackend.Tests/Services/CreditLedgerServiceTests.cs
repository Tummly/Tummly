using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class CreditLedgerServiceTests : IDisposable
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly DateTime _now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
        private readonly TimeProvider _clock;

        public CreditLedgerServiceTests()
        {
            _clock = new FixedTimeProvider(_now);
        }

        [Fact]
        public async Task ConsumeOnSuccess_BindsIncludedThenPilotThenEarliestTopUp()
        {
            var harness = await SeedAsync();
            var includedId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                10,
                createdAtUtc: _now.AddDays(-3),
                expiresAtUtc: _now.AddDays(10)
            );
            var pilotId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                10,
                createdAtUtc: _now.AddDays(-2),
                expiresAtUtc: null
            );
            var lateTopUpId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.TopupAllocation,
                10,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddMonths(11)
            );
            var earlyTopUpId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.TopupAllocation,
                10,
                createdAtUtc: _now,
                expiresAtUtc: _now.AddMonths(1)
            );

            var result = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 25,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.Inserted.Count);
            Assert.All(
                result.Inserted,
                row => Assert.Equal(CreditLedgerEntryTypes.Consumption, row.EntryType)
            );
            Assert.Equal(includedId, result.Inserted[0].AllocationId);
            Assert.Equal(10, result.Inserted[0].Quantity);
            Assert.Equal(pilotId, result.Inserted[1].AllocationId);
            Assert.Equal(10, result.Inserted[1].Quantity);
            Assert.Equal(earlyTopUpId, result.Inserted[2].AllocationId);
            Assert.Equal(5, result.Inserted[2].Quantity);
            Assert.DoesNotContain(result.Inserted, row => row.AllocationId == lateTopUpId);

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var email = Channel(snapshot, CreditChannels.Email);
            Assert.Equal(15, email.Remaining);
            Assert.Equal(0, email.Held);
        }

        [Fact]
        public async Task ConsumeOnSuccess_ClockZerosExpiredBindable()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                10,
                createdAtUtc: _now.AddMonths(-1),
                expiresAtUtc: _now.AddDays(-1)
            );
            var liveTopUpId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.TopupAllocation,
                10,
                createdAtUtc: _now.AddDays(-2),
                expiresAtUtc: _now.AddMonths(12)
            );

            var result = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 10,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(result.Succeeded);
            var inserted = Assert.Single(result.Inserted);
            Assert.Equal(liveTopUpId, inserted.AllocationId);
            Assert.Equal(10, inserted.Quantity);

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            Assert.Equal(0, Channel(snapshot, CreditChannels.Email).Remaining);
        }

        [Fact]
        public async Task ConsumeOnSuccess_ConcurrentWritesCannotOverspend()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                10,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddDays(20)
            );

            var secondContext = CreateContext();
            var secondLedger = new CreditLedgerService(secondContext, _clock);
            var request = new CreditLedgerConsumeRequest
            {
                RestaurantId = harness.RestaurantId,
                Channel = CreditChannels.Email,
                Units = 10,
                LocationId = harness.LocationId,
            };

            var outcomes = await Task.WhenAll(
                harness.Ledger.ConsumeOnSuccessAsync(request),
                secondLedger.ConsumeOnSuccessAsync(request)
            );
            Assert.Equal(1, outcomes.Count(row => row.Succeeded));
            Assert.Equal(1, outcomes.Count(row => !row.Succeeded));
            Assert.Equal(
                10,
                outcomes.Where(row => row.Succeeded).Sum(row =>
                    row.Inserted.Sum(item => item.Quantity)
                )
            );

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            Assert.Equal(0, Channel(snapshot, CreditChannels.Email).Remaining);
            secondContext.Dispose();
        }

        [Fact]
        public async Task ConsumeOnSuccess_WritesConsumptionOnly()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                8,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: null
            );

            var result = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 3,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(result.Succeeded);
            Assert.All(
                result.Inserted,
                row =>
                {
                    Assert.Equal(CreditLedgerEntryTypes.Consumption, row.EntryType);
                    Assert.True(string.IsNullOrEmpty(row.ReservationRef));
                }
            );
            Assert.Equal(3, result.Inserted.Sum(row => row.Quantity));

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var email = Channel(snapshot, CreditChannels.Email);
            Assert.Equal(5, email.Remaining);
            Assert.Equal(0, email.Held);
            Assert.Equal(3, email.UsedThisCycle);
        }

        [Fact]
        public async Task ConsumeOnSuccess_RejectsWhenRemainingWouldFallBelowHeld()
        {
            var harness = await SeedAsync();
            var grantId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                10,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddDays(20)
            );
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Reservation,
                    Quantity = 10,
                    AllocationId = grantId,
                    ReservationRef = Guid.NewGuid().ToString("D"),
                    LocationId = harness.LocationId,
                    CreatedAtUtc = _now,
                }
            );
            await harness.Context.SaveChangesAsync();

            var result = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 1,
                    LocationId = harness.LocationId,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Empty(result.Inserted);

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var email = Channel(snapshot, CreditChannels.Email);
            Assert.Equal(0, email.Remaining);
            Assert.Equal(10, email.Held);
        }

        [Fact]
        public async Task ConsumeOnSuccess_RejectsWhenLocationIdMissing()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                8,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: null
            );

            var result = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 1,
                    LocationId = null,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("location_required", result.Code);
            Assert.Empty(result.Inserted);
        }

        [Fact]
        public async Task StaffManualAdjust_GrantIncreasesRemaining()
        {
            var harness = await SeedAsync();
            var result = await harness.Ledger.StaffManualAdjustAsync(
                new StaffManualAdjustRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Direction = StaffManualAdjustDirections.Grant,
                    Quantity = 25,
                    Reason = "Goodwill credit for onboarding issue",
                    ActorStaffUserId = 1,
                }
            );

            Assert.True(result.Succeeded);
            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            Assert.Equal(25, Channel(snapshot, CreditChannels.Email).Remaining);

            var activity = await harness.Context.RestaurantBillingActivities
                .SingleAsync(row => row.RestaurantId == harness.RestaurantId);
            Assert.Equal(BillingActivityKinds.ManualCreditAdjusted, activity.Kind);
            Assert.Equal(BillingActivityActors.TummlySupport, activity.ActorDisplayName);
            Assert.Equal(BillingManualAdjustDirections.Add, activity.ManualAdjustDirection);
            Assert.Equal(25, activity.Qty);
        }

        [Fact]
        public async Task StaffManualAdjust_DebitExceedsBindableRefused()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                10,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddDays(20)
            );

            var result = await harness.Ledger.StaffManualAdjustAsync(
                new StaffManualAdjustRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Direction = StaffManualAdjustDirections.Debit,
                    Quantity = 11,
                    Reason = "Correct duplicate grant",
                    ActorStaffUserId = 1,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("insufficient_credits", result.Code);
            Assert.Empty(result.Inserted);
        }

        [Fact]
        public async Task StaffManualAdjust_DebitHeldRefused()
        {
            var harness = await SeedAsync();
            var grantId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                10,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddDays(20)
            );
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Reservation,
                    Quantity = 10,
                    AllocationId = grantId,
                    ReservationRef = Guid.NewGuid().ToString("D"),
                    LocationId = harness.LocationId,
                    CreatedAtUtc = _now,
                }
            );
            await harness.Context.SaveChangesAsync();

            var result = await harness.Ledger.StaffManualAdjustAsync(
                new StaffManualAdjustRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Direction = StaffManualAdjustDirections.Debit,
                    Quantity = 1,
                    Reason = "Remove mistaken grant",
                    ActorStaffUserId = 1,
                    AllocationId = grantId,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("held_credits", result.Code);
            Assert.Empty(result.Inserted);
        }

        [Fact]
        public async Task StaffManualAdjust_ReasonRequired()
        {
            var harness = await SeedAsync();
            var result = await harness.Ledger.StaffManualAdjustAsync(
                new StaffManualAdjustRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Direction = StaffManualAdjustDirections.Grant,
                    Quantity = 5,
                    Reason = "   ",
                    ActorStaffUserId = 1,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("reason_required", result.Code);
        }

        [Fact]
        public async Task StaffManualAdjust_NegativeRemainingRefused()
        {
            var harness = await SeedAsync();
            var grantId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                5,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddDays(20)
            );
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Consumption,
                    Quantity = 8,
                    AllocationId = grantId,
                    LocationId = harness.LocationId,
                    CreatedAtUtc = _now,
                }
            );
            await harness.Context.SaveChangesAsync();

            var result = await harness.Ledger.StaffManualAdjustAsync(
                new StaffManualAdjustRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Direction = StaffManualAdjustDirections.Debit,
                    Quantity = 1,
                    Reason = "Attempt over-debit on negative remaining",
                    ActorStaffUserId = 1,
                    AllocationId = grantId,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("insufficient_credits", result.Code);
            Assert.Empty(result.Inserted);
        }

        public void Dispose()
        {
        }

        private async Task<Harness> SeedAsync()
        {
            var context = CreateContext();
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Ledger Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Ledger Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    PricebookId
                )
            );
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            return new Harness(
                context,
                new CreditLedgerService(context, _clock),
                new CreditBalanceSnapshotService(context, _clock),
                restaurant.Id,
                location.Id
            );
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private async Task<Guid> InsertGrantAsync(
            ApplicationDbContext context,
            int restaurantId,
            string entryType,
            int quantity,
            DateTime createdAtUtc,
            DateTime? expiresAtUtc
        )
        {
            var id = Guid.NewGuid();
            var isIncludedClass =
                entryType == CreditLedgerEntryTypes.IncludedAllocation
                || entryType == CreditLedgerEntryTypes.PlanMigration;
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = id,
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = entryType,
                    Quantity = quantity,
                    PricebookVersion = PricebookId,
                    ExpiresAtUtc = expiresAtUtc,
                    PeriodStartUtc = isIncludedClass ? createdAtUtc : null,
                    CreatedAtUtc = createdAtUtc,
                }
            );
            await context.SaveChangesAsync();
            return id;
        }

        private static CreditBalanceChannelSnapshot Channel(
            CreditBalanceAccountSnapshot? snapshot,
            string channel
        )
        {
            Assert.NotNull(snapshot);
            return Assert.Single(
                snapshot.Channels,
                row => row.Channel == channel
            );
        }

        private sealed record Harness(
            ApplicationDbContext Context,
            CreditLedgerService Ledger,
            CreditBalanceSnapshotService Snapshot,
            int RestaurantId,
            int LocationId
        );

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
            }

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }
    }
}
