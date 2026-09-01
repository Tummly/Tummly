using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class FirstPaidConversionPaySessionServiceTests
    {
        [Fact]
        public async Task StartAsync_ListsThenCreatesCustomer_OnlyOnThisPath()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            var merchant = new RecordingMerchant
            {
                ListResult = new RevolutListCustomersResult(Succeeded: true),
                CreateCustomerResult = new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: "cust_new"
                ),
                CreateSubscriptionResult = new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: "sub_1",
                    SetupOrderId: "ord_setup_1"
                ),
                GetOrderResult = new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_setup_1",
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/payment-link/abc"
                ),
            };
            var service = CreateService(context, merchant);

            var result = await service.StartAsync(
                account,
                owner,
                restaurantAccountType: "Single",
                locationId: 42,
                targetPlan: "Starter",
                targetCadenceApi: "monthly",
                idempotencyKey: "key-1"
            );

            Assert.Equal("pay", result.Outcome);
            Assert.Equal(
                "https://checkout.revolut.com/payment-link/abc",
                result.RedirectUrl
            );
            Assert.Equal(1, merchant.ListCallCount);
            Assert.Equal(1, merchant.CreateCustomerCallCount);
            Assert.Equal(1, merchant.CreateSubscriptionCallCount);
            Assert.Equal("cust_new", account.RevolutCustomerId);
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Pilot, account.BillingStatus);
        }

        [Fact]
        public async Task StartAsync_ReusesCustomer_WhenAlreadyStamped()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            account.RevolutCustomerId = "cust_existing";
            await context.SaveChangesAsync();
            var merchant = new RecordingMerchant
            {
                CreateSubscriptionResult = new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: "sub_1",
                    SetupOrderId: "ord_setup_1"
                ),
                GetOrderResult = new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_setup_1",
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/payment-link/abc"
                ),
            };
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                owner,
                "Single",
                1,
                "Starter",
                "monthly",
                "key-1"
            );

            Assert.Equal(0, merchant.ListCallCount);
            Assert.Equal(0, merchant.CreateCustomerCallCount);
            Assert.Equal("cust_existing", account.RevolutCustomerId);
        }

        [Fact]
        public async Task StartAsync_CancelsPending_WhenSameTargetNotReusable()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            account.RevolutCustomerId = "cust_1";
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = account.RestaurantId,
                    TargetPlan = "Starter",
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_stale",
                    SetupOrderId = "ord_stale",
                    CheckoutUrl = "https://checkout.revolut.com/stale",
                    IdempotencyKey = "old-key",
                    IsOpen = true,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
            var merchant = new RecordingMerchant
            {
                CancelResult = new RevolutMerchantCreateResult(Succeeded: true),
                CreateSubscriptionResult = new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: "sub_new",
                    SetupOrderId: "ord_new"
                ),
                GetOrderResult = new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_stale",
                    State: "cancelled",
                    CheckoutUrl: null
                ),
            };
            var getCount = 0;
            merchant.GetOrderOverride = orderId =>
            {
                getCount++;
                if (getCount == 1)
                {
                    return new RevolutOrderRetrieveResult(
                        Succeeded: true,
                        Id: orderId,
                        State: "cancelled",
                        CheckoutUrl: null
                    );
                }

                return new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: orderId,
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/new"
                );
            };
            var service = CreateService(context, merchant);

            var result = await service.StartAsync(
                account,
                owner,
                "Single",
                1,
                "Starter",
                "monthly",
                "new-key"
            );

            Assert.Equal(1, merchant.CancelCallCount);
            Assert.Equal("sub_stale", merchant.LastCancelledSubscriptionId);
            Assert.Equal(1, merchant.CreateSubscriptionCallCount);
            Assert.Equal(
                "https://checkout.revolut.com/new",
                result.RedirectUrl
            );
        }

        [Fact]
        public async Task StartAsync_ReusesPending_SamePlanCadence()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            account.RevolutCustomerId = "cust_1";
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = account.RestaurantId,
                    TargetPlan = "Starter",
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_open",
                    SetupOrderId = "ord_open",
                    CheckoutUrl = "https://checkout.revolut.com/old",
                    IdempotencyKey = "old-key",
                    IsOpen = true,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
            var merchant = new RecordingMerchant
            {
                GetOrderResult = new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_open",
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/reusable"
                ),
            };
            var service = CreateService(context, merchant);

            var result = await service.StartAsync(
                account,
                owner,
                "Single",
                1,
                "Starter",
                "monthly",
                "new-key"
            );

            Assert.Equal(
                "https://checkout.revolut.com/reusable",
                result.RedirectUrl
            );
            Assert.Equal(0, merchant.CancelCallCount);
            Assert.Equal(0, merchant.CreateSubscriptionCallCount);
            Assert.Equal(1, merchant.GetOrderCallCount);
        }

        [Fact]
        public async Task StartAsync_CancelsPending_WhenTargetDiffers()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            account.RevolutCustomerId = "cust_1";
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = account.RestaurantId,
                    TargetPlan = "Starter",
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_old",
                    SetupOrderId = "ord_old",
                    CheckoutUrl = "https://checkout.revolut.com/old",
                    IdempotencyKey = "old-key",
                    IsOpen = true,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
            var merchant = new RecordingMerchant
            {
                CancelResult = new RevolutMerchantCreateResult(Succeeded: true),
                CreateSubscriptionResult = new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: "sub_new",
                    SetupOrderId: "ord_new"
                ),
                GetOrderResult = new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_new",
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/new"
                ),
            };
            var service = CreateService(context, merchant);

            var result = await service.StartAsync(
                account,
                owner,
                "Single",
                1,
                "Growth",
                "monthly",
                "new-key"
            );

            Assert.Equal(1, merchant.CancelCallCount);
            Assert.Equal("sub_old", merchant.LastCancelledSubscriptionId);
            Assert.Equal(1, merchant.CreateSubscriptionCallCount);
            Assert.Equal(
                "https://checkout.revolut.com/new",
                result.RedirectUrl
            );
            var open = await context.RevolutPendingPaySessions
                .Where(row => row.RestaurantId == account.RestaurantId && row.IsOpen)
                .ToListAsync();
            Assert.Single(open);
            Assert.Equal("Growth", open[0].TargetPlan);
        }

        [Fact]
        public async Task StartAsync_SameIdempotencyKey_DifferentTarget_Rejects()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            account.RevolutCustomerId = "cust_1";
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = account.RestaurantId,
                    TargetPlan = "Starter",
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_1",
                    SetupOrderId = "ord_1",
                    CheckoutUrl = "https://checkout.revolut.com/a",
                    IdempotencyKey = "same-key",
                    IsOpen = true,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
            var service = CreateService(context, new RecordingMerchant());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartAsync(
                    account,
                    owner,
                    "Single",
                    1,
                    "Growth",
                    "monthly",
                    "same-key"
                )
            );

            Assert.Equal("idempotency_target_mismatch", ex.Message);
        }

        [Fact]
        public async Task StartAsync_PrefersBillingEmail_OverOwner()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAsync(context);
            account.BillingEmail = "billing@venue.test";
            await context.SaveChangesAsync();
            var merchant = new RecordingMerchant
            {
                ListResult = new RevolutListCustomersResult(
                    Succeeded: true,
                    FirstCustomerId: "cust_listed"
                ),
                CreateSubscriptionResult = new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: "sub_1",
                    SetupOrderId: "ord_1"
                ),
                GetOrderResult = new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_1",
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/x"
                ),
            };
            var service = CreateService(context, merchant);

            await service.StartAsync(
                account,
                owner,
                "Single",
                1,
                "Starter",
                "monthly",
                "key-1"
            );

            Assert.Equal("billing@venue.test", merchant.LastListedEmail);
            Assert.Equal(0, merchant.CreateCustomerCallCount);
            Assert.Equal("cust_listed", account.RevolutCustomerId);
        }

        private static FirstPaidConversionPaySessionService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
                    }
                )
                .Build();
            return new FirstPaidConversionPaySessionService(
                context,
                merchant,
                config
            );
        }

        private static async Task<(BillingAccount Account, User Owner)> SeedPilotAsync(
            ApplicationDbContext context
        )
        {
            var owner = new User
            {
                FullName = "Owner",
                Email = "owner@venue.test",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Pay Session Cafe",
                OwnerUserId = owner.Id,
                AccountType = "Single",
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();
            return (account, owner);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class RecordingMerchant : IRevolutMerchantClient
        {
            public RevolutListCustomersResult ListResult { get; set; } =
                new(Succeeded: true);

            public RevolutMerchantCreateResult CreateCustomerResult { get; set; } =
                new(Succeeded: true, Id: "cust_x");

            public RevolutMerchantCreateResult CreateSubscriptionResult { get; set; } =
                new(Succeeded: true, Id: "sub_x", SetupOrderId: "ord_x");

            public RevolutMerchantCreateResult CancelResult { get; set; } =
                new(Succeeded: true);

            public RevolutOrderRetrieveResult GetOrderResult { get; set; } =
                new(
                    Succeeded: true,
                    Id: "ord_x",
                    State: "pending",
                    CheckoutUrl: "https://checkout.revolut.com/x"
                );

            public Func<string, RevolutOrderRetrieveResult>? GetOrderOverride { get; set; }

            public int ListCallCount { get; private set; }

            public int CreateCustomerCallCount { get; private set; }

            public int CreateSubscriptionCallCount { get; private set; }

            public int CancelCallCount { get; private set; }

            public int GetOrderCallCount { get; private set; }

            public string? LastListedEmail { get; private set; }

            public string? LastCancelledSubscriptionId { get; private set; }

            public void EnsureReadyForCreate(string? planVariationLookupKey = null)
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            )
            {
                ListCallCount++;
                LastListedEmail = email;
                return Task.FromResult(ListResult);
            }

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            )
            {
                CreateCustomerCallCount++;
                return Task.FromResult(CreateCustomerResult);
            }

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            )
            {
                CreateSubscriptionCallCount++;
                return Task.FromResult(CreateSubscriptionResult);
            }

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();


public Task<RevolutMerchantCreateResult> ScheduleSubscriptionCancelAtCycleEndAsync(

                string subscriptionId,

                CancellationToken cancellationToken = default

            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            )
            {
                CancelCallCount++;
                LastCancelledSubscriptionId = subscriptionId;
                return Task.FromResult(CancelResult);
            }

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            )
            {
                GetOrderCallCount++;
                if (GetOrderOverride != null)
                {
                    return Task.FromResult(GetOrderOverride(orderId));
                }

                return Task.FromResult(GetOrderResult);
            }

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
                );
        }
    }
}
