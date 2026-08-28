using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

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
            var secondLedger = new CreditLedgerService(
                secondContext,
                _clock,
                new StubPricebookCatalog()
            );
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

        [Fact]
        public async Task StaffReverse_DebitManualAdjustmentRestoresRemaining_WithoutActivity()
        {
            var harness = await SeedAsync();
            var grantId = await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                20,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddDays(20)
            );
            var debit = await harness.Ledger.StaffManualAdjustAsync(
                new StaffManualAdjustRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Direction = StaffManualAdjustDirections.Debit,
                    Quantity = 5,
                    Reason = "Mistaken debit",
                    ActorStaffUserId = 1,
                    AllocationId = grantId,
                }
            );
            Assert.True(debit.Succeeded);
            var debitId = Assert.Single(debit.Inserted).Id;

            var reverse = await harness.Ledger.StaffReverseAsync(
                new StaffReverseRequest
                {
                    ReversedEntryId = debitId,
                    Reason = "Undo mistaken debit",
                    ActorStaffUserId = 1,
                }
            );

            Assert.True(reverse.Succeeded);
            Assert.Equal(
                CreditLedgerEntryTypes.Reversal,
                Assert.Single(reverse.Inserted).EntryType
            );
            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            Assert.Equal(20, Channel(snapshot, CreditChannels.Email).Remaining);
            Assert.Empty(
                harness.Context.RestaurantBillingActivities.Where(row =>
                    row.RestaurantId == harness.RestaurantId
                    && row.Kind == BillingActivityKinds.ManualCreditAdjusted
                    && row.ManualAdjustDirection == BillingManualAdjustDirections.Add
                )
            );
            Assert.Equal(
                1,
                await harness.Context.RestaurantBillingActivities.CountAsync(row =>
                    row.RestaurantId == harness.RestaurantId
                )
            );
        }

        [Fact]
        public async Task MintTopupAllocation_UsesCurrentPricebookAndTwelveMonthExpiry()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-email-001";

            var result = await harness.Ledger.MintTopupAllocationAsync(
                new CreditLedgerMintTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Quantity = 500,
                    SourcePaymentRef = paymentRef,
                }
            );

            Assert.True(result.Succeeded);
            Assert.NotNull(result.AllocationId);

            var grant = await harness.Context.CreditLedgerEntries.SingleAsync(row =>
                row.Id == result.AllocationId
            );
            Assert.Equal(CreditLedgerEntryTypes.TopupAllocation, grant.EntryType);
            Assert.Equal(paymentRef, grant.SourcePaymentRef);
            Assert.Equal(PricebookId, grant.PricebookVersion);
            Assert.Equal(_now.AddMonths(12), grant.ExpiresAtUtc);
        }

        [Fact]
        public async Task DrainUnusedTopup_TakesBindableOnly()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-drain-bindable";
            var allocationId = await MintTopupAsync(
                harness,
                CreditChannels.Email,
                100,
                paymentRef
            );
            await InsertConsumptionAsync(
                harness,
                allocationId,
                quantity: 30,
                locationId: harness.LocationId
            );
            await InsertReservationAsync(
                harness,
                allocationId,
                quantity: 20,
                locationId: harness.LocationId
            );

            var result = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                }
            );

            Assert.True(result.Succeeded);
            var channel = Assert.Single(result.Channels);
            Assert.Equal(CreditChannels.Email, channel.Channel);
            Assert.Equal(50, channel.Refunded);
            Assert.Equal(20, channel.Held);
            Assert.Equal(30, channel.Consumed);

            var refunds = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.Refund
                    && row.SourcePaymentRef == paymentRef
                )
                .ToListAsync();
            Assert.Single(refunds);
            Assert.Equal(50, refunds[0].Quantity);
            Assert.Equal(CorrectionSources.Dispute, refunds[0].CorrectionSource);
        }

        [Fact]
        public async Task DrainUnusedTopup_SecondDrainInsertsNothing()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-drain-repeat";
            await MintTopupAsync(harness, CreditChannels.Email, 40, paymentRef);

            var first = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.PaymentRefund,
                }
            );
            Assert.True(first.Succeeded);
            Assert.Equal(40, Assert.Single(first.Channels).Refunded);

            var refundCountBefore = await harness.Context.CreditLedgerEntries.CountAsync(row =>
                row.EntryType == CreditLedgerEntryTypes.Refund
            );

            var second = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.PaymentRefund,
                }
            );

            Assert.True(second.Succeeded);
            Assert.Equal(0, Assert.Single(second.Channels).Refunded);
            Assert.Equal(
                refundCountBefore,
                await harness.Context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.Refund
                )
            );
        }

        [Fact]
        public async Task DrainUnusedTopup_DoesNotClawConsumedUnits()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-drain-consume";
            var allocationId = await MintTopupAsync(
                harness,
                CreditChannels.Email,
                80,
                paymentRef
            );
            await InsertConsumptionAsync(
                harness,
                allocationId,
                quantity: 25,
                locationId: harness.LocationId
            );

            var result = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                }
            );

            Assert.True(result.Succeeded);
            var channel = Assert.Single(result.Channels);
            Assert.Equal(25, channel.Consumed);
            Assert.Equal(55, channel.Refunded);

            var consumptionQty = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.AllocationId == allocationId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
                .SumAsync(row => row.Quantity);
            Assert.Equal(25, consumptionQty);
        }

        [Fact]
        public async Task DrainUnusedTopup_DrainInvariantKeepsBindableZeroUntilRestore()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-drain-invariant";
            var allocationId = await MintTopupAsync(
                harness,
                CreditChannels.Email,
                100,
                paymentRef
            );
            var reservationRef = Guid.NewGuid().ToString("D");
            await InsertReservationAsync(
                harness,
                allocationId,
                quantity: 30,
                locationId: harness.LocationId,
                reservationRef: reservationRef
            );

            var drain = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                }
            );
            Assert.True(drain.Succeeded);
            Assert.Equal(70, Assert.Single(drain.Channels).Refunded);

            var snapshotAfterDrain = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var emailAfterDrain = Channel(snapshotAfterDrain, CreditChannels.Email);
            Assert.Equal(0, emailAfterDrain.Remaining);
            Assert.Equal(30, emailAfterDrain.Held);

            var release = await harness.Ledger.ReleaseHeldAsync(
                new CreditLedgerReleaseHeldRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    AllocationId = allocationId,
                    ReservationRef = reservationRef,
                    Quantity = 30,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(release.Succeeded);
            Assert.Contains(
                release.Inserted,
                row => row.EntryType == CreditLedgerEntryTypes.Release && row.Quantity == 30
            );
            Assert.Contains(
                release.Inserted,
                row => row.EntryType == CreditLedgerEntryTypes.Refund && row.Quantity == 30
            );
            Assert.DoesNotContain(
                release.Inserted,
                row => row.EntryType == CreditLedgerEntryTypes.Expiry
            );

            var snapshotAfterRelease = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var emailAfterRelease = Channel(snapshotAfterRelease, CreditChannels.Email);
            Assert.Equal(0, emailAfterRelease.Remaining);
            Assert.Equal(0, emailAfterRelease.Held);

            var refundCountBefore = await harness.Context.CreditLedgerEntries.CountAsync(row =>
                row.EntryType == CreditLedgerEntryTypes.Refund
            );
            var secondDrain = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                }
            );
            Assert.True(secondDrain.Succeeded);
            Assert.Equal(0, Assert.Single(secondDrain.Channels).Refunded);
            Assert.Equal(
                refundCountBefore,
                await harness.Context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.Refund
                )
            );
        }

        [Fact]
        public async Task RestoreUnusedTopup_MerchantWonRestoresBindableWithoutClawingConsumed()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-restore";
            var allocationId = await MintTopupAsync(
                harness,
                CreditChannels.Email,
                60,
                paymentRef
            );
            await InsertConsumptionAsync(
                harness,
                allocationId,
                quantity: 15,
                locationId: harness.LocationId
            );

            var drain = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                }
            );
            Assert.True(drain.Succeeded);
            Assert.Equal(45, Assert.Single(drain.Channels).Refunded);

            var restore = await harness.Ledger.RestoreUnusedTopupAsync(
                new CreditLedgerRestoreTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                }
            );
            Assert.True(restore.Succeeded);
            var channel = Assert.Single(restore.Channels);
            Assert.Equal(15, channel.Consumed);
            Assert.Equal(0, channel.Refunded);

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var email = Channel(snapshot, CreditChannels.Email);
            Assert.Equal(45, email.Remaining);

            var replay = await harness.Ledger.RestoreUnusedTopupAsync(
                new CreditLedgerRestoreTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                }
            );
            Assert.True(replay.Succeeded);
            Assert.Equal(0, replay.Channels.Sum(row => row.Refunded));
        }

        [Fact]
        public async Task DrainUnusedTopup_NegativeRemainingRefused()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-negative";
            var allocationId = await MintTopupAsync(
                harness,
                CreditChannels.Email,
                10,
                paymentRef
            );
            await InsertConsumptionAsync(
                harness,
                allocationId,
                quantity: 10,
                locationId: harness.LocationId
            );
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Refund,
                    Quantity = 5,
                    AllocationId = allocationId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                    CreatedAtUtc = _now,
                }
            );
            await harness.Context.SaveChangesAsync();

            var result = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.Dispute,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("negative_remaining_refused", result.Code);
        }

        [Fact]
        public async Task DrainUnusedTopup_WritesTopupRefundedBillingActivityPerChannel()
        {
            var harness = await SeedAsync();
            const string paymentRef = "pay-topup-activity";
            await MintTopupAsync(harness, CreditChannels.Email, 25, paymentRef);
            await MintTopupAsync(harness, CreditChannels.Sms, 10, paymentRef);

            var result = await harness.Ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = CorrectionSources.PaymentRefund,
                }
            );

            Assert.True(result.Succeeded);
            var activity = await harness.Context.RestaurantBillingActivities
                .Where(row => row.RestaurantId == harness.RestaurantId)
                .ToListAsync();
            Assert.Equal(2, activity.Count);
            Assert.All(
                activity,
                row => Assert.Equal(BillingActivityKinds.TopupRefunded, row.Kind)
            );
            Assert.Contains(activity, row => row.Channel == CreditChannels.Email && row.Qty == 25);
            Assert.Contains(activity, row => row.Channel == CreditChannels.Sms && row.Qty == 10);
        }

        [Fact]
        public async Task ReserveAsync_BindsIncludedThenPilotThenEarliestTopUp()
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
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.PilotAllocation,
                10,
                createdAtUtc: _now.AddDays(-2),
                expiresAtUtc: null
            );
            await InsertGrantAsync(
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

            var result = await harness.Ledger.ReserveAsync(
                new CreditLedgerReserveRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 25,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(result.Succeeded);
            Assert.False(string.IsNullOrWhiteSpace(result.ReservationRef));
            Assert.Equal(3, result.Inserted.Count);
            Assert.All(
                result.Inserted,
                row => Assert.Equal(CreditLedgerEntryTypes.Reservation, row.EntryType)
            );
            Assert.Equal(includedId, result.Inserted[0].AllocationId);
            Assert.Equal(10, result.Inserted[0].Quantity);
            Assert.Equal(earlyTopUpId, result.Inserted[2].AllocationId);
            Assert.Equal(5, result.Inserted[2].Quantity);

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            var email = Channel(snapshot, CreditChannels.Email);
            Assert.Equal(15, email.Remaining);
            Assert.Equal(25, email.Held);
        }

        [Fact]
        public async Task SettleAsync_PartialSettleWalksBindOrder()
        {
            var harness = await SeedAsync();
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.IncludedAllocation,
                300,
                createdAtUtc: _now.AddDays(-3),
                expiresAtUtc: _now.AddDays(10)
            );
            await InsertGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditLedgerEntryTypes.TopupAllocation,
                700,
                createdAtUtc: _now.AddDays(-1),
                expiresAtUtc: _now.AddMonths(12)
            );

            var reserve = await harness.Ledger.ReserveAsync(
                new CreditLedgerReserveRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 1000,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(reserve.Succeeded);

            var settle = await harness.Ledger.SettleAsync(
                new CreditLedgerSettleRequest
                {
                    RestaurantId = harness.RestaurantId,
                    ReservationRef = reserve.ReservationRef!,
                    Channel = CreditChannels.Email,
                    AcceptedUnits = 800,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(settle.Succeeded);
            Assert.Equal(800, settle.SettledUnits);
            Assert.Equal(2, settle.Inserted.Count);
            Assert.Equal(300, settle.Inserted[0].Quantity);
            Assert.Equal(500, settle.Inserted[1].Quantity);
        }

        [Fact]
        public async Task SettleAsync_OnClosedRef_Fails()
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

            var reserve = await harness.Ledger.ReserveAsync(
                new CreditLedgerReserveRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 10,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(reserve.Succeeded);

            var settle = await harness.Ledger.SettleAsync(
                new CreditLedgerSettleRequest
                {
                    RestaurantId = harness.RestaurantId,
                    ReservationRef = reserve.ReservationRef!,
                    Channel = CreditChannels.Email,
                    AcceptedUnits = 10,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(settle.Succeeded);

            var release = await harness.Ledger.ReleaseAsync(
                new CreditLedgerReleaseRequest
                {
                    RestaurantId = harness.RestaurantId,
                    ReservationRef = reserve.ReservationRef!,
                    Channel = CreditChannels.Email,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(release.Succeeded);

            var closedSettle = await harness.Ledger.SettleAsync(
                new CreditLedgerSettleRequest
                {
                    RestaurantId = harness.RestaurantId,
                    ReservationRef = reserve.ReservationRef!,
                    Channel = CreditChannels.Email,
                    AcceptedUnits = 1,
                    LocationId = harness.LocationId,
                }
            );

            Assert.False(closedSettle.Succeeded);
            Assert.Equal("reservation_closed", closedSettle.Code);
        }

        [Fact]
        public async Task ReleaseAsync_OnClosedRef_IsOk()
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

            var reserve = await harness.Ledger.ReserveAsync(
                new CreditLedgerReserveRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    Units = 10,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(reserve.Succeeded);

            var release = await harness.Ledger.ReleaseAsync(
                new CreditLedgerReleaseRequest
                {
                    RestaurantId = harness.RestaurantId,
                    ReservationRef = reserve.ReservationRef!,
                    Channel = CreditChannels.Email,
                    LocationId = harness.LocationId,
                }
            );
            Assert.True(release.Succeeded);

            var again = await harness.Ledger.ReleaseAsync(
                new CreditLedgerReleaseRequest
                {
                    RestaurantId = harness.RestaurantId,
                    ReservationRef = reserve.ReservationRef!,
                    Channel = CreditChannels.Email,
                    LocationId = harness.LocationId,
                }
            );

            Assert.True(again.Succeeded);
            Assert.Empty(again.Inserted);
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
                new CreditLedgerService(context, _clock, new StubPricebookCatalog()),
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

        private async Task<Guid> MintTopupAsync(
            Harness harness,
            string channel,
            int quantity,
            string sourcePaymentRef
        )
        {
            var result = await harness.Ledger.MintTopupAllocationAsync(
                new CreditLedgerMintTopupRequest
                {
                    RestaurantId = harness.RestaurantId,
                    Channel = channel,
                    Quantity = quantity,
                    SourcePaymentRef = sourcePaymentRef,
                }
            );
            Assert.True(result.Succeeded);
            return result.AllocationId!.Value;
        }

        private static async Task InsertConsumptionAsync(
            Harness harness,
            Guid allocationId,
            int quantity,
            int locationId
        )
        {
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Consumption,
                    Quantity = quantity,
                    AllocationId = allocationId,
                    LocationId = locationId,
                    CreatedAtUtc = harness.Context.CreditLedgerEntries
                        .Where(row => row.Id == allocationId)
                        .Select(row => row.CreatedAtUtc)
                        .Single(),
                }
            );
            await harness.Context.SaveChangesAsync();
        }

        private static async Task InsertReservationAsync(
            Harness harness,
            Guid allocationId,
            int quantity,
            int locationId,
            string? reservationRef = null
        )
        {
            harness.Context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = harness.RestaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.Reservation,
                    Quantity = quantity,
                    AllocationId = allocationId,
                    ReservationRef = reservationRef ?? Guid.NewGuid().ToString("D"),
                    LocationId = locationId,
                    CreatedAtUtc = harness.Context.CreditLedgerEntries
                        .Where(row => row.Id == allocationId)
                        .Select(row => row.CreatedAtUtc)
                        .Single(),
                }
            );
            await harness.Context.SaveChangesAsync();
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

        private sealed class StubPricebookCatalog : IPricebookCatalog
        {
            public string CurrentPricebookId => PricebookId;

            public PricebookSnapshot GetRequired(string pricebookId) =>
                throw new NotImplementedException();

            public string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle) =>
                throw new NotImplementedException();

            public string FormatIncludedCreditsLabel(PricebookPlan plan, string channel) =>
                throw new NotImplementedException();

            public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available) =>
                throw new NotImplementedException();
        }
    }
}
