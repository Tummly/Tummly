using Microsoft.EntityFrameworkCore;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_SuccessfulLiveAnswer_ConsumesOneAiCredit()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 901, "Camden");
            var restaurantId = await RestaurantIdForLocationAsync(locationId);

            await _service.SendTurnAsync(
                ownerUserId: 901,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var consumption = await _context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Channel == CreditChannels.Ai
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
                .SingleAsync();
            Assert.Equal(1, consumption.Quantity);
            Assert.Equal(locationId, consumption.LocationId);

            var snapshot = await _creditSnapshot.GetAccountAsync(restaurantId);
            var ai = Assert.Single(snapshot!.Channels, row => row.Channel == CreditChannels.Ai);
            Assert.Equal(99, ai.Remaining);
        }

        [Fact]
        public async Task SendTurn_ProviderFailure_BurnsZero()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 902, "Camden");
            var restaurantId = await RestaurantIdForLocationAsync(locationId);
            _fake.Fail();

            await _service.SendTurnAsync(
                ownerUserId: 902,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            Assert.False(
                await _context.CreditLedgerEntries.AnyAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
            );
        }

        [Fact]
        public async Task SendTurn_RemainingZero_DoesNotCallLiveAnswer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 903, "Camden");
            var restaurantId = await RestaurantIdForLocationAsync(locationId);
            var consumed = await _context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Channel == CreditChannels.Ai
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
                .SumAsync(row => row.Quantity);
            var granted = await _context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Channel == CreditChannels.Ai
                    && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                )
                .SumAsync(row => row.Quantity);
            _context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Ai,
                    EntryType = CreditLedgerEntryTypes.Consumption,
                    Quantity = granted - consumed,
                    AllocationId = (
                        await _context.CreditLedgerEntries.FirstAsync(row =>
                            row.RestaurantId == restaurantId
                            && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                        )
                    ).Id,
                    LocationId = locationId,
                    CreatedAtUtc = _now,
                }
            );
            await _context.SaveChangesAsync();
            _fake.ResetToCannedStub();
            var callsBefore = _fake.CompleteCount;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 903,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var denied = Assert.IsType<AssistantTurnOutcome.CreditSpendDenied>(outcome);
            Assert.Equal("channel_hard_stopped", denied.Code);
            Assert.Equal(0, denied.Remaining);
            Assert.Equal(1, denied.Requested);
            Assert.Equal(callsBefore, _fake.CompleteCount);
        }

        [Fact]
        public async Task SendTurn_ConsumeFailAfterProviderSuccess_WithholdsAssistantPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 904, "Camden");
            var failingBilling = new AssistantAiBillingService(
                _context,
                new FailingCreditLedger(),
                _creditSnapshot,
                _clock
            );
            var service = CreateConversationService(aiBilling: failingBilling);

            var outcome = await service.SendTurnAsync(
                ownerUserId: 904,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var denied = Assert.IsType<AssistantTurnOutcome.CreditSpendDenied>(outcome);
            Assert.Equal("insufficient_credits", denied.Code);
            Assert.Equal(1, await _context.AssistantMessages.CountAsync());
            Assert.Equal(
                AssistantMessageRole.User,
                (await _context.AssistantMessages.SingleAsync()).Role
            );
        }

        [Fact]
        public async Task SendTurn_SameIdempotencyKey_ReturnsCachedOutcomeWithoutExtraBurn()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 905, "Camden");
            var restaurantId = await RestaurantIdForLocationAsync(locationId);
            const string key = "11111111-1111-1111-1111-111111111111";

            var first = await _service.SendTurnAsync(
                ownerUserId: 905,
                FirstSendRequest(locationId, "Summarise recent feedback"),
                idempotencyKey: key
            );
            var okFirst = Assert.IsType<AssistantTurnOutcome.Ok>(first);

            var second = await _service.SendTurnAsync(
                ownerUserId: 905,
                FirstSendRequest(locationId, "Summarise recent feedback"),
                idempotencyKey: key
            );
            var okSecond = Assert.IsType<AssistantTurnOutcome.Ok>(second);
            Assert.Equal(okFirst.Conversation.Id, okSecond.Conversation.Id);
            Assert.Equal(
                okFirst.Conversation.Messages.Count,
                okSecond.Conversation.Messages.Count
            );

            var burns = await _context.CreditLedgerEntries.CountAsync(row =>
                row.RestaurantId == restaurantId
                && row.EntryType == CreditLedgerEntryTypes.Consumption
            );
            Assert.Equal(1, burns);
        }

        private sealed class FailingCreditLedger : ICreditLedger
        {
            public Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
                CreditLedgerConsumeRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("insufficient_credits"));

            public Task<CreditLedgerWriteResult> ReserveAsync(
                CreditLedgerReserveRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> SettleAsync(
                CreditLedgerSettleRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> ReleaseAsync(
                CreditLedgerReleaseRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
                StaffManualAdjustRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> StaffReverseAsync(
                StaffReverseRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
                CreditLedgerMintTopupRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerMintTopupResult.Fail("not_implemented"));

            public Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
                CreditLedgerDrainTopupRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerDrainTopupResult.Fail("not_implemented"));

            public Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
                CreditLedgerRestoreTopupRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerRestoreTopupResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> ReleaseHeldAsync(
                CreditLedgerReleaseHeldRequest request,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));
        }
    }
}
