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

        private static RevolutWebhookService CreateService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IRevolutOrderCompletedApplier applier,
            IBillingAccountLifecycle? lifecycle = null
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
                settings
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
            ) => Task.CompletedTask;
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
