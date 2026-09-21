using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class BillingCreditsServiceTests
    {
        [Theory]
        [InlineData("Pilot", "Starter", true, null, "monthly", true)]
        [InlineData("Starter", "Growth", false, "monthly", "monthly", true)]
        [InlineData("Starter", "Growth", false, "monthly", "annual", false)]
        [InlineData("Growth", "Starter", false, "monthly", "monthly", false)]
        [InlineData("Growth", "Growth", false, "monthly", "annual", false)]
        public void ResolvePlanChangeRequiresPay_MatchesContract(
            string currentPlan,
            string targetPlan,
            bool isPilot,
            string? liveCadence,
            string targetCadence,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                BillingCreditsService.ResolvePlanChangeRequiresPay(
                    currentPlan,
                    targetPlan,
                    isPilot,
                    liveCadence,
                    targetCadence
                )
            );
        }

        [Theory]
        [InlineData(0, 5, 5, false)]
        [InlineData(1, 5, 6, false)]
        [InlineData(2, 6, 6, true)]
        [InlineData(2, 6, 7, false)]
        public void CanRemoveExtraGroupLocation_MatchesContract(
            int paidExtra,
            int entitledAfterRemove,
            int activeLocations,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                BillingCreditsService.CanRemoveExtraGroupLocation(
                    paidExtra,
                    entitledAfterRemove,
                    activeLocations
                )
            );
        }

        [Fact]
        public async Task GetPage_WhenVatModeOff_ExposesZeroVatRateAndOffMode()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings { IsActive = false }
            );

            var page = await harness.Service.GetPageAsync(
                harness.OwnerUserId,
                harness.RestaurantId,
                actorCanManage: true
            );

            Assert.NotNull(page);
            Assert.Equal(0, page!.CurrentCatalog.VatRateBps);
            Assert.False(page.VatModeActive);
        }

        [Fact]
        public async Task GetPage_WhenVatModeActive_Exposes2000VatRateAndActiveMode()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings { IsActive = true }
            );

            var page = await harness.Service.GetPageAsync(
                harness.OwnerUserId,
                harness.RestaurantId,
                actorCanManage: true
            );

            Assert.NotNull(page);
            Assert.Equal(2000, page!.CurrentCatalog.VatRateBps);
            Assert.True(page.VatModeActive);
        }

        [Fact]
        public async Task ConfirmCreditTopUp_WhenVatModeOff_GrossEqualsNet()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings { IsActive = false },
                paidStarter: true
            );

            var (response, statusCode, error) =
                await harness.Service.ConfirmCreditTopUpAsync(
                    harness.OwnerUserId,
                    harness.RestaurantId,
                    actorCanManage: true,
                    new CreditTopUpRequestDto { Channel = "sms", Quantity = 500 }
                );

            Assert.Equal(StatusCodes.Status200OK, statusCode);
            Assert.Null(error);
            Assert.NotNull(response);
            Assert.Equal("£55", response!.NetLabel);
            Assert.Equal("£55", response.GrossLabel);
            Assert.Equal("£0", response.VatLabel);
        }

        [Fact]
        public async Task ConfirmCreditTopUp_WhenVatModeActive_GrossIncludes20PercentVat()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings { IsActive = true },
                paidStarter: true
            );

            var (response, statusCode, error) =
                await harness.Service.ConfirmCreditTopUpAsync(
                    harness.OwnerUserId,
                    harness.RestaurantId,
                    actorCanManage: true,
                    new CreditTopUpRequestDto { Channel = "sms", Quantity = 500 }
                );

            Assert.Equal(StatusCodes.Status200OK, statusCode);
            Assert.Null(error);
            Assert.NotNull(response);
            Assert.Equal("£55", response!.NetLabel);
            Assert.Equal("£66", response.GrossLabel);
            Assert.Equal("£11", response.VatLabel);
        }

        [Theory]
        [InlineData(55, 0, 55)]
        [InlineData(55, 2000, 66)]
        [InlineData(12, 2000, 14.4)]
        public void GrossPounds_UsesEffectiveVatRateBps(
            decimal net,
            int vatRateBps,
            decimal expectedGross
        )
        {
            Assert.Equal(
                expectedGross,
                CreditTopUpPricebook.GrossPounds(net, vatRateBps)
            );
        }

        private static async Task<Harness> SeedAsync(
            TummlySellerVatSettings sellerVat,
            bool paidStarter = false
        )
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
                Name = "Billing Credits VAT Test",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var pricebook = TestPricebookPaths.LoadV3();
            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                pricebook.CurrentPricebookId
            );
            if (paidStarter)
            {
                account.SubscriptionPlan = BillingSubscriptionPlans.Starter;
                account.BillingStatus = BillingStatuses.Active;
                account.BillingCycle = "Monthly";
            }
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();

            var service = new BillingCreditsService(
                context,
                pricebook,
                new StubCreditBalanceSnapshot(),
                new StubBillingAccountLifecycle(),
                new PlanChangeService(context, pricebook, TimeProvider.System),
                new AlwaysReadyRevolutMerchantCreateGate(),
                new ThrowingFirstPaidConversionPaySession(),
                new ThrowingSameCadenceUpgradePaySession(),
                new ThrowingPaymentMethodUpdatePaySession(),
                new ThrowingCreditTopUpPaySession(),
                new EmptyTummlyVatInvoiceService(),
                new NoOpCycleEndPlanChange(),
                new NoOpCycleEndPlanCancel(),
                Options.Create(sellerVat)
            );
            return new Harness(context, service, restaurant.Id, owner.Id);
        }

        private sealed class Harness : IAsyncDisposable
        {
            public Harness(
                ApplicationDbContext context,
                BillingCreditsService service,
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

            public BillingCreditsService Service { get; }

            public int RestaurantId { get; }

            public int OwnerUserId { get; }

            public ValueTask DisposeAsync() => Context.DisposeAsync();
        }

        private sealed class EmptyTummlyVatInvoiceService : ITummlyVatInvoiceService
        {
            public Task<TummlyVatInvoice> MintForCompletedOrderAsync(
                TummlyVatInvoiceMintRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<TummlyVatInvoice> MintCreditNoteForRefundAsync(
                TummlyVatCreditNoteMintRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

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

        private sealed class NoOpCycleEndPlanChange : ICycleEndPlanChange
        {
            public Task ApplyRevolutChangePlanIfNeededAsync(
                int restaurantId,
                string targetPlan,
                string targetCadenceApi,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }

        private sealed class NoOpCycleEndPlanCancel : ICycleEndPlanCancel
        {
            public Task ApplyRevolutCancelAtCycleEndIfNeededAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }

        private sealed class ThrowingFirstPaidConversionPaySession
            : IFirstPaidConversionPaySession
        {
            public Task<PlanChangeResultDto> StartAsync(
                BillingAccount billingAccount,
                User owner,
                string restaurantAccountType,
                int locationId,
                string targetPlan,
                string targetCadenceApi,
                string idempotencyKey,
                CancellationToken cancellationToken = default
            ) =>
                throw new NotImplementedException(
                    "First paid conversion is not under test here."
                );
        }

        private sealed class ThrowingSameCadenceUpgradePaySession
            : ISameCadenceUpgradePaySession
        {
            public Task<PlanChangeResultDto> StartAsync(
                BillingAccount billingAccount,
                string restaurantAccountType,
                int locationId,
                string targetPlan,
                string targetCadenceApi,
                string idempotencyKey,
                CancellationToken cancellationToken = default
            ) =>
                throw new NotImplementedException(
                    "Same-cadence upgrade is not under test here."
                );
        }

        private sealed class ThrowingPaymentMethodUpdatePaySession
            : IPaymentMethodUpdatePaySession
        {
            public Task<PaymentMethodUpdateSessionDto> StartAsync(
                BillingAccount billingAccount,
                string restaurantAccountType,
                int locationId,
                CancellationToken cancellationToken = default
            ) =>
                throw new NotImplementedException(
                    "Payment method update is not under test here."
                );
        }

        private sealed class ThrowingCreditTopUpPaySession : ICreditTopUpPaySession
        {
            public Task<string> StartAsync(
                BillingAccount billingAccount,
                string restaurantAccountType,
                int locationId,
                PricebookTopUpPack pack,
                string idempotencyKey,
                CancellationToken cancellationToken = default
            ) =>
                throw new NotImplementedException(
                    "Credit top-up pay is not under test here."
                );
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
            ) => Task.CompletedTask;

            public BillingLifecycleCommandResult ApplyPostCancelSoftLock(
                BillingAccount billingAccount,
                DateTime renewalEndUtc
            ) => BillingLifecycleCommandResult.NoOp();
        }
    }
}
