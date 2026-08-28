using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CreditThresholdEvaluatorTests : IDisposable
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly DateTime _now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
        private readonly TimeProvider _clock;
        private readonly RecordingBillingAccountNoticeNotifier _notifier;

        public CreditThresholdEvaluatorTests()
        {
            _clock = new FixedTimeProvider(_now);
            _notifier = new RecordingBillingAccountNoticeNotifier();
        }

        [Fact]
        public async Task Crossing80_WritesWatermark80AndEnqueuesEvent()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                100,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: null
            );

            var consume = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 80,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(consume.Succeeded);
            var watermark = await LoadWatermarkAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email
            );
            Assert.Equal(CreditThresholdBands.Band80, watermark);

            var notification = Assert.Single(_notifier.CreditThresholdCalls);
            Assert.Equal(80, notification.ThresholdBand);
            Assert.Equal(CreditChannels.Email, notification.Channel);
            Assert.Equal("pilot-once", notification.PeriodKey);
        }

        [Fact]
        public async Task SkipAheadTo100_Marks80And90Passed()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                100,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: null
            );

            var consume = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 100,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(consume.Succeeded);
            var watermark = await LoadWatermarkAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email
            );
            Assert.Equal(CreditThresholdBands.Band100, watermark);

            Assert.Equal(
                [80, 90, 100],
                _notifier.CreditThresholdCalls
                    .Select(row => row.ThresholdBand)
                    .OrderBy(row => row)
                    .ToList()
            );
        }

        [Fact]
        public async Task SoftLock_AdvancesWatermarkWithNoNotify()
        {
            var harness = await SeedAsync(BillingStatuses.SoftLock);
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                100,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: null
            );

            var consume = await harness.Ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 80,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(consume.Succeeded);
            var watermark = await LoadWatermarkAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email
            );
            Assert.Equal(CreditThresholdBands.Band80, watermark);
            Assert.Empty(_notifier.CreditThresholdCalls);
        }

        [Fact]
        public async Task NewIncludedMint_ResetsBands()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                100,
                createdAtUtc: _now.AddDays(-30),
                expiresAtUtc: null
            );
            harness.Context.CreditWarningStates.Add(
                new CreditWarningState
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    HighestBandThisPeriod = CreditThresholdBands.Band90,
                }
            );
            await harness.Context.SaveChangesAsync();

            await harness.Evaluator.ResetBandsForIncludedPeriodMintAsync(
                harness.RestaurantId
            );
            await harness.Context.SaveChangesAsync();

            var watermark = await LoadWatermarkAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email
            );
            Assert.Equal(CreditThresholdBands.None, watermark);
        }

        public void Dispose()
        {
        }

        private async Task<Harness> SeedAsync(
            string billingStatus = BillingStatuses.Pilot
        )
        {
            var context = CreateContext();
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Threshold Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Threshold Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                PricebookId
            );
            billingAccount.BillingStatus = billingStatus;
            context.BillingAccounts.Add(billingAccount);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var evaluator = new CreditThresholdEvaluator(
                context,
                _clock,
                _notifier
            );
            var ledger = new CreditLedgerService(
                context,
                _clock,
                new StubPricebookCatalog(),
                evaluator
            );

            return new Harness(
                context,
                ledger,
                evaluator,
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

        private static async Task<int> LoadWatermarkAsync(
            ApplicationDbContext context,
            int restaurantId,
            string channel
        )
        {
            var row = await context.CreditWarningStates.SingleAsync(entry =>
                entry.RestaurantId == restaurantId && entry.Channel == channel
            );
            return row.HighestBandThisPeriod;
        }

        private sealed record Harness(
            ApplicationDbContext Context,
            CreditLedgerService Ledger,
            CreditThresholdEvaluator Evaluator,
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

        private sealed class RecordingBillingAccountNoticeNotifier
            : IBillingAccountNoticeNotifier
        {
            public List<CreditThresholdCall> CreditThresholdCalls { get; } = [];

            public Task NotifyCreditThresholdCrossedAsync(
                int restaurantId,
                string channel,
                int thresholdBand,
                string periodKey,
                string billingStatus,
                bool isPilot,
                CancellationToken cancellationToken = default
            )
            {
                CreditThresholdCalls.Add(
                    new CreditThresholdCall(
                        restaurantId,
                        channel,
                        thresholdBand,
                        periodKey,
                        billingStatus,
                        isPilot
                    )
                );
                return Task.CompletedTask;
            }

            public Task NotifyPaymentFailureDayStepAsync(
                int restaurantId,
                int dayStep,
                string episodeId,
                CancellationToken cancellationToken = default
            )
            {
                return Task.CompletedTask;
            }

            public Task NotifyUnpaidPilotLockEnterAsync(
                int restaurantId,
                string episodeKey,
                CancellationToken cancellationToken = default
            )
            {
                return Task.CompletedTask;
            }
        }

        private sealed record CreditThresholdCall(
            int RestaurantId,
            string Channel,
            int ThresholdBand,
            string PeriodKey,
            string BillingStatus,
            bool IsPilot
        );
    }
}
