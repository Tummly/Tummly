using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class TopupConcurrentApplySqlTests
    {
        private const string TestPricebook = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly SqlServerFixture _fixture;

        public TopupConcurrentApplySqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task TwoConcurrentTopupApplies_OneAllocation()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var seeded = await SeedTopupIntentAsync();
            try
            {
                await using var factory = _fixture.CreateFactory();

                // Hold the Billing Account lock so both applies pass their
                // "already minted" check and then queue behind the mint lock.
                await using var blocker = _fixture.CreateContext();
                await using var blockerTx = await blocker.Database.BeginTransactionAsync();
                await blocker.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM [BillingAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [RestaurantId] = {seeded.RestaurantId}"
                );

                var applyA = ApplyTopupAsync(factory, seeded.OrderId);
                var applyB = ApplyTopupAsync(factory, seeded.OrderId);
                await WaitForBlockedRequestsAsync(blocker, expected: 2);

                // The winner commits its allocation row, then releases the lock.
                blocker.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = seeded.RestaurantId,
                        Channel = CreditChannels.Ai,
                        EntryType = CreditLedgerEntryTypes.TopupAllocation,
                        Quantity = 500,
                        PricebookVersion = TestPricebook,
                        SourcePaymentRef = seeded.OrderId,
                        ExpiresAtUtc = DateTime.UtcNow.AddMonths(12),
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await blocker.SaveChangesAsync();
                await blockerTx.CommitAsync();

                await Task.WhenAll(applyA, applyB);

                await using var verify = _fixture.CreateContext();
                Assert.Equal(
                    1,
                    await verify.CreditLedgerEntries.CountAsync(row =>
                        row.RestaurantId == seeded.RestaurantId
                        && row.SourcePaymentRef == seeded.OrderId
                        && row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                    )
                );
                // The losing apply must not leave threshold state behind.
                Assert.Equal(
                    0,
                    await verify.CreditWarningStates.CountAsync(row =>
                        row.RestaurantId == seeded.RestaurantId
                    )
                );
                Assert.False(
                    await verify.RevolutOrderIntents
                        .Where(row => row.OrderId == seeded.OrderId)
                        .Select(row => row.IsOpen)
                        .SingleAsync()
                );
            }
            finally
            {
                await CleanupAsync(seeded);
            }
        }

        private static async Task ApplyTopupAsync(
            Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory,
            string orderId
        )
        {
            using var scope = factory.Services.CreateScope();
            var applier = scope.ServiceProvider
                .GetRequiredService<IRevolutOrderCompletedApplier>();
            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: orderId,
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );
        }

        private static async Task WaitForBlockedRequestsAsync(
            ApplicationDbContext context,
            int expected
        )
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (true)
            {
                var blocked = await context.Database
                    .SqlQuery<int>(
                        $"SELECT COUNT(*) AS [Value] FROM sys.dm_exec_requests WHERE blocking_session_id <> 0"
                    )
                    .SingleAsync();
                if (blocked >= expected)
                {
                    return;
                }

                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException(
                        $"Expected {expected} blocked requests, saw {blocked}."
                    );
                }

                await Task.Delay(100);
            }
        }

        private async Task<(int RestaurantId, int UserId, string OrderId, int SequenceYear, bool SeededSequence)> SeedTopupIntentAsync()
        {
            await using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N");
            var orderId = $"ord_topup_race_{suffix}";

            var user = new User
            {
                FullName = "Owner",
                Email = $"topup-race-owner-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = now,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Topup Race {suffix}",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(restaurant.Id, TestPricebook)
            );
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    RestaurantId = restaurant.Id,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = $"k_{suffix}",
                    IsOpen = true,
                    NetAmountMinor = 1500,
                    VatAmountMinor = 300,
                    GrossAmountMinor = 1800,
                    Channel = CreditChannels.Ai,
                    Quantity = 500,
                    PackLookupKey = "tummly_ai_500_gbp_v3",
                    CreatedAtUtc = now,
                }
            );

            // The first invoice of a year inserts its number sequence. Two applies
            // racing on that insert is a separate issue, so seed the row here and
            // keep this test on the ledger mint.
            var sequenceYear = LondonDateFormat.UkCalendarYear(now);
            var seededSequence = !await context.TummlyDocumentSequences.AnyAsync(row =>
                row.DocumentPrefix == TummlyDocumentSequence.PrefixTm
                && row.Year == sequenceYear
            );
            if (seededSequence)
            {
                context.TummlyDocumentSequences.Add(
                    new TummlyDocumentSequence
                    {
                        DocumentPrefix = TummlyDocumentSequence.PrefixTm,
                        Year = sequenceYear,
                        NextNumber = 1,
                    }
                );
            }
            await context.SaveChangesAsync();

            return (restaurant.Id, user.Id, orderId, sequenceYear, seededSequence);
        }

        private async Task CleanupAsync((int RestaurantId, int UserId, string OrderId, int SequenceYear, bool SeededSequence) seeded)
        {
            await using var context = _fixture.CreateContext();
            await context.CreditLedgerEntries
                .Where(row => row.RestaurantId == seeded.RestaurantId)
                .ExecuteDeleteAsync();
            await context.CreditWarningStates
                .Where(row => row.RestaurantId == seeded.RestaurantId)
                .ExecuteDeleteAsync();
            if (seeded.SeededSequence)
            {
                await context.TummlyDocumentSequences
                    .Where(row =>
                        row.DocumentPrefix == TummlyDocumentSequence.PrefixTm
                        && row.Year == seeded.SequenceYear
                    )
                    .ExecuteDeleteAsync();
            }
            await context.TummlyVatInvoices
                .Where(row => row.RevolutOrderId == seeded.OrderId)
                .ExecuteDeleteAsync();
            await context.RevolutOrderIntents
                .Where(row => row.OrderId == seeded.OrderId)
                .ExecuteDeleteAsync();
            await context.Restaurants
                .Where(row => row.Id == seeded.RestaurantId)
                .ExecuteDeleteAsync();
            await context.Users
                .Where(row => row.Id == seeded.UserId)
                .ExecuteDeleteAsync();
        }
    }
}
