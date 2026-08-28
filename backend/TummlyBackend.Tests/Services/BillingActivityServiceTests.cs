using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class BillingActivityServiceTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        [Fact]
        public async Task GetActivityAsync_ReturnsNewestFirst()
        {
            var harness = await SeedAsync();
            var older = DateTime.UtcNow.AddHours(-2);
            var newer = DateTime.UtcNow.AddHours(-1);
            harness.Context.RestaurantBillingActivities.AddRange(
                new RestaurantBillingActivity
                {
                    RestaurantId = harness.RestaurantId,
                    Kind = BillingActivityKinds.InvoicePaid,
                    OccurredAtUtc = older,
                    InvoiceNo = "TM-2026-000001",
                },
                new RestaurantBillingActivity
                {
                    RestaurantId = harness.RestaurantId,
                    Kind = BillingActivityKinds.TopupPurchased,
                    OccurredAtUtc = newer,
                    ActorDisplayName = "Owner",
                    Channel = CreditChannels.Sms,
                    Qty = 100,
                }
            );
            await harness.Context.SaveChangesAsync();

            var result = await harness.Service.GetActivityAsync(
                harness.RestaurantId,
                skip: 0,
                take: 10
            );

            Assert.NotNull(result);
            Assert.Equal(2, result!.TotalCount);
            Assert.Equal(BillingActivityKinds.TopupPurchased, result.Items[0].Kind);
            Assert.Equal(BillingActivityKinds.InvoicePaid, result.Items[1].Kind);
        }

        [Fact]
        public async Task GetActivityAsync_PagesWithSkipAndTake()
        {
            var harness = await SeedAsync();
            for (var i = 0; i < 5; i++)
            {
                harness.Context.RestaurantBillingActivities.Add(
                    new RestaurantBillingActivity
                    {
                        RestaurantId = harness.RestaurantId,
                        Kind = BillingActivityKinds.SubscriptionRenewed,
                        OccurredAtUtc = DateTime.UtcNow.AddMinutes(-i),
                        Plan = "Growth",
                    }
                );
            }

            await harness.Context.SaveChangesAsync();

            var page = await harness.Service.GetActivityAsync(
                harness.RestaurantId,
                skip: 2,
                take: 2
            );

            Assert.NotNull(page);
            Assert.Equal(5, page!.TotalCount);
            Assert.Equal(2, page.Items.Count);
        }

        private async Task<Harness> SeedAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var context = new ApplicationDbContext(options);
            var owner = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid()}@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Billing Activity Test",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            await context.SaveChangesAsync();

            var pricebook = new StubPricebookCatalog();
            var creditBalance = new StubCreditBalanceSnapshot();
            var service = new BillingCreditsService(context, pricebook, creditBalance);
            return new Harness(context, service, restaurant.Id);
        }

        public void Dispose()
        {
        }

        private sealed record Harness(
            ApplicationDbContext Context,
            BillingCreditsService Service,
            int RestaurantId
        );

        private sealed class StubPricebookCatalog : IPricebookCatalog
        {
            public string CurrentPricebookId => "TUMMLY-UK-GBP-2026-08-V3";

            public PricebookSnapshot GetRequired(string pricebookId) =>
                throw new NotImplementedException();

            public string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle) =>
                throw new NotImplementedException();

            public string FormatIncludedCreditsLabel(PricebookPlan plan, string channel) =>
                throw new NotImplementedException();

            public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available) =>
                throw new NotImplementedException();
        }

        private sealed class StubCreditBalanceSnapshot : ICreditBalanceSnapshot
        {
            public Task<CreditBalanceAccountSnapshot?> GetAccountAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<CreditBalanceAccountSnapshot?>(null);
        }
    }
}
