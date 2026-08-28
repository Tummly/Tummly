using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CreditLedgerPilotMintTests : IDisposable
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly DateTime _now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
        private readonly TimeProvider _clock;

        public CreditLedgerPilotMintTests()
        {
            _clock = new FixedTimeProvider(_now);
        }

        [Fact]
        public async Task MintPilotAtActivation_InsertsThreeChannelGrants_FromContractedPricebook()
        {
            var harness = await SeedAsync(TestPricebookPaths.LoadV3());

            var result = await harness.Ledger.MintPilotAtActivationAsync(
                harness.RestaurantId
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.Inserted.Count);
            await harness.Context.SaveChangesAsync();

            var rows = await harness.Context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == harness.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                )
                .ToListAsync();

            Assert.Equal(3, rows.Count);
            Assert.All(rows, row => Assert.Null(row.ExpiresAtUtc));
            Assert.All(rows, row => Assert.Equal(PricebookId, row.PricebookVersion));

            var email = rows.Single(row => row.Channel == CreditChannels.Email);
            var sms = rows.Single(row => row.Channel == CreditChannels.Sms);
            var ai = rows.Single(row => row.Channel == CreditChannels.Ai);
            Assert.Equal(500, email.Quantity);
            Assert.Equal(20, sms.Quantity);
            Assert.Equal(20, ai.Quantity);

            var snapshot = await harness.Snapshot.GetAccountAsync(harness.RestaurantId);
            Assert.NotNull(snapshot);
            Assert.Equal(500, Channel(snapshot, CreditChannels.Email).Remaining);
            Assert.Equal(20, Channel(snapshot, CreditChannels.Sms).Remaining);
            Assert.Equal(20, Channel(snapshot, CreditChannels.Ai).Remaining);
        }

        [Fact]
        public async Task MintPilotAtActivation_RejectsSecondMint()
        {
            var harness = await SeedAsync(TestPricebookPaths.LoadV3());

            var first = await harness.Ledger.MintPilotAtActivationAsync(
                harness.RestaurantId
            );
            Assert.True(first.Succeeded);
            await harness.Context.SaveChangesAsync();

            var second = await harness.Ledger.MintPilotAtActivationAsync(
                harness.RestaurantId
            );

            Assert.False(second.Succeeded);
            Assert.Equal("pilot_already_minted", second.Code);
        }

        [Fact]
        public async Task MintPilotAtActivation_SkipsChannelsWithQuantityZero()
        {
            var catalog = new StubPricebookCatalog(
                new PricebookChannelCredits
                {
                    Ai = 20,
                    Email = 500,
                    Sms = 0,
                }
            );
            var harness = await SeedAsync(catalog);

            var result = await harness.Ledger.MintPilotAtActivationAsync(
                harness.RestaurantId
            );

            Assert.True(result.Succeeded);
            Assert.Equal(2, result.Inserted.Count);
            await harness.Context.SaveChangesAsync();

            var channels = await harness.Context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == harness.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                )
                .Select(row => row.Channel)
                .ToListAsync();

            Assert.Contains(CreditChannels.Email, channels);
            Assert.Contains(CreditChannels.Ai, channels);
            Assert.DoesNotContain(CreditChannels.Sms, channels);
        }

        private async Task<Harness> SeedAsync(IPricebookCatalog catalog)
        {
            var context = CreateContext();
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Pilot Mint Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Pilot Mint Venue",
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
            await context.SaveChangesAsync();

            return new Harness(
                context,
                new CreditLedgerService(context, _clock, catalog),
                new CreditBalanceSnapshotService(context, _clock),
                restaurant.Id
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

        public void Dispose()
        {
        }

        private sealed record Harness(
            ApplicationDbContext Context,
            CreditLedgerService Ledger,
            CreditBalanceSnapshotService Snapshot,
            int RestaurantId
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
