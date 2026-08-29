using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class BillingActivityServiceTests
    {
        [Fact]
        public async Task GetActivityAsync_ReturnsNewestFirst()
        {
            await using var harness = await SeedAsync();
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
            await using var harness = await SeedAsync();
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

        private static async Task<Harness> SeedAsync()
        {
            var context = BillingActivityTestDb.Create();
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

            var service = new BillingCreditsService(
                context,
                new StubPricebookCatalog(),
                new StubCreditBalanceSnapshot(),
                new StubBillingAccountLifecycle(),
                new PlanChangeService(
                    context,
                    new StubPricebookCatalog(),
                    TimeProvider.System
                )
            );
            return new Harness(context, service, restaurant.Id);
        }

        private sealed class Harness : IAsyncDisposable
        {
            public Harness(
                ApplicationDbContext context,
                BillingCreditsService service,
                int restaurantId
            )
            {
                Context = context;
                Service = service;
                RestaurantId = restaurantId;
            }

            public ApplicationDbContext Context { get; }

            public BillingCreditsService Service { get; }

            public int RestaurantId { get; }

            public ValueTask DisposeAsync() => Context.DisposeAsync();
        }

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

        private sealed class StubBillingAccountLifecycle : IBillingAccountLifecycle
        {
            public Task TickAsync(
                int restaurantId,
                DateTime now,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task<BillingLifecycleCommandResult> StartDunningEpisodeAsync(
                int restaurantId,
                DateTime now,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(BillingLifecycleCommandResult.NoOp());

            public Task RecoverDunningAsync(
                int restaurantId,
                DateTime now,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task ActivatePaidPlanAsync(
                int restaurantId,
                DateTime now,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task<BillingLifecycleCommandResult> ExtendPilotActivationAsync(
                int restaurantId,
                DateTime newPeriodEnd,
                DateTime now,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(BillingLifecycleCommandResult.NoOp());

            public Task SetChargebackRestrictionAsync(
                int restaurantId,
                bool restricted,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
