using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class RevolutWebhookServiceTests
    {
        [Fact]
        public async Task ClaimAndApply_RollsBackTogether_WhenApplierThrows()
        {
            await using var context = CreateContext();
            var merchant = new FixedOrderMerchant(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_rollback",
                    State: "completed",
                    BillingReason: "setup_intent",
                    RawBody: """{"id":"ord_rollback","state":"completed"}"""
                )
            );
            var applier = new ThrowingApplier(context);
            var service = CreateService(context, merchant, applier);
            var body = """{"event":"ORDER_COMPLETED","order_id":"ord_rollback"}""";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.HandleAsync(body, signature, timestamp)
            );

            Assert.True(applier.Called);
            Assert.True(
                applier.ClaimVisibleDuringApply,
                "Claim must be inserted before apply side effects."
            );
            Assert.Equal(0, await context.RevolutWebhookEventClaims.CountAsync());
        }

        [Fact]
        public async Task OrderCompleted_Completed_ClaimsApplied_AndCallsApplier()
        {
            await using var context = CreateContext();
            var merchant = new FixedOrderMerchant(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_ok",
                    State: "completed",
                    BillingReason: "setup_intent",
                    RawBody: """{"id":"ord_ok","state":"completed"}"""
                )
            );
            var applier = new RecordingApplier();
            var service = CreateService(context, merchant, applier);
            var body = """{"event":"ORDER_COMPLETED","order_id":"ord_ok"}""";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            var result = await service.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, result.Status);
            var claim = await context.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(RevolutWebhookClaimDispositions.Applied, claim.Disposition);
            Assert.NotNull(applier.Last);
            Assert.Equal("ord_ok", applier.Last!.OrderId);
            Assert.Equal("setup_intent", applier.Last.BillingReason);
        }

        [Theory]
        [InlineData("SUBSCRIPTION_CANCELLED", "sub_cancel_sync")]
        [InlineData("SUBSCRIPTION_FINISHED", "sub_finished_sync")]
        public async Task SubscriptionCancelOrFinished_SyncOnly_DoesNotRecoverDunning(
            string eventName,
            string subscriptionId
        )
        {
            await using var context = CreateContext();
            var restaurant = await SeedRestaurantWithOpenDunningAsync(
                context,
                subscriptionId
            );
            var lifecycle = new RecordingLifecycle();
            var service = CreateService(
                context,
                new FixedOrderMerchant(
                    new RevolutOrderRetrieveResult(Succeeded: false)
                ),
                new RecordingApplier(),
                lifecycle
            );
            var body =
                $"{{\"event\":\"{eventName}\",\"subscription_id\":\"{subscriptionId}\"}}";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            var result = await service.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, result.Status);
            Assert.Equal(0, lifecycle.RecoverCallCount);
            var account = await context.BillingAccounts.AsNoTracking().SingleAsync(row =>
                row.RestaurantId == restaurant.Id
            );
            Assert.NotNull(account.DunningEpisodeStartedAt);
            Assert.Equal(
                1,
                await context.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == eventName && row.ObjectId == subscriptionId
                )
            );
        }

        [Fact]
        public async Task DisputeActionRequired_ClaimIsIdempotent_OnReplay()
        {
            await using var context = CreateContext();
            var restaurant = await SeedRestaurantWithOpenDunningAsync(
                context,
                "sub_disp_idem"
            );
            const string paymentOrderId = "ord_disp_idem";
            const string disputeId = "disp_idem";
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.TopupAllocation,
                    Quantity = 40,
                    AllocationId = Guid.NewGuid(),
                    SourcePaymentRef = paymentOrderId,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var lifecycle = new RecordingLifecycle();
            var ledger = new RecordingLedger();
            var merchant = new FixedDisputeMerchant(
                new RevolutDisputeRetrieveResult(
                    Succeeded: true,
                    Id: disputeId,
                    PaymentOrderId: paymentOrderId
                )
            );
            var service = CreateService(
                context,
                merchant,
                new RecordingApplier(),
                lifecycle,
                ledger
            );
            var body =
                $$"""{"event":"DISPUTE_ACTION_REQUIRED","dispute_id":"{{disputeId}}"}""";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            var first = await service.HandleAsync(body, signature, timestamp);
            var second = await service.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, first.Status);
            Assert.Equal(RevolutWebhookHandleStatus.Replay, second.Status);
            Assert.Equal(1, lifecycle.ChargebackOpenCount);
            Assert.Equal(1, ledger.DrainCallCount);
            Assert.Equal(
                1,
                await context.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "DISPUTE_ACTION_REQUIRED"
                    && row.ObjectId == disputeId
                )
            );
            Assert.Equal(1, merchant.GetDisputeCallCount);
        }

        [Fact]
        public void ChallengeReason_UsesRefundAlreadyIssued_WhenPaymentRefundDrainExists()
        {
            Assert.Equal(
                RevolutDisputeChallengeReasons.RefundAlreadyIssued,
                RevolutDisputeChallengeReasons.ForSupportRefundAlreadyCompleted()
            );
        }

        private static RevolutWebhookService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IRevolutOrderCompletedApplier applier,
            IBillingAccountLifecycle? lifecycle = null,
            ICreditLedger? ledger = null
        )
        {
            var settings = Options.Create(
                new RevolutSettings
                {
                    WebhookSigningSecret = "whsec_service",
                    SecretKey = "sk_test",
                    ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                    ApiVersion = RevolutSettings.DefaultApiVersion,
                }
            );
            return new RevolutWebhookService(
                context,
                merchant,
                applier,
                lifecycle ?? new NoOpBillingAccountLifecycle(),
                TimeProvider.System,
                settings,
                dunningPay: null,
                ledger: ledger
            );
        }

        private static async Task<Restaurant> SeedRestaurantWithOpenDunningAsync(
            ApplicationDbContext context,
            string subscriptionId
        )
        {
            var now = DateTime.UtcNow;
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Webhook Sync Owner",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
                CreatedAt = now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Webhook Sync Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Starter,
                    BillingCycle = BillingCycles.Monthly,
                    BillingStatus = BillingStatuses.PastDue,
                    ContractedPricebookId = "pb",
                    StarterKitState = StarterKitStates.Unused,
                    DunningEpisodeStartedAt = now.AddDays(-1),
                    DunningOutstandingOrderId = "ord_open",
                }
            );
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Starter,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = subscriptionId,
                    SetupOrderId = "ord_setup_sync",
                    CheckoutUrl = "https://checkout.test",
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    IsOpen = false,
                    CreatedAtUtc = now,
                }
            );
            await context.SaveChangesAsync();
            return restaurant;
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class RecordingLifecycle : IBillingAccountLifecycle
        {
            public int RecoverCallCount { get; private set; }

            public int ChargebackOpenCount { get; private set; }

            public int ChargebackClearCount { get; private set; }

            public Task TickAsync(
                int restaurantId,
                DateTime now,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task<BillingLifecycleCommandResult> StartDunningEpisodeAsync(
                int restaurantId,
                DateTime now,
                string? outstandingOrderId = null,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(BillingLifecycleCommandResult.NoOp());

            public Task RecoverDunningAsync(
                int restaurantId,
                DateTime now,
                CancellationToken cancellationToken = default
            )
            {
                RecoverCallCount++;
                return Task.CompletedTask;
            }

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
            )
            {
                if (restricted)
                {
                    ChargebackOpenCount++;
                }
                else
                {
                    ChargebackClearCount++;
                }

                return Task.CompletedTask;
            }
        }

        private sealed class RecordingLedger : ICreditLedger
        {
            public int DrainCallCount { get; private set; }

            public int RestoreCallCount { get; private set; }

            public Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
                CreditLedgerConsumeRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> ReserveAsync(
                CreditLedgerReserveRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> SettleAsync(
                CreditLedgerSettleRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> ReleaseAsync(
                CreditLedgerReleaseRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
                StaffManualAdjustRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> StaffReverseAsync(
                StaffReverseRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
                CreditLedgerMintTopupRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerMintTopupResult.Fail("not_implemented"));

            public Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
                CreditLedgerDrainTopupRequest request,
                CancellationToken cancellationToken = default
            )
            {
                DrainCallCount++;
                return Task.FromResult(
                    CreditLedgerDrainTopupResult.Ok(
                        [
                            new TopupPaymentChannelSnapshot
                            {
                                Channel = request.SourcePaymentRef,
                                Refunded = 40,
                                Held = 0,
                                Consumed = 0,
                            },
                        ]
                    )
                );
            }

            public Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
                CreditLedgerRestoreTopupRequest request,
                CancellationToken cancellationToken = default
            )
            {
                RestoreCallCount++;
                return Task.FromResult(CreditLedgerRestoreTopupResult.Ok([]));
            }

            public Task<CreditLedgerWriteResult> ReleaseHeldAsync(
                CreditLedgerReleaseHeldRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Ok([]));
        }

        private sealed class FixedDisputeMerchant : IRevolutMerchantClient
        {
            private readonly RevolutDisputeRetrieveResult _dispute;

            public FixedDisputeMerchant(RevolutDisputeRetrieveResult dispute)
            {
                _dispute = dispute;
            }

            public int GetDisputeCallCount { get; private set; }

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

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(new RevolutOrderRetrieveResult(Succeeded: false));

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
                );

            public Task<RevolutDisputeRetrieveResult> GetDisputeAsync(
                string disputeId,
                CancellationToken cancellationToken = default
            )
            {
                GetDisputeCallCount++;
                return Task.FromResult(_dispute);
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

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
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

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
                );
        }

        private sealed class ThrowingApplier : IRevolutOrderCompletedApplier
        {
            private readonly ApplicationDbContext _context;

            public ThrowingApplier(ApplicationDbContext context)
            {
                _context = context;
            }

            public bool Called { get; private set; }

            public bool ClaimVisibleDuringApply { get; private set; }

            public Task ApplyAsync(
                RevolutOrderCompletedApplyRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Called = true;
                ClaimVisibleDuringApply = _context.RevolutWebhookEventClaims.Any(
                    row =>
                        row.Event == "ORDER_COMPLETED"
                        && row.ObjectId == request.OrderId
                );
                throw new InvalidOperationException("simulated apply failure");
            }
        }

        private sealed class RecordingApplier : IRevolutOrderCompletedApplier
        {
            public RevolutOrderCompletedApplyRequest? Last { get; private set; }

            public Task ApplyAsync(
                RevolutOrderCompletedApplyRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Last = request;
                return Task.CompletedTask;
            }
        }
    }
}
