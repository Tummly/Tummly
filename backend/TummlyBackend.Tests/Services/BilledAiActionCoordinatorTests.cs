using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class BilledAiActionCoordinatorTests : IDisposable
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly DateTime _now = new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        private readonly TimeProvider _clock;

        public BilledAiActionCoordinatorTests()
        {
            _clock = new FixedTimeProvider(_now);
        }

        [Fact]
        public async Task ExecuteAsync_ConsumeFailWithholdsDraftBody()
        {
            var harness = await SeedAsync(aiCredits: 1);
            var coordinator = new BilledAiActionCoordinator(
                harness.Context,
                harness.Snapshot,
                new RejectingCreditLedger(),
                _clock
            );

            var result = await coordinator.ExecuteAsync(
                new BilledAiActionRequest
                {
                    RestaurantId = harness.RestaurantId,
                    LocationId = harness.LocationId,
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    PackKey = BilledAiPackKeys.CampaignAiDraftCompleted,
                },
                _ => Task.FromResult<BilledAiGenerationResult>(
                    new BilledAiGenerationResult.Ok(
                        new BilledAiDraftPayload
                        {
                            Body = "Secret draft",
                            Subject = "Subject",
                            Channel = "email",
                        }
                    )
                )
            );

            var consumeFailed = Assert.IsType<BilledAiActionResult.ConsumeFailed>(
                result
            );
            Assert.Equal("insufficient_credits", consumeFailed.Code);
            Assert.DoesNotContain(
                "Secret draft",
                JsonSerializer.Serialize(result)
            );
            Assert.Empty(
                await harness.Context.AiActionIdempotencyRecords.ToListAsync()
            );
            Assert.Empty(
                await harness.Context.CreditLedgerEntries
                    .Where(row =>
                        row.EntryType == CreditLedgerEntryTypes.Consumption
                    )
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task ExecuteAsync_SameIdempotencyKeyBurnsOnce()
        {
            var harness = await SeedAsync(aiCredits: 2);
            var coordinator = new BilledAiActionCoordinator(
                harness.Context,
                harness.Snapshot,
                harness.Ledger,
                _clock
            );
            var key = Guid.NewGuid().ToString("D");
            var request = new BilledAiActionRequest
            {
                RestaurantId = harness.RestaurantId,
                LocationId = harness.LocationId,
                IdempotencyKey = key,
                PackKey = BilledAiPackKeys.RecoveryAiDraftCompleted,
            };

            Task<BilledAiGenerationResult> GenerateAsync(CancellationToken _)
                => Task.FromResult<BilledAiGenerationResult>(
                    new BilledAiGenerationResult.Ok(
                        new BilledAiDraftPayload
                        {
                            Body = "Draft body",
                            Subject = "Draft subject",
                            Channel = "email",
                        }
                    )
                );

            var first = await coordinator.ExecuteAsync(request, GenerateAsync);
            var second = await coordinator.ExecuteAsync(request, GenerateAsync);

            Assert.IsType<BilledAiActionResult.Succeeded>(first);
            Assert.IsType<BilledAiActionResult.Cached>(second);

            var consumptions = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.Consumption
                    && row.Channel == CreditChannels.Ai
                )
                .ToListAsync();
            Assert.Single(consumptions);
            Assert.Equal(1, consumptions[0].Quantity);
        }

        private async Task<Harness> SeedAsync(int aiCredits)
        {
            var context = CreateContext();
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Billed AI Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Billed AI Venue",
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

            if (aiCredits > 0)
            {
                context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = restaurant.Id,
                        Channel = CreditChannels.Ai,
                        EntryType = CreditLedgerEntryTypes.PilotAllocation,
                        Quantity = aiCredits,
                        PricebookVersion = PricebookId,
                        CreatedAtUtc = _now.AddDays(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

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

        public void Dispose()
        {
        }

        private sealed record Harness(
            ApplicationDbContext Context,
            CreditLedgerService Ledger,
            CreditBalanceSnapshotService Snapshot,
            int RestaurantId,
            int LocationId
        );

        private sealed class RejectingCreditLedger : ICreditLedger
        {
            public Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
                CreditLedgerConsumeRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    CreditLedgerWriteResult.Fail("insufficient_credits")
                );
            }

            public Task<CreditLedgerWriteResult> ReserveAsync(
                CreditLedgerReserveRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerWriteResult> SettleAsync(
                CreditLedgerSettleRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerWriteResult> ReleaseAsync(
                CreditLedgerReleaseRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
                StaffManualAdjustRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerWriteResult> StaffReverseAsync(
                StaffReverseRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
                CreditLedgerMintTopupRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
                CreditLedgerDrainTopupRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
                CreditLedgerRestoreTopupRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerWriteResult> ReleaseHeldAsync(
                CreditLedgerReleaseHeldRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();
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

        private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
                => new(utcNow, TimeSpan.Zero);
        }
    }
}
