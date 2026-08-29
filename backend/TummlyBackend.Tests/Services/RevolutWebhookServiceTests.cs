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

        private static RevolutWebhookService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IRevolutOrderCompletedApplier applier
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
                new NoOpBillingAccountLifecycle(),
                TimeProvider.System,
                settings
            );
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
