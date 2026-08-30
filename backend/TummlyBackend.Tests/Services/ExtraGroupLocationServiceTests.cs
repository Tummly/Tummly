using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class ExtraGroupLocationServiceTests
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly IPricebookCatalog _pricebook =
            PricebookCatalog.LoadFromDirectory(PackDirectory());

        [Fact]
        public async Task SubmitAdd_CreatesOrderIntent_AndReturnsCheckoutUrl()
        {
            var harness = await SeedGroupAsync(paidExtra: 0);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = new ExtraGroupLocationService(
                harness.Context,
                _pricebook,
                new AlwaysReadyRevolutMerchantCreateGate(),
                merchant,
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://tummly.example",
                        }
                    )
                    .Build(),
                TimeProvider.System
            );

            var result = await service.SubmitAsync(
                harness.OwnerUserId,
                harness.RestaurantId,
                "add"
            );

            Assert.NotNull(result);
            Assert.Equal("pay", result!.Outcome);
            Assert.Equal(FakeFirstPaidRevolutMerchantClient.CheckoutUrl, result.RedirectUrl);
            Assert.Equal(1, merchant.CreateOrderCallCount);
            Assert.Equal(
                RevolutPlanVariationKeys.GroupLocationMonthly,
                merchant.LastCreateOrderRequest!.PlanVariationLookupKey
            );

            var intent = await harness.Context.RevolutOrderIntents.SingleAsync();
            Assert.Equal(RevolutOrderIntentPurposes.ExtraLocation, intent.Purpose);
            Assert.Equal("cust_extra_seed", merchant.LastCreateOrderRequest!.CustomerId);
            Assert.StartsWith("ord_pm_", intent.OrderId);
            Assert.Equal(1, intent.TargetPaidExtraLocationCount);
        }

        [Fact]
        public async Task SubmitRemove_DoesNotCreateOrder()
        {
            var harness = await SeedGroupAsync(paidExtra: 2, locationCount: 5);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = new ExtraGroupLocationService(
                harness.Context,
                _pricebook,
                new AlwaysReadyRevolutMerchantCreateGate(),
                merchant,
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://tummly.example",
                        }
                    )
                    .Build(),
                TimeProvider.System
            );

            var result = await service.SubmitAsync(
                harness.OwnerUserId,
                harness.RestaurantId,
                "remove"
            );

            Assert.NotNull(result);
            Assert.Equal("scheduled", result!.Outcome);
            Assert.Equal(0, merchant.CreateOrderCallCount);
        }

        [Fact]
        public async Task ApplyAddOnOrderCompleted_16Of31_GrantsExtraAddFloorQuantities()
        {
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            var harness = await SeedGroupAsync(paidExtra: 0);
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Ai,
                500,
                periodStart,
                periodEnd
            );
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Email,
                10_000,
                periodStart,
                periodEnd
            );
            await InsertIncludedGrantAsync(
                harness.Context,
                harness.RestaurantId,
                CreditChannels.Sms,
                350,
                periodStart,
                periodEnd
            );

            var result = await harness.Service.ApplyAddOnOrderCompletedAsync(
                harness.RestaurantId,
                now
            );

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.InsertedAllocationIds.Count);

            var account = await harness.Context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == harness.RestaurantId
            );
            Assert.Equal(1, account.PaidExtraLocationCount);
            Assert.Equal(_pricebook.CurrentPricebookId, account.ContractedPricebookId);
            Assert.False(account.HasScheduledChange);

            var migrations = await harness.Context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == harness.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.PlanMigration
                )
                .ToListAsync();
            Assert.Equal(3, migrations.Count);
            Assert.Equal(
                51,
                Assert.Single(migrations, row => row.Channel == CreditChannels.Ai).Quantity
            );
            Assert.Equal(
                2_580,
                Assert
                    .Single(migrations, row => row.Channel == CreditChannels.Email)
                    .Quantity
            );
            Assert.Equal(
                51,
                Assert.Single(migrations, row => row.Channel == CreditChannels.Sms).Quantity
            );
            Assert.All(
                migrations,
                row =>
                {
                    Assert.Equal(periodStart, row.PeriodStartUtc);
                    Assert.Equal(periodEnd, row.ExpiresAtUtc);
                    Assert.Equal(_pricebook.CurrentPricebookId, row.PricebookVersion);
                }
            );
        }

        [Fact]
        public async Task ApplyScheduledRemove_DecrementsCount_WithoutDebitThisPeriod()
        {
            var harness = await SeedGroupAsync(paidExtra: 2);
            var account = await harness.Context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == harness.RestaurantId
            );
            account.HasScheduledChange = true;
            account.ScheduledTargetSubscriptionPlan = BillingSubscriptionPlans.Group;
            account.ScheduledTargetBillingCycle = BillingCycles.Monthly;
            account.ScheduledTargetExtraLocationCount = 1;
            account.ScheduledCancelPlan = false;
            await harness.Context.SaveChangesAsync();

            var beforeCount = await harness.Context.CreditLedgerEntries.CountAsync(row =>
                row.RestaurantId == harness.RestaurantId
            );

            var result = await harness.Service.ApplyScheduledRemoveAsync(
                harness.RestaurantId,
                DateTime.UtcNow
            );

            Assert.True(result.Succeeded);
            await harness.Context.Entry(account).ReloadAsync();
            Assert.Equal(1, account.PaidExtraLocationCount);
            Assert.False(account.HasScheduledChange);

            var afterCount = await harness.Context.CreditLedgerEntries.CountAsync(row =>
                row.RestaurantId == harness.RestaurantId
            );
            Assert.Equal(beforeCount, afterCount);
            Assert.Empty(
                await harness.Context.CreditLedgerEntries
                    .Where(row =>
                        row.RestaurantId == harness.RestaurantId
                        && row.EntryType == CreditLedgerEntryTypes.PlanMigration
                    )
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task SubmitRemove_SchedulesTargetExtraCount()
        {
            var harness = await SeedGroupAsync(paidExtra: 2, locationCount: 5);
            var result = await harness.Service.SubmitAsync(
                harness.OwnerUserId,
                harness.RestaurantId,
                "remove"
            );

            Assert.NotNull(result);
            Assert.Equal("scheduled", result!.Outcome);
            Assert.Contains("Removes 1 Additional Group Location", result.ScheduledChangeLine);

            var account = await harness.Context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == harness.RestaurantId
            );
            Assert.True(account.HasScheduledChange);
            Assert.Equal(1, account.ScheduledTargetExtraLocationCount);
            Assert.Equal(2, account.PaidExtraLocationCount);
        }

        [Fact]
        public void RemainingPeriodRatio_16Of31_MatchesLock()
        {
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            var ratio = PlanMigrationMath.RemainingPeriodRatio(periodStart, periodEnd, now);
            Assert.Equal(51, PlanMigrationMath.FloorGrant(100, ratio));
            Assert.Equal(2_580, PlanMigrationMath.FloorGrant(5_000, ratio));
        }

        private async Task<Harness> SeedGroupAsync(
            int paidExtra,
            int locationCount = 1
        )
        {
            var context = CreateContext();
            var now = DateTime.UtcNow;
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Extra Owner",
                Role = "Owner",
                CreatedAt = now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Paid Billing Venue Group Test",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                PricebookId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Group;
            account.BillingCycle = BillingCycles.Monthly;
            account.BillingStatus = BillingStatuses.Active;
            account.PaidExtraLocationCount = paidExtra;
            account.RevolutCustomerId = "cust_extra_seed";
            account.RenewalDateUtc = new DateTime(
                2026,
                9,
                15,
                0,
                0,
                0,
                DateTimeKind.Utc
            );
            context.BillingAccounts.Add(account);

            for (var i = 0; i < locationCount; i++)
            {
                context.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        RestaurantId = restaurant.Id,
                        LocationName = $"Loc {i + 1}",
                        Address = $"{i + 1} High Street",
                        CreatedAt = now,
                    }
                );
            }

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    Status = MembershipStatus.Active,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                }
            );
            await context.SaveChangesAsync();

            return new Harness(
                context,
                new ExtraGroupLocationService(
                    context,
                    _pricebook,
                    new AlwaysReadyRevolutMerchantCreateGate(),
                    new FakeFirstPaidRevolutMerchantClient(),
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                ["Frontend:BaseUrl"] = "https://tummly.example",
                            }
                        )
                        .Build(),
                    TimeProvider.System
                ),
                restaurant.Id,
                owner.Id
            );
        }

        private static async Task InsertIncludedGrantAsync(
            ApplicationDbContext context,
            int restaurantId,
            string channel,
            int quantity,
            DateTime periodStartUtc,
            DateTime expiresAtUtc
        )
        {
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = channel,
                    EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                    Quantity = quantity,
                    PricebookVersion = PricebookId,
                    PeriodStartUtc = periodStartUtc,
                    ExpiresAtUtc = expiresAtUtc,
                    CreatedAtUtc = periodStartUtc,
                }
            );
            await context.SaveChangesAsync();
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName + Guid.NewGuid())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private static string PackDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var pack = Path.Combine(
                    dir.FullName,
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                );
                if (Directory.Exists(pack))
                {
                    return pack;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("billing-pack-v3.0 not found.");
        }

        private sealed class Harness
        {
            public Harness(
                ApplicationDbContext context,
                ExtraGroupLocationService service,
                int restaurantId,
                int ownerUserId
            )
            {
                Context = context;
                Service = service;
                RestaurantId = restaurantId;
                OwnerUserId = ownerUserId;
            }

            public ApplicationDbContext Context { get; }

            public ExtraGroupLocationService Service { get; }

            public int RestaurantId { get; }

            public int OwnerUserId { get; }
        }
    }
}
