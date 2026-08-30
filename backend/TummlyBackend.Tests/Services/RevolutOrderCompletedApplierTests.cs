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
            var applier = CreateApplier(context, mint, clock);

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
            var invoice = await context.TummlyVatInvoices.SingleAsync();
            Assert.Equal("ord_txn", invoice.RevolutOrderId);
            Assert.StartsWith("TM-2026-", invoice.DocumentNumber);
        }

        [Fact]
        public async Task PaySessionStart_DoesNotActivate_BeforeWebhook()
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
        public async Task Apply_CycleBilling_RenewsAndMints()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(
                context,
                "ord_cycle",
                "sub_cycle"
            );
            var account = await context.BillingAccounts.SingleAsync();
            account.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.BillingCycle = BillingCycles.Monthly;
            account.BillingStatus = BillingStatuses.Active;
            account.RenewalDateUtc = _now.Date;
            pending.IsOpen = false;
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_cycle",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.CycleBilling,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Equal(_now.Date.AddMonths(1), account.RenewalDateUtc);
            Assert.Null(account.DunningEpisodeStartedAt);
            Assert.Equal(
                3,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == account.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_cycle"
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

        [Fact]
        public async Task Apply_CycleBilling_OpenPending_AppliesPlanFromPending()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(
                context,
                "ord_first_cycle",
                "sub_first_cycle"
            );
            pending.TargetPlan = BillingSubscriptionPlans.Starter;
            pending.TargetCadence = "monthly";
            pending.IsOpen = true;
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_first_cycle",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.CycleBilling,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var account = await context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Starter, account.SubscriptionPlan);
            Assert.Equal(BillingCycles.Monthly, account.BillingCycle);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.False(
                (
                    await context.RevolutPendingPaySessions.SingleAsync()
                ).IsOpen
            );
        }

        [Fact]
        public async Task Webhook_RetriesSkippedUnknownBillingReason_AndApplies()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(
                context,
                "ord_retry",
                "sub_retry"
            );
            context.RevolutWebhookEventClaims.Add(
                new RevolutWebhookEventClaim
                {
                    Id = Guid.NewGuid(),
                    Event = "ORDER_COMPLETED",
                    ObjectId = "ord_retry",
                    Disposition =
                        RevolutWebhookClaimDispositions.SkippedUnknownBillingReason,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var merchant = new FixedOrderMerchant(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_retry",
                    State: "completed",
                    BillingReason: RevolutOrderCompletedApplier.CycleBilling,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawBody: """{"id":"ord_retry","state":"completed"}"""
                )
            );
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);
            var service = new RevolutWebhookService(
                context,
                merchant,
                applier,
                new NoOpBillingAccountLifecycle(),
                clock,
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
            var body = """{"event":"ORDER_COMPLETED","order_id":"ord_retry"}""";
            var timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_service",
                timestamp,
                body
            );

            var result = await service.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, result.Status);
            var claim = await context.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(
                RevolutWebhookClaimDispositions.Applied,
                claim.Disposition
            );
            var account = await context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Starter, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
        }

        [Fact]
        public async Task Apply_Topup_AllocatesOnceAndMintsTm()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_topup_ai",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = "k_topup",
                    IsOpen = true,
                    NetAmountMinor = 1500,
                    VatAmountMinor = 300,
                    GrossAmountMinor = 1800,
                    Channel = "ai",
                    Quantity = 500,
                    PackLookupKey = "tummly_ai_500_gbp_v3",
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_topup_ai",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Equal(
                1,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == account.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                    && row.SourcePaymentRef == "ord_topup_ai"
                    && row.Channel == CreditChannels.Ai
                    && row.Quantity == 500
                )
            );
            var invoice = await context.TummlyVatInvoices.SingleAsync(row =>
                row.RevolutOrderId == "ord_topup_ai"
            );
            Assert.Equal(1500, invoice.NetPence);
            Assert.Contains("AI", invoice.LineDescription);
            Assert.False(
                await context.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_topup_ai")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task Apply_Topup_ClosedIntent_DoesNotDoubleAllocate()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_topup_closed",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = "k_closed",
                    IsOpen = false,
                    NetAmountMinor = 1500,
                    VatAmountMinor = 300,
                    GrossAmountMinor = 1800,
                    Channel = "ai",
                    Quantity = 500,
                    PackLookupKey = "tummly_ai_500_gbp_v3",
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_topup_closed",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                )
            );
            Assert.Equal(0, await context.TummlyVatInvoices.CountAsync());
        }

        [Fact]
        public async Task Apply_Topup_AbandonedOpenIntent_DoesNotAllocateWithoutApply()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_topup_abandon",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = "k_abandon",
                    IsOpen = true,
                    NetAmountMinor = 1500,
                    VatAmountMinor = 300,
                    GrossAmountMinor = 1800,
                    Channel = "ai",
                    Quantity = 500,
                    PackLookupKey = "tummly_ai_500_gbp_v3",
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                )
            );
            Assert.True(
                await context.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_topup_abandon")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task Apply_Topup_ReplayAfterApply_DoesNotDoubleAllocate()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_topup_replay",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = "k_replay_topup",
                    IsOpen = true,
                    NetAmountMinor = 1500,
                    VatAmountMinor = 300,
                    GrossAmountMinor = 1800,
                    Channel = "ai",
                    Quantity = 500,
                    PackLookupKey = "tummly_ai_500_gbp_v3",
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var request = new RevolutOrderCompletedApplyRequest(
                OrderId: "ord_topup_replay",
                OrderState: "completed",
                BillingReason: null,
                SubscriptionId: null,
                RawWebhookBody: "{}",
                RawOrderBody: "{}"
            );
            await applier.ApplyAsync(request);
            await applier.ApplyAsync(request);

            Assert.Equal(
                1,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                    && row.SourcePaymentRef == "ord_topup_replay"
                )
            );
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_topup_replay"
                )
            );
        }

        [Fact]
        public async Task Apply_PlanUpgradeProration_UpgradesChangePlanAndMintsTm()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            var merchant = new RecordingChangePlanMerchant();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock, merchant);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_upgrade",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.PlanUpgradeProration,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_upgrade",
                    CheckoutUrl = "https://checkout.revolut.test/up",
                    IdempotencyKey = "k1",
                    IsOpen = true,
                    NetAmountMinor = 3000,
                    VatAmountMinor = 600,
                    GrossAmountMinor = 3600,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_upgrade",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: "sub_upgrade",
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingSubscriptionPlans.Growth, account.SubscriptionPlan);
            Assert.Equal(1, merchant.ChangePlanCallCount);
            Assert.Equal("sub_upgrade", merchant.LastSubscriptionId);
            Assert.Equal(
                RevolutPlanVariationKeys.GrowthMonthly,
                merchant.LastLookupKey
            );
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_upgrade"
                )
            );
            Assert.False(
                await context.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_upgrade")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task Apply_ExtraLocation_AppliesAddOnChangePlanAndMintsTm()
        {
            await using var context = CreateContext();
            var account = await SeedActiveGroupAsync(context);
            var merchant = new RecordingChangePlanMerchant();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock, merchant);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_extra",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.ExtraLocation,
                    TargetPlan = BillingSubscriptionPlans.Group,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_extra",
                    CheckoutUrl = "https://checkout.revolut.test/extra",
                    IdempotencyKey = "extra-1",
                    IsOpen = true,
                    NetAmountMinor = 2000,
                    VatAmountMinor = 400,
                    GrossAmountMinor = 2400,
                    TargetPaidExtraLocationCount = 1,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_extra",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: "sub_extra",
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            await context.Entry(account).ReloadAsync();
            Assert.Equal(1, account.PaidExtraLocationCount);
            Assert.Equal(1, merchant.ChangePlanCallCount);
            Assert.Equal(
                RevolutPlanVariationKeys.GroupLocationMonthly,
                merchant.LastLookupKey
            );
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_extra"
                )
            );
            Assert.False(
                await context.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_extra")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task Apply_ExtraLocation_ClosedIntent_DoesNotDoubleApply()
        {
            await using var context = CreateContext();
            var account = await SeedActiveGroupAsync(context);
            var merchant = new RecordingChangePlanMerchant();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock, merchant);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_extra_replay",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.ExtraLocation,
                    TargetPlan = BillingSubscriptionPlans.Group,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_extra",
                    CheckoutUrl = "https://checkout.revolut.test/extra",
                    IdempotencyKey = "extra-replay",
                    IsOpen = true,
                    NetAmountMinor = 2000,
                    VatAmountMinor = 400,
                    GrossAmountMinor = 2400,
                    TargetPaidExtraLocationCount = 1,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var request = new RevolutOrderCompletedApplyRequest(
                OrderId: "ord_extra_replay",
                OrderState: "completed",
                BillingReason: null,
                SubscriptionId: "sub_extra",
                RawWebhookBody: "{}",
                RawOrderBody: "{}"
            );
            await applier.ApplyAsync(request);
            await applier.ApplyAsync(request);

            await context.Entry(account).ReloadAsync();
            Assert.Equal(1, account.PaidExtraLocationCount);
            Assert.Equal(1, merchant.ChangePlanCallCount);
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_extra_replay"
                )
            );
        }

        [Fact]
        public async Task Webhook_ExtraLocation_Replay_DoesNotDoubleApply()
        {
            await using var context = CreateContext();
            var account = await SeedActiveGroupAsync(context);
            var merchant = new RecordingChangePlanMerchant();
            merchant.Orders["ord_extra_wh"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_extra_wh",
                State: "completed",
                BillingReason: null,
                SubscriptionId: "sub_extra_wh",
                RawBody: """{"id":"ord_extra_wh","state":"completed"}"""
            );
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_extra_wh",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.ExtraLocation,
                    TargetPlan = BillingSubscriptionPlans.Group,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_extra_wh",
                    CheckoutUrl = "https://checkout.revolut.test/extra",
                    IdempotencyKey = "extra-wh",
                    IsOpen = true,
                    NetAmountMinor = 2000,
                    VatAmountMinor = 400,
                    GrossAmountMinor = 2400,
                    TargetPaidExtraLocationCount = 1,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock, merchant);
            var webhook = new RevolutWebhookService(
                context,
                merchant,
                applier,
                new NoOpBillingAccountLifecycle(),
                TimeProvider.System,
                Options.Create(
                    new RevolutSettings
                    {
                        WebhookSigningSecret = "whsec_replay_extra",
                        SecretKey = "sk_test",
                        ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                        ApiVersion = RevolutSettings.DefaultApiVersion,
                    }
                )
            );

            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_extra_wh"}""";
            const string timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_replay_extra",
                timestamp,
                body
            );

            var first = await webhook.HandleAsync(body, signature, timestamp);
            var second = await webhook.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, first.Status);
            Assert.Equal(RevolutWebhookHandleStatus.Replay, second.Status);
            Assert.Equal(1, merchant.ChangePlanCallCount);
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_extra_wh"
                )
            );
            await context.Entry(account).ReloadAsync();
            Assert.Equal(1, account.PaidExtraLocationCount);
        }

        [Fact]
        public async Task Apply_WithoutIntent_FallsThroughToBillingReasonPath()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_setup", "sub_s");
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_setup",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var account = await context.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Starter, account.SubscriptionPlan);
            Assert.True(
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                ) >= 1
            );
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_setup"
                )
            );
        }

        [Fact]
        public async Task Webhook_PlanUpgradeProration_Replay_DoesNotDoubleApply()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            var merchant = new RecordingChangePlanMerchant();
            merchant.Orders["ord_upgrade_replay"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_upgrade_replay",
                State: "completed",
                BillingReason: null,
                SubscriptionId: "sub_upgrade_replay",
                RawBody: """{"id":"ord_upgrade_replay","state":"completed"}"""
            );
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_upgrade_replay",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.PlanUpgradeProration,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_upgrade_replay",
                    CheckoutUrl = "https://checkout.revolut.test/up",
                    IdempotencyKey = "k_replay",
                    IsOpen = true,
                    NetAmountMinor = 3000,
                    VatAmountMinor = 600,
                    GrossAmountMinor = 3600,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock, merchant);
            var webhook = new RevolutWebhookService(
                context,
                merchant,
                applier,
                new NoOpBillingAccountLifecycle(),
                TimeProvider.System,
                Options.Create(
                    new RevolutSettings
                    {
                        WebhookSigningSecret = "whsec_replay_upgrade",
                        SecretKey = "sk_test",
                        ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                        ApiVersion = RevolutSettings.DefaultApiVersion,
                    }
                )
            );

            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_upgrade_replay"}""";
            const string timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                "whsec_replay_upgrade",
                timestamp,
                body
            );

            var first = await webhook.HandleAsync(body, signature, timestamp);
            var second = await webhook.HandleAsync(body, signature, timestamp);

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, first.Status);
            Assert.Equal(RevolutWebhookHandleStatus.Replay, second.Status);
            Assert.Equal(1, merchant.ChangePlanCallCount);
            Assert.Equal(
                1,
                await context.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_upgrade_replay"
                )
            );
            Assert.Equal(
                1,
                await context.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "ORDER_COMPLETED"
                    && row.ObjectId == "ord_upgrade_replay"
                )
            );
            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingSubscriptionPlans.Growth, account.SubscriptionPlan);
        }

        private async Task<BillingAccount> SeedActiveStarterAsync(
            ApplicationDbContext context
        )
        {
            var (account, _) = await SeedPilotAccountAsync(context);
            account.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.BillingStatus = BillingStatuses.Active;
            account.BillingCycle = BillingCycles.Monthly;
            account.RenewalDateUtc = _now.Date.AddMonths(1);
            account.RevolutCustomerId = "cust_active";
            await context.SaveChangesAsync();
            return account;
        }

        private async Task<BillingAccount> SeedActiveGroupAsync(
            ApplicationDbContext context
        )
        {
            var (account, _) = await SeedPilotAccountAsync(context);
            account.SubscriptionPlan = BillingSubscriptionPlans.Group;
            account.BillingStatus = BillingStatuses.Active;
            account.BillingCycle = BillingCycles.Monthly;
            account.RenewalDateUtc = _now.Date.AddMonths(1);
            account.RevolutCustomerId = "cust_group";
            account.PaidExtraLocationCount = 0;
            await context.SaveChangesAsync();
            return account;
        }

        private RevolutOrderCompletedApplier CreateApplier(
            ApplicationDbContext context,
            IIncludedPeriodMintService mint,
            TimeProvider clock,
            IRevolutMerchantClient? merchant = null
        )
        {
            return new RevolutOrderCompletedApplier(
                context,
                mint,
                CreateVatService(context),
                new PlanChangeService(context, _pricebook, clock),
                new ExtraGroupLocationService(
                    context,
                    _pricebook,
                    new AlwaysReadyRevolutMerchantCreateGate(),
                    merchant ?? new RecordingLandMerchant(),
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                ["Frontend:BaseUrl"] = "https://app.test",
                            }
                        )
                        .Build(),
                    clock
                ),
                new CreditLedgerService(context, clock, _pricebook),
                merchant ?? new RecordingLandMerchant(),
                clock
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

        private TummlyVatInvoiceService CreateVatService(ApplicationDbContext context)
        {
            return new TummlyVatInvoiceService(
                context,
                _pricebook,
                Options.Create(
                    new TummlySellerVatSettings
                    {
                        RegistrationNumber = "GB123456789",
                        EffectiveDate = "2024-01-01",
                        LegalName = "Tummly Ltd",
                        RegisteredAddress = "1 High Street",
                    }
                )
            );
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

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
                );
        }

        private sealed class RecordingChangePlanMerchant : IRevolutMerchantClient
        {
            public Dictionary<string, RevolutOrderRetrieveResult> Orders { get; } =
                new(StringComparer.Ordinal);

            public int ChangePlanCallCount { get; private set; }

            public string? LastSubscriptionId { get; private set; }

            public string? LastLookupKey { get; private set; }

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
            )
            {
                ChangePlanCallCount++;
                LastSubscriptionId = subscriptionId;
                LastLookupKey = planVariationLookupKey;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
                );
            }

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            )
            {
                if (Orders.TryGetValue(orderId, out var order))
                {
                    return Task.FromResult(order);
                }

                return Task.FromResult(
                    new RevolutOrderRetrieveResult(
                        Succeeded: false,
                        ErrorCode: "not_found"
                    )
                );
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
