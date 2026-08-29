using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class RevolutOrderCompletedApplierTests
    {
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly DateTime _now = new(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Apply_SetupIntent_ApplyAndMint_ShareAmbientClaimTransaction()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_txn", "sub_txn");
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = new RevolutOrderCompletedApplier(context, mint, clock);

            await using var ambient =
                await context.Database.BeginTransactionAsync();
            context.RevolutWebhookEventClaims.Add(
                new RevolutWebhookEventClaim
                {
                    Id = Guid.NewGuid(),
                    Event = "ORDER_COMPLETED",
                    ObjectId = "ord_txn",
                    Disposition = RevolutWebhookClaimDispositions.Applied,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_txn",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Equal(1, await context.RevolutWebhookEventClaims.CountAsync());
            Assert.Equal(
                3,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );

            await ambient.CommitAsync();

            var account = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Starter, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Equal(BillingCycles.Monthly, account.BillingCycle);
            Assert.Equal(
                3,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == pending.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task RedirectLand_PaySession_DoesNotActivate()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAccountAsync(context);
            var merchant = new RecordingLandMerchant();
            var service = new FirstPaidConversionPaySessionService(
                context,
                merchant,
                new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.test",
                    }
                ).Build()
            );

            var result = await service.StartAsync(
                account,
                owner,
                restaurantAccountType: "Single",
                locationId: 1,
                targetPlan: BillingSubscriptionPlans.Starter,
                targetCadenceApi: "monthly",
                idempotencyKey: Guid.NewGuid().ToString("D")
            );

            Assert.Equal("pay", result.Outcome);
            Assert.False(string.IsNullOrWhiteSpace(result.RedirectUrl));

            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Pilot, account.BillingStatus);
            Assert.Null(account.BillingCycle);
            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == account.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task Webhook_UnknownReason_DoesNotCallMintApplierPath()
        {
            await using var context = CreateContext();
            await SeedPilotPendingAsync(context, "ord_skip", "sub_skip");
            var merchant = new FixedOrderMerchant(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_skip",
                    State: "completed",
                    BillingReason: "weird_reason",
                    RawBody: """{"id":"ord_skip","state":"completed"}"""
                )
            );
            var applier = new CountingApplier();
            var service = new RevolutWebhookService(
                context,
                merchant,
                applier,
                Options.Create(
                    new RevolutSettings
                    {
                        WebhookSigningSecret = "whsec_service",
                        SecretKey = "sk_test",
                        ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                        ApiVersion = RevolutSettings.DefaultApiVersion,
                    }
                )
            );
            var body = """{"event":"ORDER_COMPLETED","order_id":"ord_skip"}""";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            var result = await service.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, result.Status);
            Assert.Equal(0, applier.Calls);
            var claim = await context.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(
                RevolutWebhookClaimDispositions.SkippedUnknownBillingReason,
                claim.Disposition
            );
        }

        private async Task<RevolutPendingPaySession> SeedPilotPendingAsync(
            ApplicationDbContext context,
            string setupOrderId,
            string subscriptionId
        )
        {
            var (account, _) = await SeedPilotAccountAsync(context);
            var pending = new RevolutPendingPaySession
            {
                Id = Guid.NewGuid(),
                RestaurantId = account.RestaurantId,
                TargetPlan = BillingSubscriptionPlans.Starter,
                TargetCadence = "monthly",
                RevolutSubscriptionId = subscriptionId,
                SetupOrderId = setupOrderId,
                CheckoutUrl = "https://checkout.revolut.test/pay",
                IdempotencyKey = Guid.NewGuid().ToString("D"),
                IsOpen = true,
                CreatedAtUtc = _now,
            };
            context.RevolutPendingPaySessions.Add(pending);
            await context.SaveChangesAsync();
            return pending;
        }

        private async Task<(BillingAccount Account, User Owner)> SeedPilotAccountAsync(
            ApplicationDbContext context
        )
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Apply Owner",
                Role = "Owner",
                CreatedAt = _now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Apply Pilot Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = _now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                _pricebook.CurrentPricebookId
            );
            account.BillingEmail = owner.Email;
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();
            return (account, owner);
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class CountingApplier : IRevolutOrderCompletedApplier
        {
            public int Calls { get; private set; }

            public Task ApplyAsync(
                RevolutOrderCompletedApplyRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls++;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedOrderMerchant : IRevolutMerchantClient
        {
            private readonly RevolutOrderRetrieveResult _order;

            public FixedOrderMerchant(RevolutOrderRetrieveResult order)
            {
                _order = order;
            }

            public void EnsureReadyForCreate(string? planVariationLookupKey = null)
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(_order);
        }

        private sealed class RecordingLandMerchant : IRevolutMerchantClient
        {
            public void EnsureReadyForCreate(string? planVariationLookupKey = null)
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    new RevolutListCustomersResult(Succeeded: true, FirstCustomerId: null)
                );
            }

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: "cus_land")
                );
            }

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: "sub_land",
                        SetupOrderId: "ord_land"
                    )
                );
            }

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    new RevolutOrderRetrieveResult(
                        Succeeded: true,
                        Id: orderId,
                        State: "pending",
                        CheckoutUrl: "https://checkout.revolut.test/land"
                    )
                );
            }
        }
    }

    file sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
