using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
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
        [Fact]
        public void CreateDefaultBillingAccount_IsFree()
        {
            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurantId: 1,
                contractedPricebookId: "TUMMLY-UK-GBP-2026-08-V3"
            );

            Assert.Equal(BillingSubscriptionPlans.Free, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Free, account.BillingStatus);
        }

        [Fact]
        public void ApplyPilotSignupBilling_SetsFreshPilotPeriodEnd()
        {
            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurantId: 1,
                contractedPricebookId: "TUMMLY-UK-GBP-2026-08-V3"
            );
            var now = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

            BillingCreditsService.ApplyPilotSignupBilling(account, now);

            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Pilot, account.BillingStatus);
            Assert.Equal(now.AddDays(30), account.PilotPeriodEnd);
        }

        [Theory]
        [InlineData("Pilot", "Starter", true, null, "monthly", true)]
        [InlineData("Starter", "Growth", false, "monthly", "monthly", true)]
        [InlineData("Starter", "Growth", false, "monthly", "annual", false)]
        [InlineData("Growth", "Starter", false, "monthly", "monthly", false)]
        [InlineData("Growth", "Growth", false, "monthly", "annual", false)]
        [InlineData("Free", "Pilot", false, null, "monthly", false)]
        [InlineData("Free", "Starter", false, null, "monthly", true)]
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

        [Fact]
        public async Task ClearScheduledChange_Downgrade_RestoresLiveRevolutPlan()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings(),
                paidStarter: true
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.SubscriptionPlan = BillingSubscriptionPlans.Growth;
            account.HasScheduledChange = true;
            account.ScheduledTargetSubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.ScheduledTargetBillingCycle = BillingCycles.Monthly;
            account.ScheduledTargetExtraLocationCount = 0;
            await harness.Context.SaveChangesAsync();

            var result = await harness.Service.ClearScheduledChangeAsync(
                harness.OwnerUserId,
                harness.RestaurantId
            );

            Assert.True(result!.Value.Success);
            var restore = Assert.Single(harness.CycleEndPlanChange.Restores);
            Assert.Equal((BillingSubscriptionPlans.Growth, "monthly", 0), restore);
            Assert.False(
                (await harness.Context.BillingAccounts.AsNoTracking().SingleAsync())
                    .HasScheduledChange
            );
        }

        [Fact]
        public async Task ClearScheduledChange_RevolutFails_KeepsSchedule()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings(),
                paidStarter: true
            );
            harness.CycleEndPlanChange.ThrowCode = "revolut_http_error";
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.SubscriptionPlan = BillingSubscriptionPlans.Growth;
            account.HasScheduledChange = true;
            account.ScheduledTargetSubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.ScheduledTargetBillingCycle = BillingCycles.Monthly;
            await harness.Context.SaveChangesAsync();

            var result = await harness.Service.ClearScheduledChangeAsync(
                harness.OwnerUserId,
                harness.RestaurantId
            );

            Assert.False(result!.Value.Success);
            Assert.Equal("revolut_http_error", result.Value.ErrorCode);
            Assert.True(
                (await harness.Context.BillingAccounts.AsNoTracking().SingleAsync())
                    .HasScheduledChange
            );
        }

        [Fact]
        public async Task ClearScheduledChange_ExtraRemoveAboveZero_SkipsRevolut()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings(),
                paidStarter: true
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.SubscriptionPlan = BillingSubscriptionPlans.Group;
            account.PaidExtraLocationCount = 2;
            account.HasScheduledChange = true;
            account.ScheduledTargetSubscriptionPlan = BillingSubscriptionPlans.Group;
            account.ScheduledTargetBillingCycle = account.BillingCycle;
            account.ScheduledTargetExtraLocationCount = 1;
            await harness.Context.SaveChangesAsync();

            var result = await harness.Service.ClearScheduledChangeAsync(
                harness.OwnerUserId,
                harness.RestaurantId
            );

            Assert.True(result!.Value.Success);
            Assert.Empty(harness.CycleEndPlanChange.Restores);
        }

        [Fact]
        public async Task ClearScheduledChange_CancelInsideLeadWindow_Refuses()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings(),
                paidStarter: true
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.HasScheduledChange = true;
            account.ScheduledCancelPlan = true;
            account.RenewalDateUtc = DateTime.UtcNow.AddHours(3);
            await harness.Context.SaveChangesAsync();

            var result = await harness.Service.ClearScheduledChangeAsync(
                harness.OwnerUserId,
                harness.RestaurantId
            );

            Assert.False(result!.Value.Success);
            Assert.Equal("cancel_already_sent", result.Value.ErrorCode);
            Assert.Empty(harness.CycleEndPlanChange.Restores);
        }

        [Fact]
        public async Task ClearScheduledChange_CancelBeforeLeadWindow_ClearsAndRestores()
        {
            await using var harness = await SeedAsync(
                new TummlySellerVatSettings(),
                paidStarter: true
            );
            var account = await harness.Context.BillingAccounts.SingleAsync();
            account.HasScheduledChange = true;
            account.ScheduledCancelPlan = true;
            account.RenewalDateUtc = DateTime.UtcNow.AddDays(10);
            await harness.Context.SaveChangesAsync();

            var result = await harness.Service.ClearScheduledChangeAsync(
                harness.OwnerUserId,
                harness.RestaurantId
            );

            Assert.True(result!.Value.Success);
            // Guarded restore: Revolut may still hold an earlier downgrade.
            Assert.Single(harness.CycleEndPlanChange.Restores);
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

        [Fact]
        public async Task RetryPendingPayment_StandardPlan_ReturnsCheckout_AndKeepsPending()
        {
            await using var context = BillingActivityTestDb.Create();
            var firstPaid = new RecordingFirstPaidConversionPaySession(context);
            var restaurantId = await SeedPendingPaymentAsync(context, BillingSubscriptionPlans.Growth, null);
            var service = BuildPendingRetryService(context, firstPaid);

            var (response, statusCode, error) = await service.RetryPendingPaymentAsync(
                ownerIdOf(context, restaurantId),
                restaurantId,
                actorCanManage: true
            );

            Assert.Equal(StatusCodes.Status200OK, statusCode);
            Assert.Null(error);
            Assert.Equal(RecordingFirstPaidConversionPaySession.CheckoutUrl, response!.CheckoutUrl);
            Assert.Equal(BillingSubscriptionPlans.Growth, firstPaid.LastTargetPlan);
            var billing = await context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Growth, billing.PendingPaymentPlan);
            Assert.Equal(BillingSubscriptionPlans.Free, billing.SubscriptionPlan);
        }

        [Fact]
        public async Task RetryPendingPayment_StandardPlan_CheckoutFails_RecordsFailureAndAudits()
        {
            await using var context = BillingActivityTestDb.Create();
            var firstPaid = new RecordingFirstPaidConversionPaySession(context)
            {
                FailWithCode = "revolut_http_error",
            };
            var restaurantId = await SeedPendingPaymentAsync(context, BillingSubscriptionPlans.Growth, null);
            var service = BuildPendingRetryService(context, firstPaid);

            var (response, statusCode, error) = await service.RetryPendingPaymentAsync(
                ownerIdOf(context, restaurantId),
                restaurantId,
                actorCanManage: true
            );

            Assert.Null(response);
            Assert.Equal(StatusCodes.Status502BadGateway, statusCode);
            Assert.Equal("revolut_http_error", error);
            var billing = await context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Growth, billing.PendingPaymentPlan);
            Assert.Equal("revolut_http_error", billing.PendingPaymentFailureCode);
            var audit = await context.AdminAuditEvents.SingleAsync();
            Assert.Equal(AdminAuditActions.BillingPendingPaymentRetryFailed, audit.Action);
            Assert.False(audit.Succeeded);
        }

        [Fact]
        public async Task RetryPendingPayment_CustomPlan_CheckoutFails_KeepsSavedQuote()
        {
            await using var context = BillingActivityTestDb.Create();
            var quote = new CustomPlanConfig
            {
                Cadence = "monthly",
                MonthlyNetPence = 12120,
                PeriodNetPence = 12120,
                Quantities = new CustomPlanQuantities { Locations = 2, EmailCredits = 20000 },
            }.ToJson();
            var restaurantId = await SeedPendingPaymentAsync(context, BillingSubscriptionPlans.Custom, quote);
            var service = BuildPendingRetryService(context, new RecordingFirstPaidConversionPaySession(context));

            var (response, statusCode, error) = await service.RetryPendingPaymentAsync(
                ownerIdOf(context, restaurantId),
                restaurantId,
                actorCanManage: true
            );

            Assert.Null(response);
            Assert.Equal(StatusCodes.Status502BadGateway, statusCode);
            Assert.NotNull(error);
            var billing = await context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Custom, billing.PendingPaymentPlan);
            Assert.Equal(quote, billing.PendingPaymentCustomConfigJson);
        }

        [Fact]
        public async Task RetryPendingPayment_NoPendingPayment_Returns404()
        {
            await using var context = BillingActivityTestDb.Create();
            var restaurantId = await SeedPendingPaymentAsync(context, null, null);
            var service = BuildPendingRetryService(context, new RecordingFirstPaidConversionPaySession(context));

            var (response, statusCode, error) = await service.RetryPendingPaymentAsync(
                ownerIdOf(context, restaurantId),
                restaurantId,
                actorCanManage: true
            );

            Assert.Null(response);
            Assert.Equal(StatusCodes.Status404NotFound, statusCode);
            Assert.Equal("no_pending_payment", error);
        }

        private static int ownerIdOf(ApplicationDbContext context, int restaurantId)
        {
            return context.Restaurants.Single(row => row.Id == restaurantId).OwnerUserId;
        }

        private static async Task<int> SeedPendingPaymentAsync(
            ApplicationDbContext context,
            string? pendingPlan,
            string? pendingCustomJson
        )
        {
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
                Name = "Pending Payment Test",
                AccountType = "Single",
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
            account.PendingPaymentPlan = pendingPlan;
            account.PendingPaymentCadence = pendingPlan == null ? null : "monthly";
            account.PendingPaymentCustomConfigJson = pendingCustomJson;
            account.PendingPaymentFailedAtUtc = pendingPlan == null ? null : DateTime.UtcNow;
            context.BillingAccounts.Add(account);
            await context.SaveChangesAsync();
            return restaurant.Id;
        }

        private static BillingCreditsService BuildPendingRetryService(
            ApplicationDbContext context,
            IFirstPaidConversionPaySession firstPaid
        )
        {
            var sellerVat = new TummlySellerVatSettings();
            return new BillingCreditsService(
                context,
                TestPricebookPaths.LoadV3(),
                new StubCreditBalanceSnapshot(),
                new StubBillingAccountLifecycle(),
                new PlanChangeService(context, TestPricebookPaths.LoadV3(), TimeProvider.System),
                new AlwaysReadyRevolutMerchantCreateGate(),
                firstPaid,
                new ThrowingSameCadenceUpgradePaySession(),
                new ThrowingPaymentMethodUpdatePaySession(),
                new ThrowingCreditTopUpPaySession(),
                new EmptyTummlyVatInvoiceService(),
                new RecordingCycleEndPlanChange(),
                new NoOpCreditLedger(),
                Options.Create(sellerVat),
                new EmailServiceStubBase(),
                new CustomPlanCycleBillingService(
                    context,
                    new NoOpRevolutMerchantForCustomPlan(),
                    TimeProvider.System,
                    Options.Create(sellerVat)
                ),
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://app.test",
                        }
                    )
                    .Build()
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

            var cycleEndPlanChange = new RecordingCycleEndPlanChange();
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
                cycleEndPlanChange,
                new NoOpCreditLedger(),
                Options.Create(sellerVat),
                new EmailServiceStubBase(),
                new CustomPlanCycleBillingService(
                    context,
                    new NoOpRevolutMerchantForCustomPlan(),
                    TimeProvider.System,
                    Options.Create(sellerVat)
                ),
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://app.test",
                        }
                    )
                    .Build()
            );
            return new Harness(
                context,
                service,
                restaurant.Id,
                owner.Id,
                cycleEndPlanChange
            );
        }

        private sealed class Harness : IAsyncDisposable
        {
            public Harness(
                ApplicationDbContext context,
                BillingCreditsService service,
                int restaurantId,
                int ownerUserId,
                RecordingCycleEndPlanChange cycleEndPlanChange
            )
            {
                Context = context;
                Service = service;
                RestaurantId = restaurantId;
                OwnerUserId = ownerUserId;
                CycleEndPlanChange = cycleEndPlanChange;
            }

            public RecordingCycleEndPlanChange CycleEndPlanChange { get; }

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

        private sealed class RecordingCycleEndPlanChange : ICycleEndPlanChange
        {
            public List<(string Plan, string Cadence, int Extras)> Restores { get; } = [];

            public string? ThrowCode { get; set; }

            public Task ApplyRevolutChangePlanIfNeededAsync(
                int restaurantId,
                string targetPlan,
                string targetCadenceApi,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task RestoreLiveRevolutPlanIfNeededAsync(
                int restaurantId,
                string livePlan,
                string liveCadenceApi,
                int paidExtraLocationCount,
                CancellationToken cancellationToken = default
            )
            {
                if (ThrowCode != null)
                {
                    throw new InvalidOperationException(ThrowCode);
                }

                Restores.Add((livePlan, liveCadenceApi, paidExtraLocationCount));
                return Task.CompletedTask;
            }
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
                CancellationToken cancellationToken = default,
                string? successRedirectUrl = null
            ) =>
                throw new NotImplementedException(
                    "First paid conversion is not under test here."
                );

            public Task AbandonOpenSessionsAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
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
