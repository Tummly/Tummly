using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class RevolutPaymentRefundHandlerTests
    {
        [Fact]
        public async Task RefundCompleted_DrainsAndMintsTcn_WebhookReplaySafe()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantWithTopupAsync(context);
            var ledger = new RecordingLedger();
            var vat = new RecordingVatInvoiceService();
            var refundHandler = new RevolutPaymentRefundCompletedHandler(
                context,
                ledger,
                vat,
                TimeProvider.System,
                NullLogger<RevolutPaymentRefundCompletedHandler>.Instance
            );
            var merchant = new FixedOrderMerchant(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_refund_1",
                    State: "completed",
                    OrderType: RevolutOrderTypes.Refund,
                    RelatedOrderId: seeded.PaymentOrderId,
                    AmountMinor: 1200,
                    RawBody: "{}"
                )
            );
            var service = new RevolutWebhookService(
                context,
                merchant,
                new RecordingApplier(),
                new NoOpBillingAccountLifecycle(),
                TimeProvider.System,
                Options.Create(
                    new RevolutSettings
                    {
                        WebhookSigningSecret = "whsec_service",
                        SecretKey = "sk_test",
                        ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                        ApiVersion = RevolutSettings.DefaultApiVersion,
                    }
                ),
                paymentRefundHandler: refundHandler
            );
            var body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_refund_1"}""";
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
            Assert.Equal(1, ledger.DrainCallCount);
            Assert.Equal(
                CorrectionSources.PaymentRefund,
                ledger.LastCorrectionSource
            );
            Assert.Equal(seeded.PaymentOrderId, ledger.LastSourcePaymentRef);
            Assert.Equal(1, vat.MintCreditNoteCallCount);
            Assert.Equal("ord_refund_1", vat.LastRefundOrderId);
            Assert.Contains(
                context.RestaurantBillingActivities,
                row =>
                    row.Kind == BillingActivityKinds.CreditNoteIssued
                    && row.CreditNoteNo == "TCN-2026-000001"
            );
        }

        [Fact]
        public async Task RefundCompleted_DoesNotSetChargebackRestriction()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantWithTopupAsync(context);
            context.AdminPaymentRefundIntents.Add(
                new AdminPaymentRefundIntent
                {
                    Id = Guid.NewGuid(),
                    IdempotencyKey = "idem-admin",
                    RestaurantId = seeded.RestaurantId,
                    SourcePaymentOrderId = seeded.PaymentOrderId,
                    RefundOrderId = "ord_refund_clean",
                    ActorStaffUserId = 1,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var lifecycle = new RecordingChargebackLifecycle();
            var webhookLifecycle = lifecycle;
            var ledger = new RecordingLedger();
            var vat = new RecordingVatInvoiceService();
            var handler = new RevolutPaymentRefundCompletedHandler(
                context,
                ledger,
                vat,
                TimeProvider.System
            );

            await handler.HandleAsync(
                new RevolutPaymentRefundCompletedRequest(
                    RefundOrderId: "ord_refund_clean",
                    RelatedOrderId: seeded.PaymentOrderId,
                    OrderType: RevolutOrderTypes.Refund,
                    AmountMinor: 1200,
                    RawOrderBody: "{}"
                )
            );

            Assert.Equal(1, ledger.DrainCallCount);
            Assert.Equal(0, lifecycle.SetChargebackCallCount);
            Assert.Equal(0, webhookLifecycle.SetChargebackCallCount);
        }

        [Fact]
        public async Task OrderCompleted_RefundType_RoutesToHandler_NotApplier()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantWithTopupAsync(context);
            var merchant = new FixedOrderMerchant(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_refund_wh",
                    State: "completed",
                    OrderType: RevolutOrderTypes.Refund,
                    RelatedOrderId: seeded.PaymentOrderId,
                    AmountMinor: 1200,
                    RawBody: """{"id":"ord_refund_wh","type":"refund","state":"completed","related_order_id":"pay"}"""
                )
            );
            var applier = new RecordingApplier();
            var refundHandler = new RecordingRefundHandler();
            var service = new RevolutWebhookService(
                context,
                merchant,
                applier,
                new NoOpBillingAccountLifecycle(),
                TimeProvider.System,
                Options.Create(
                    new RevolutSettings
                    {
                        WebhookSigningSecret = "whsec_service",
                        SecretKey = "sk_test",
                        ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                        ApiVersion = RevolutSettings.DefaultApiVersion,
                    }
                ),
                paymentRefundHandler: refundHandler
            );
            var body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_refund_wh"}""";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            var result = await service.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, result.Status);
            Assert.Null(applier.Last);
            Assert.NotNull(refundHandler.Last);
            Assert.Equal("ord_refund_wh", refundHandler.Last!.RefundOrderId);
            Assert.Equal(
                seeded.PaymentOrderId,
                refundHandler.Last.RelatedOrderId
            );
            var claim = await context.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(RevolutWebhookClaimDispositions.Applied, claim.Disposition);
        }

        private static async Task<Seeded> SeedRestaurantWithTopupAsync(
            ApplicationDbContext context
        )
        {
            var now = DateTime.UtcNow;
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Refund Owner",
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
                Name = "Refund Handler Venue",
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
                    BillingStatus = BillingStatuses.Active,
                    ContractedPricebookId = "pb",
                    StarterKitState = StarterKitStates.Unused,
                }
            );

            var paymentOrderId = $"ord_pay_{Guid.NewGuid():N}";
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.TopupAllocation,
                    Quantity = 100,
                    SourcePaymentRef = paymentOrderId,
                    CreatedAtUtc = now,
                }
            );
            await context.SaveChangesAsync();
            return new Seeded(restaurant.Id, paymentOrderId);
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

        private sealed record Seeded(int RestaurantId, string PaymentOrderId);

        private sealed class RecordingLedger : ICreditLedger
        {
            public int DrainCallCount { get; private set; }

            public string? LastSourcePaymentRef { get; private set; }

            public string? LastCorrectionSource { get; private set; }

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
                LastSourcePaymentRef = request.SourcePaymentRef;
                LastCorrectionSource = request.CorrectionSource;
                return Task.FromResult(
                    CreditLedgerDrainTopupResult.Ok([])
                );
            }

            public Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
                CreditLedgerRestoreTopupRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    CreditLedgerRestoreTopupResult.Fail("not_implemented")
                );

            public Task<CreditLedgerWriteResult> ReleaseHeldAsync(
                CreditLedgerReleaseHeldRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

            public Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));
        }

        private sealed class RecordingVatInvoiceService : ITummlyVatInvoiceService
        {
            public int MintCreditNoteCallCount { get; private set; }

            public string? LastRefundOrderId { get; private set; }

            public Task<TummlyVatInvoice> MintForCompletedOrderAsync(
                TummlyVatInvoiceMintRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<TummlyVatInvoice> MintCreditNoteForRefundAsync(
                TummlyVatCreditNoteMintRequest request,
                CancellationToken cancellationToken = default
            )
            {
                MintCreditNoteCallCount++;
                LastRefundOrderId = request.RefundOrderId;
                return Task.FromResult(
                    new TummlyVatInvoice
                    {
                        Id = Guid.NewGuid(),
                        DocumentNumber = "TCN-2026-000001",
                        DocumentPrefix = TummlyDocumentSequence.PrefixTcn,
                        RevolutOrderId = request.RefundOrderId,
                        RelatedRevolutOrderId = request.OriginalPaymentOrderId,
                        RestaurantId = request.RestaurantId,
                    }
                );
            }

            public Task<TummlyVatInvoice?> FindByRevolutOrderIdAsync(
                string revolutOrderId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<TummlyVatInvoice?>(null);

            public Task<IReadOnlyList<InvoiceRowDto>> ListInvoiceRowsForRestaurantAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult<IReadOnlyList<InvoiceRowDto>>([]);

            public Task<(byte[] Content, string FileName)?> RenderPdfAsync(
                int restaurantId,
                string documentNumber,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<(byte[] Content, string FileName)?>(null);
        }

        private sealed class RecordingChargebackLifecycle : IBillingAccountLifecycle
        {
            public int SetChargebackCallCount { get; private set; }

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
            )
            {
                SetChargebackCallCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingRefundHandler
            : IRevolutPaymentRefundCompletedHandler
        {
            public RevolutPaymentRefundCompletedRequest? Last { get; private set; }

            public Task HandleAsync(
                RevolutPaymentRefundCompletedRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Last = request;
                return Task.CompletedTask;
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
    }
}
