using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
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
        public async Task Apply_WhenModeOff_MintsZeroVatInvoiceAndEmails()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_off", "sub_off");
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var offVat = new TummlySellerVatSettings
            {
                IsActive = false,
                RegistrationNumber = "GB123456789",
                EffectiveDate = "2024-01-01",
                LegalName = "Tummly Ltd",
                RegisteredAddress = "1 High Street",
            };
            var emailDelivery = new RecordingInvoiceEmailDelivery();
            var applier = CreateApplier(
                context,
                mint,
                clock,
                invoiceEmail: emailDelivery,
                sellerVat: offVat
            );

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_off",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var invoice = Assert.Single(await context.TummlyVatInvoices.ToListAsync());
            Assert.Equal(0, invoice.VatRateBps);
            Assert.Equal(0, invoice.VatPence);
            Assert.Equal(invoice.NetPence, invoice.GrossPence);
            Assert.Single(emailDelivery.Calls);
            Assert.True(emailDelivery.Calls[0].WasNewlyMinted);
            var account = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
        }

        [Fact]
        public async Task Apply_PlanUpgradeProration_WhenModeOff_EmailsInvoice()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            var merchant = new RecordingChangePlanMerchant();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var offVat = new TummlySellerVatSettings
            {
                IsActive = false,
                RegistrationNumber = "GB123456789",
                EffectiveDate = "2024-01-01",
                LegalName = "Tummly Ltd",
                RegisteredAddress = "1 High Street",
            };
            var emailDelivery = new RecordingInvoiceEmailDelivery();
            var applier = CreateApplier(
                context,
                mint,
                clock,
                merchant,
                invoiceEmail: emailDelivery,
                sellerVat: offVat
            );

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_upgrade_off",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.PlanUpgradeProration,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_upgrade_off",
                    CheckoutUrl = "https://checkout.revolut.test/up",
                    IdempotencyKey = "k_upgrade_off",
                    IsOpen = true,
                    NetAmountMinor = 3000,
                    VatAmountMinor = 0,
                    GrossAmountMinor = 3000,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_upgrade_off",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: "sub_upgrade_off",
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var invoice = Assert.Single(
                await context.TummlyVatInvoices
                    .Where(row => row.RevolutOrderId == "ord_upgrade_off")
                    .ToListAsync()
            );
            Assert.Equal(0, invoice.VatRateBps);
            Assert.Equal(3000, invoice.GrossPence);
            Assert.Single(emailDelivery.Calls);
            Assert.True(emailDelivery.Calls[0].WasNewlyMinted);
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

            var activityKinds = await context.RestaurantBillingActivities
                .OrderBy(row => row.OccurredAtUtc)
                .ThenBy(row => row.Id)
                .Select(row => row.Kind)
                .ToListAsync();
            Assert.Equal(
                [
                    BillingActivityKinds.SubscriptionCreated,
                    BillingActivityKinds.InvoicePaid,
                ],
                activityKinds
            );
            var created = await context.RestaurantBillingActivities.SingleAsync(
                row => row.Kind == BillingActivityKinds.SubscriptionCreated
            );
            Assert.Equal("Apply Owner", created.ActorDisplayName);
            Assert.Equal(BillingSubscriptionPlans.Starter, created.Plan);
            Assert.Equal(BillingCycles.Monthly, created.Cadence);
            var invoicePaid = await context.RestaurantBillingActivities.SingleAsync(
                row => row.Kind == BillingActivityKinds.InvoicePaid
            );
            Assert.False(string.IsNullOrWhiteSpace(invoicePaid.InvoiceNo));
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

            await using var ambient =
                await context.Database.BeginTransactionAsync();
            context.RevolutWebhookEventClaims.Add(
                new RevolutWebhookEventClaim
                {
                    Id = Guid.NewGuid(),
                    Event = "ORDER_COMPLETED",
                    ObjectId = "ord_topup_ai",
                    Disposition = RevolutWebhookClaimDispositions.Applied,
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

            await ambient.CommitAsync();

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
        public async Task Apply_Topup_EmailsInvoice_ToBillingEmail()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            account.BillingEmail = "invoices@venue.test";
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var emailDelivery = new RecordingInvoiceEmailDelivery();
            var applier = CreateApplier(context, mint, clock, invoiceEmail: emailDelivery);

            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_topup_email",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = "k_topup_email",
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
                    OrderId: "ord_topup_email",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Single(emailDelivery.Calls);
            Assert.True(emailDelivery.Calls[0].WasNewlyMinted);
            Assert.Equal(
                "ord_topup_email",
                emailDelivery.Calls[0].Invoice.RevolutOrderId
            );
        }

        [Fact]
        public async Task Apply_SetupIntent_ClearsPendingPayment()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_pending_clear", "sub_pending_clear");
            var account = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            account.PendingPaymentPlan = BillingSubscriptionPlans.Starter;
            account.PendingPaymentCadence = "monthly";
            account.PendingPaymentFailedAtUtc = _now;
            account.PendingPaymentFailureCode = "revolut_http_error";
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_pending_clear",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var after = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Starter, after.SubscriptionPlan);
            Assert.Null(after.PendingPaymentPlan);
            Assert.Null(after.PendingPaymentCadence);
            Assert.Null(after.PendingPaymentFailedAtUtc);
            Assert.Null(after.PendingPaymentFailureCode);
        }

        [Fact]
        public async Task Apply_SetupIntent_EmailsInvoice_OnceOnReplay()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_email_setup", "sub_email");
            var account = await context.BillingAccounts.SingleAsync();
            account.BillingEmail = null;
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var emailDelivery = new RecordingInvoiceEmailDelivery();
            var applier = CreateApplier(context, mint, clock, invoiceEmail: emailDelivery);

            var request = new RevolutOrderCompletedApplyRequest(
                OrderId: "ord_email_setup",
                OrderState: "completed",
                BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                SubscriptionId: pending.RevolutSubscriptionId,
                RawWebhookBody: "{}",
                RawOrderBody: "{}"
            );

            await applier.ApplyAsync(request);
            await applier.ApplyAsync(request);

            Assert.Equal(2, emailDelivery.Calls.Count);
            Assert.True(emailDelivery.Calls[0].WasNewlyMinted);
            Assert.False(emailDelivery.Calls[1].WasNewlyMinted);
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
        public async Task ApplyTopup_AlreadyMintedByOtherApply_ClosesIntentWithoutPostMintWork()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            AddOpenTopupIntent(context, account.RestaurantId, "ord_topup_other");
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = account.RestaurantId,
                    Channel = CreditChannels.Ai,
                    EntryType = CreditLedgerEntryTypes.TopupAllocation,
                    Quantity = 500,
                    SourcePaymentRef = "ord_topup_other",
                    CreatedAtUtc = _now,
                    ExpiresAtUtc = _now.AddMonths(12),
                }
            );
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(
                context,
                mint,
                clock,
                creditLedger: new FailingMintLedger()
            );

            await applier.ApplyAsync(TopupRequest("ord_topup_other"));

            Assert.False(
                await context.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_topup_other")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
            Assert.Equal(0, await context.TummlyVatInvoices.CountAsync());
            Assert.Equal(
                0,
                await context.RestaurantBillingActivities.CountAsync(row =>
                    row.Kind == BillingActivityKinds.TopupPurchased
                )
            );
        }

        [Fact]
        public async Task ApplyTopup_OtherDbUpdateOnMint_Rethrows()
        {
            await using var context = CreateContext();
            var account = await SeedActiveStarterAsync(context);
            AddOpenTopupIntent(context, account.RestaurantId, "ord_topup_fail");
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(
                context,
                mint,
                clock,
                creditLedger: new FailingMintLedger()
            );

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                applier.ApplyAsync(TopupRequest("ord_topup_fail"))
            );

            Assert.True(
                await context.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_topup_fail")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
        }

        private static RevolutOrderCompletedApplyRequest TopupRequest(string orderId)
        {
            return new RevolutOrderCompletedApplyRequest(
                OrderId: orderId,
                OrderState: "completed",
                BillingReason: null,
                SubscriptionId: null,
                RawWebhookBody: "{}",
                RawOrderBody: "{}"
            );
        }

        private static void AddOpenTopupIntent(
            ApplicationDbContext context,
            int restaurantId,
            string orderId
        )
        {
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    RestaurantId = restaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/topup",
                    IdempotencyKey = $"k_{orderId}",
                    IsOpen = true,
                    NetAmountMinor = 1500,
                    VatAmountMinor = 300,
                    GrossAmountMinor = 1800,
                    Channel = "ai",
                    Quantity = 500,
                    PackLookupKey = "tummly_ai_500_gbp_v3",
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
        }

        /// <summary>
        /// Mint fails with a DbUpdateException that is not a unique violation.
        /// </summary>
        private sealed class FailingMintLedger : NoOpCreditLedger
        {
            public override Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
                CreditLedgerMintTopupRequest request,
                CancellationToken cancellationToken = default
            )
            {
                throw new DbUpdateException("Mint failed.", (Exception?)null);
            }
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

        [Fact]
        public async Task Apply_CustomPlanCycle_MintsInvoice_WritesActivity_ClearsDunning()
        {
            await using var context = CreateContext();
            var (account, _) = await SeedPilotAccountAsync(context);
            account.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            account.BillingStatus = BillingStatuses.SoftLock;
            account.BillingCycle = BillingCycles.Monthly;
            account.RenewalDateUtc = _now.AddDays(-1);
            account.CustomPlanConfigJson = new CustomPlanConfig
            {
                CatalogPublicId = "cat",
                Cadence = "monthly",
                PeriodNetPence = 10_000,
                MonthlyNetPence = 10_000,
                Quantities = new CustomPlanQuantities
                {
                    Locations = 2,
                    TeamUsers = 2,
                    QrPlacements = 5,
                    Offers = 1,
                    EmailCredits = 500,
                    SmsCredits = 20,
                    AiCredits = 20,
                },
            }.ToJson();
            account.DunningEpisodeStartedAt = _now.AddDays(-11);
            account.DunningOutstandingOrderId = "ord_custom_cycle";
            account.DunningFiredSteps = "0,3,7,10";
            account.SoftLockEnteredAt = _now.AddDays(-1);
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_custom_cycle",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.CustomPlanCycle,
                    TargetPlan = BillingSubscriptionPlans.Custom,
                    TargetCadence = "monthly",
                    CheckoutUrl = "https://checkout.test/custom",
                    IdempotencyKey = "custom-cycle-test",
                    IsOpen = true,
                    NetAmountMinor = 10_000,
                    VatAmountMinor = 2_000,
                    GrossAmountMinor = 12_000,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_custom_cycle",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            await context.Entry(account).ReloadAsync();
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Null(account.DunningEpisodeStartedAt);
            Assert.Null(account.DunningOutstandingOrderId);
            Assert.Null(account.SoftLockEnteredAt);
            Assert.Equal(_now.AddDays(30), account.RenewalDateUtc);

            var invoice = Assert.Single(await context.TummlyVatInvoices.ToListAsync());
            Assert.Equal("ord_custom_cycle", invoice.RevolutOrderId);
            Assert.Equal(10_000, invoice.NetPence);

            var kinds = await context.RestaurantBillingActivities
                .Where(row => row.RestaurantId == account.RestaurantId)
                .Select(row => row.Kind)
                .ToListAsync();
            Assert.Contains(BillingActivityKinds.SubscriptionRenewed, kinds);
            Assert.Contains(BillingActivityKinds.InvoicePaid, kinds);
        }

        [Fact]
        public async Task Apply_CustomPlanCycle_FirstPay_WritesSubscriptionCreated()
        {
            await using var context = CreateContext();
            var (account, _) = await SeedPilotAccountAsync(context);
            account.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            account.BillingStatus = BillingStatuses.Active;
            account.BillingCycle = BillingCycles.Monthly;
            account.RenewalDateUtc = null;
            account.CustomPlanConfigJson = new CustomPlanConfig
            {
                CatalogPublicId = "cat",
                Cadence = "monthly",
                PeriodNetPence = 8_000,
                MonthlyNetPence = 8_000,
                Quantities = new CustomPlanQuantities
                {
                    Locations = 1,
                    TeamUsers = 2,
                    QrPlacements = 5,
                    Offers = 1,
                    EmailCredits = 500,
                    SmsCredits = 20,
                    AiCredits = 20,
                },
            }.ToJson();
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_custom_first",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.CustomPlanCycle,
                    TargetPlan = BillingSubscriptionPlans.Custom,
                    TargetCadence = "monthly",
                    CheckoutUrl = "https://checkout.test/custom",
                    IdempotencyKey = "custom-cycle-first",
                    IsOpen = true,
                    NetAmountMinor = 8_000,
                    VatAmountMinor = 0,
                    GrossAmountMinor = 8_000,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();

            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_custom_first",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var kinds = await context.RestaurantBillingActivities
                .Where(row => row.RestaurantId == account.RestaurantId)
                .Select(row => row.Kind)
                .ToListAsync();
            Assert.Contains(BillingActivityKinds.SubscriptionCreated, kinds);
            Assert.DoesNotContain(BillingActivityKinds.SubscriptionRenewed, kinds);
        }

        [Fact]
        public async Task Apply_SetupFromPilot_KeepsPilotStartAndEnd()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_keep", "sub_keep");
            var account = await context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == pending.RestaurantId
            );
            var pilotStart = _now.AddDays(-3);
            account.PilotStartedAtUtc = pilotStart;
            var pilotEnd = account.PilotPeriodEnd!.Value;
            var owner = await OwnerOfRestaurantAsync(context, pending.RestaurantId);
            owner.ActivationExpiresAt = pilotEnd;
            var member = await SeedActiveMemberAsync(
                context,
                pending.RestaurantId,
                pilotEnd
            );
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_keep",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var after = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Starter, after.SubscriptionPlan);
            Assert.Equal(pilotStart, after.PilotStartedAtUtc);
            Assert.Equal(pilotEnd, after.PilotPeriodEnd);
            var ownerAfter = await context.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == owner.Id);
            Assert.Null(ownerAfter.ActivationExpiresAt);
            var memberAfter = await context.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == member.Id);
            Assert.Null(memberAfter.ActivationExpiresAt);
        }

        [Fact]
        public async Task Apply_CustomFirstPaid_KeepsPilotHistory()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAccountAsync(context);
            var pilotStart = _now.AddDays(-3);
            var pilotEnd = account.PilotPeriodEnd!.Value;
            account.PilotStartedAtUtc = pilotStart;
            account.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            account.CustomPlanConfigJson = new CustomPlanConfig
            {
                CatalogPublicId = "cat",
                Cadence = "monthly",
                PeriodNetPence = 10_000,
                MonthlyNetPence = 10_000,
                Quantities = new CustomPlanQuantities
                {
                    Locations = 2,
                    TeamUsers = 2,
                    QrPlacements = 5,
                    Offers = 1,
                    EmailCredits = 500,
                    SmsCredits = 20,
                    AiCredits = 20,
                },
            }.ToJson();
            owner.ActivationExpiresAt = pilotEnd;
            var location = new RestaurantLocation
            {
                RestaurantId = account.RestaurantId,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.DigitalGuestLink,
                    Token = Guid.NewGuid().ToString("N")[..32],
                    Status = QrCodeStatus.Paused,
                    LinkName = "Operator paused",
                    NormalizedLinkName = "operator paused",
                    Channel = DigitalGuestLinkChannel.Email,
                    CreatedAt = _now,
                }
            );
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_custom_first",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.CustomPlanCycle,
                    TargetPlan = BillingSubscriptionPlans.Custom,
                    TargetCadence = "monthly",
                    CheckoutUrl = "https://checkout.test/custom-first",
                    IdempotencyKey = "custom-first-test",
                    IsOpen = true,
                    NetAmountMinor = 10_000,
                    VatAmountMinor = 2_000,
                    GrossAmountMinor = 12_000,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_custom_first",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var after = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == account.RestaurantId);
            Assert.Equal(pilotStart, after.PilotStartedAtUtc);
            Assert.Equal(pilotEnd, after.PilotPeriodEnd);
            var ownerAfter = await context.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == owner.Id);
            Assert.Null(ownerAfter.ActivationExpiresAt);
            var code = await context.QrCodes
                .AsNoTracking()
                .SingleAsync(qr => qr.RestaurantLocationId == location.Id);
            Assert.Equal(QrCodeStatus.Paused, code.Status);
            var sent = Assert.Single(email.PlanStartedCalls);
            Assert.Equal(BillingSubscriptionPlans.Custom, sent.PlanName);
        }

        [Fact]
        public async Task Apply_Setup_SendsPlanStartedOnce()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_once", "sub_once");
            var owner = await OwnerOfRestaurantAsync(context, pending.RestaurantId);
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);
            var setup = new RevolutOrderCompletedApplyRequest(
                OrderId: "ord_once",
                OrderState: "completed",
                BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                SubscriptionId: pending.RevolutSubscriptionId,
                RawWebhookBody: "{}",
                RawOrderBody: "{}"
            );

            await applier.ApplyAsync(setup);

            var sent = Assert.Single(email.PlanStartedCalls);
            Assert.Equal(owner.Email, sent.ToEmail);
            Assert.Equal(BillingSubscriptionPlans.Starter, sent.PlanName);
            Assert.Equal("Apply Pilot Venue", sent.RestaurantName);

            // Renewal on the same subscription. The pending session is closed, so
            // no plan-started. A redelivery of the setup order never reaches the
            // applier: the webhook claim replays it (see RevolutWebhookServiceTests).
            var renewalClock = new FixedTimeProvider(_now.AddMonths(1));
            var renewalApplier = CreateApplier(
                context,
                new IncludedPeriodMintService(context, _pricebook, renewalClock),
                renewalClock,
                emailService: email
            );
            await renewalApplier.ApplyAsync(
                setup with
                {
                    OrderId = "ord_once_renewal",
                    BillingReason = RevolutOrderCompletedApplier.CycleBilling,
                }
            );
            Assert.Single(email.PlanStartedCalls);
        }

        [Fact]
        public async Task Apply_Renewal_DoesNotTouchMemberPilotClock()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(
                context,
                "ord_renew_clock",
                "sub_renew_clock"
            );
            var setup = new RevolutOrderCompletedApplyRequest(
                OrderId: "ord_renew_clock",
                OrderState: "completed",
                BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                SubscriptionId: pending.RevolutSubscriptionId,
                RawWebhookBody: "{}",
                RawOrderBody: "{}"
            );
            var setupClock = new FixedTimeProvider(_now);
            await CreateApplier(
                context,
                new IncludedPeriodMintService(context, _pricebook, setupClock),
                setupClock
            ).ApplyAsync(setup);

            // A member of this restaurant holds a Pilot clock that another
            // restaurant also uses. A renewal must not change it.
            var otherClock = _now.AddDays(10);
            var member = await SeedActiveMemberAsync(
                context,
                pending.RestaurantId,
                otherClock
            );

            var renewalClock = new FixedTimeProvider(_now.AddMonths(1));
            await CreateApplier(
                context,
                new IncludedPeriodMintService(context, _pricebook, renewalClock),
                renewalClock
            ).ApplyAsync(
                setup with
                {
                    OrderId = "ord_renew_clock_2",
                    BillingReason = RevolutOrderCompletedApplier.CycleBilling,
                }
            );

            var after = await context.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == member.Id);
            Assert.Equal(otherClock, after.ActivationExpiresAt);
        }

        [Fact]
        public async Task Apply_Setup_OwnerEmailMissing_AppliesWithoutEmail()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_no_owner", "sub_no_owner");
            var owner = await OwnerOfRestaurantAsync(context, pending.RestaurantId);
            owner.Email = " ";
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_no_owner",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var account = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Empty(email.PlanStartedCalls);
        }

        [Fact]
        public async Task Apply_Setup_EmailSendFails_StillApplies()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_mail_fail", "sub_mail_fail");
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(
                context,
                mint,
                clock,
                emailService: new ThrowingPlanStartedEmailService()
            );

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_mail_fail",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var account = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == pending.RestaurantId);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            var pendingAfter = await context.RevolutPendingPaySessions
                .AsNoTracking()
                .SingleAsync(row => row.Id == pending.Id);
            Assert.False(pendingAfter.IsOpen);
        }

        [Fact]
        public async Task Apply_Topup_SendsNoPlanStarted()
        {
            await using var context = CreateContext();
            var (account, _) = await SeedPilotAccountAsync(context);
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_topup_plan",
                    RestaurantId = account.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/topup-plan",
                    IdempotencyKey = "k_topup_plan",
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
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_topup_plan",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Empty(email.PlanStartedCalls);
        }

        [Fact]
        public async Task Apply_ShopOrder_SendsNoPlanStarted()
        {
            await using var context = CreateContext();
            var (account, owner) = await SeedPilotAccountAsync(context);
            var location = new RestaurantLocation
            {
                RestaurantId = account.RestaurantId,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            var shopOrder = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "SHOP-PLAN-1",
                RestaurantId = account.RestaurantId,
                LocationId = location.Id,
                LocationNameSnapshot = "Main",
                PlacedByUserId = owner.Id,
                PlacedByNameSnapshot = "Apply Owner",
                MaterialsNetPence = 1000,
                VatPence = 200,
                DeliveryNetPence = 0,
                GrossPence = 1200,
                ShipToContactName = "Apply Owner",
                ShipToAddressLine1 = "1 High Street",
                ShipToPostcode = "AB1 2CD",
                CreatedAtUtc = _now,
                UpdatedAtUtc = _now,
            };
            context.ShopOrders.Add(shopOrder);
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_shop_plan",
                    RestaurantId = account.RestaurantId,
                    ShopOrderId = shopOrder.Id,
                    Purpose = RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/shop-plan",
                    IdempotencyKey = "k_shop_plan",
                    IsOpen = true,
                    NetAmountMinor = 1000,
                    VatAmountMinor = 200,
                    GrossAmountMinor = 1200,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_shop_plan",
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Empty(email.PlanStartedCalls);
        }

        [Fact]
        public async Task Apply_ShopOrder_Deferring_SendsNoEmailUntilPostCommitRuns()
        {
            await using var context = CreateContext();
            var seeded = await SeedAwaitingShopOrderAsync(context, "ord_shop_defer");
            var invoiceEmail = new RecordingInvoiceEmailDelivery();
            var shopEmail = new RecordingShopOrderEmail();
            var postCommit = new PostCommitActions(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PostCommitActions>.Instance
            );
            var clock = new FixedTimeProvider(_now);
            var applier = CreateApplier(
                context,
                new IncludedPeriodMintService(context, _pricebook, clock),
                clock,
                invoiceEmail: invoiceEmail,
                shopOrderEmail: shopEmail,
                postCommit: postCommit
            );

            postCommit.BeginDeferral();
            await applier.ApplyAsync(CompletedRequest("ord_shop_defer"));

            // Paid and invoice are written; nothing has gone out yet.
            Assert.Equal(
                ShopPaymentStatuses.Paid,
                (await context.ShopOrders.AsNoTracking().SingleAsync()).PaymentStatus
            );
            Assert.Single(await context.TummlyVatInvoices.ToListAsync());
            Assert.Empty(invoiceEmail.Calls);
            Assert.Empty(shopEmail.Confirmed);

            await postCommit.RunAsync();

            var call = Assert.Single(invoiceEmail.Calls);
            Assert.True(call.WasNewlyMinted);
            Assert.Equal(seeded.ShopOrderId, Assert.Single(shopEmail.Confirmed));
        }

        [Fact]
        public async Task Apply_ShopOrder_TransactionDoesNotCommit_SendsNoEmail()
        {
            await using var context = CreateContext();
            await SeedAwaitingShopOrderAsync(context, "ord_shop_rollback");
            var invoiceEmail = new RecordingInvoiceEmailDelivery();
            var shopEmail = new RecordingShopOrderEmail();
            var postCommit = new PostCommitActions(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PostCommitActions>.Instance
            );
            var clock = new FixedTimeProvider(_now);
            var applier = CreateApplier(
                context,
                new IncludedPeriodMintService(context, _pricebook, clock),
                clock,
                invoiceEmail: invoiceEmail,
                shopOrderEmail: shopEmail,
                postCommit: postCommit
            );

            postCommit.BeginDeferral();
            await applier.ApplyAsync(CompletedRequest("ord_shop_rollback"));
            // The outer transaction fails after the mint: the webhook discards.
            postCommit.Discard();
            await postCommit.RunAsync();

            Assert.Empty(invoiceEmail.Calls);
            Assert.Empty(shopEmail.Confirmed);
        }

        [Fact]
        public async Task Apply_ShopOrder_AlreadyPaidByWebhook_StaleTrackedRowsDoNotReplayThePaidWrite()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            await using var sweeperContext = new ApplicationDbContext(options);
            var seeded = await SeedAwaitingShopOrderAsync(sweeperContext, "ord_shop_stale");

            // The sweeper read the intent and order before the webhook paid them.
            var staleIntent = await sweeperContext.RevolutOrderIntents
                .SingleAsync(row => row.OrderId == "ord_shop_stale");
            var staleOrder = await sweeperContext.ShopOrders
                .Include(row => row.Lines)
                .SingleAsync();
            Assert.True(staleIntent.IsOpen);

            var webhookEmail = new RecordingInvoiceEmailDelivery();
            await using (var webhookContext = new ApplicationDbContext(options))
            {
                var clock = new FixedTimeProvider(_now);
                var webhookApplier = CreateApplier(
                    webhookContext,
                    new IncludedPeriodMintService(webhookContext, _pricebook, clock),
                    clock,
                    invoiceEmail: webhookEmail
                );
                await webhookApplier.ApplyAsync(CompletedRequest("ord_shop_stale"));
                var paid = await webhookContext.ShopOrders.SingleAsync();
                paid.FulfilmentStatus = ShopFulfilmentStatuses.InTransit;
                await webhookContext.SaveChangesAsync();
            }

            var sweeperInvoiceEmail = new RecordingInvoiceEmailDelivery();
            var sweeperShopEmail = new RecordingShopOrderEmail();
            var laterClock = new FixedTimeProvider(_now.AddHours(1));
            var sweeperApplier = CreateApplier(
                sweeperContext,
                new IncludedPeriodMintService(sweeperContext, _pricebook, laterClock),
                laterClock,
                invoiceEmail: sweeperInvoiceEmail,
                shopOrderEmail: sweeperShopEmail
            );

            await sweeperApplier.ApplyAsync(CompletedRequest("ord_shop_stale"));

            await using var verify = new ApplicationDbContext(options);
            var order = await verify.ShopOrders.AsNoTracking().SingleAsync();
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Equal(_now, order.PaidAtUtc);
            Assert.Equal(_now, order.ProcessingStartedAtUtc);
            Assert.Equal(ShopFulfilmentStatuses.InTransit, order.FulfilmentStatus);
            Assert.Empty(sweeperInvoiceEmail.Calls);
            Assert.Empty(sweeperShopEmail.Confirmed);
            Assert.Single(await verify.TummlyVatInvoices.ToListAsync());
            Assert.Equal(seeded.ShopOrderId, order.Id);
        }

        [Fact]
        public async Task Apply_Setup_ActivatesPausedQrWithinCap()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(
                context,
                "ord_qr",
                "sub_qr",
                BillingSubscriptionPlans.Growth
            );
            var account = await context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == pending.RestaurantId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Free;
            account.BillingStatus = BillingStatuses.Free;
            account.PilotPeriodEnd = null;
            var location = new RestaurantLocation
            {
                RestaurantId = pending.RestaurantId,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            var cap = _pricebook
                .GetRequired(account.ContractedPricebookId!)
                .Plans[BillingSubscriptionPlans.Growth.ToLowerInvariant()]
                .ActiveQrPlacementsPerLocation;
            for (var i = 1; i <= cap + 2; i++)
            {
                context.QrCodes.Add(
                    new QrCode
                    {
                        RestaurantLocationId = location.Id,
                        QrType = QrType.DigitalGuestLink,
                        Token = Guid.NewGuid().ToString("N")[..32],
                        Status = QrCodeStatus.Paused,
                        LinkName = $"Paused link {i}",
                        NormalizedLinkName = $"paused link {i}",
                        Channel = DigitalGuestLinkChannel.Email,
                        CreatedAt = _now.AddMinutes(i),
                    }
                );
            }
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_qr",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var codes = await context.QrCodes
                .AsNoTracking()
                .Where(qr => qr.RestaurantLocationId == location.Id)
                .ToListAsync();
            Assert.Equal(cap, codes.Count(qr => qr.Status == QrCodeStatus.Active));
            Assert.Equal(2, codes.Count(qr => qr.Status == QrCodeStatus.Paused));
        }

        [Fact]
        public async Task Apply_Setup_ClosedPaySession_ClearsClocksAndSendsPlanStartedOnce()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_closed", "sub_closed");
            pending.IsOpen = false;
            var pilotEnd = _now.AddDays(20);
            var owner = await OwnerOfRestaurantAsync(context, pending.RestaurantId);
            owner.ActivationExpiresAt = pilotEnd;
            var member = await SeedActiveMemberAsync(
                context,
                pending.RestaurantId,
                pilotEnd
            );
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_closed",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var ownerAfter = await context.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == owner.Id);
            Assert.Null(ownerAfter.ActivationExpiresAt);
            var memberAfter = await context.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == member.Id);
            Assert.Null(memberAfter.ActivationExpiresAt);
            var sent = Assert.Single(email.PlanStartedCalls);
            Assert.Equal(BillingSubscriptionPlans.Starter, sent.PlanName);
        }

        [Fact]
        public async Task Apply_Setup_OnPaidAccount_SendsNoPlanStarted()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(context, "ord_paid_setup", "sub_paid_setup");
            var account = await context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == pending.RestaurantId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.BillingStatus = BillingStatuses.Active;
            pending.IsOpen = false;
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var email = new RecordingPlanStartedEmailService();
            var applier = CreateApplier(context, mint, clock, emailService: email);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_paid_setup",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            Assert.Empty(email.PlanStartedCalls);
        }

        [Fact]
        public async Task Apply_Setup_FromPilot_KeepsOperatorPausedCodePaused()
        {
            await using var context = CreateContext();
            var pending = await SeedPilotPendingAsync(
                context,
                "ord_pilot_paused",
                "sub_pilot_paused",
                BillingSubscriptionPlans.Growth
            );
            var location = new RestaurantLocation
            {
                RestaurantId = pending.RestaurantId,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.DigitalGuestLink,
                    Token = Guid.NewGuid().ToString("N")[..32],
                    Status = QrCodeStatus.Paused,
                    LinkName = "Operator paused",
                    NormalizedLinkName = "operator paused",
                    Channel = DigitalGuestLinkChannel.Email,
                    CreatedAt = _now,
                }
            );
            await context.SaveChangesAsync();
            var clock = new FixedTimeProvider(_now);
            var mint = new IncludedPeriodMintService(context, _pricebook, clock);
            var applier = CreateApplier(context, mint, clock);

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: "ord_pilot_paused",
                    OrderState: "completed",
                    BillingReason: RevolutOrderCompletedApplier.SetupIntent,
                    SubscriptionId: pending.RevolutSubscriptionId,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            var code = await context.QrCodes
                .AsNoTracking()
                .SingleAsync(qr => qr.RestaurantLocationId == location.Id);
            Assert.Equal(QrCodeStatus.Paused, code.Status);
        }

        private async Task<User> SeedActiveMemberAsync(
            ApplicationDbContext context,
            int restaurantId,
            DateTime? activationExpiresAt
        )
        {
            var member = new User
            {
                Email = $"member-{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Apply Member",
                Role = "Member",
                CreatedAt = _now,
                ActivatedAt = activationExpiresAt?.AddDays(-30),
                ActivationExpiresAt = activationExpiresAt,
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = member.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = PermissionRoles.Owner,
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();
            return member;
        }

        private async Task<User> OwnerOfRestaurantAsync(
            ApplicationDbContext context,
            int restaurantId
        )
        {
            var ownerUserId = await context.Restaurants
                .Where(row => row.Id == restaurantId)
                .Select(row => row.OwnerUserId)
                .SingleAsync();
            return await context.Users.SingleAsync(row => row.Id == ownerUserId);
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
            IRevolutMerchantClient? merchant = null,
            ITummlyVatInvoiceEmailDelivery? invoiceEmail = null,
            TummlySellerVatSettings? sellerVat = null,
            IEmailService? emailService = null,
            ICreditLedger? creditLedger = null,
            IShopOrderEmailNotifier? shopOrderEmail = null,
            IPostCommitActions? postCommit = null
        )
        {
            var vat =
                sellerVat
                ?? new TummlySellerVatSettings
                {
                    IsActive = true,
                    RegistrationNumber = "GB123456789",
                    EffectiveDate = "2024-01-01",
                    LegalName = "Tummly Ltd",
                    RegisteredAddress = "1 High Street",
                };
            return new RevolutOrderCompletedApplier(
                context,
                mint,
                CreateVatService(context, vat),
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
                    clock,
                    Options.Create(vat)
                ),
                creditLedger
                    ?? new CreditLedgerService(
                        context,
                        clock,
                        _pricebook,
                        new AdminAuditService(context, clock)
                    ),
                merchant ?? new RecordingLandMerchant(),
                clock,
                Options.Create(vat),
                emailService ?? new EmailServiceStubBase(),
                CreateCaptureQrLifecycle(context),
                invoiceEmail,
                shopOrderEmail,
                postCommitActions: postCommit
            );
        }

        private CaptureQrLifecycleService CreateCaptureQrLifecycle(
            ApplicationDbContext context
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.test",
                    }
                )
                .Build();
            return new CaptureQrLifecycleService(
                context,
                new SmartGuestLinkService(
                    context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                _pricebook,
                null!
            );
        }

        private async Task<RevolutPendingPaySession> SeedPilotPendingAsync(
            ApplicationDbContext context,
            string setupOrderId,
            string subscriptionId,
            string targetPlan = BillingSubscriptionPlans.Starter
        )
        {
            var (account, _) = await SeedPilotAccountAsync(context);
            var pending = new RevolutPendingPaySession
            {
                Id = Guid.NewGuid(),
                RestaurantId = account.RestaurantId,
                TargetPlan = targetPlan,
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
            BillingCreditsService.ApplyPilotSignupBilling(account, _now);
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

        private TummlyVatInvoiceService CreateVatService(
            ApplicationDbContext context,
            TummlySellerVatSettings? sellerVat = null
        )
        {
            return new TummlyVatInvoiceService(
                context,
                _pricebook,
                Options.Create(
                    sellerVat
                        ?? new TummlySellerVatSettings
                        {
                            IsActive = true,
                            RegistrationNumber = "GB123456789",
                            EffectiveDate = "2024-01-01",
                            LegalName = "Tummly Ltd",
                            RegisteredAddress = "1 High Street",
                        }
                )
            );
        }

        private sealed class ThrowingPlanStartedEmailService : EmailServiceStubBase
        {
            public override Task SendPlanStartedEmailAsync(
                string toEmail,
                string firstName,
                string planName,
                string restaurantName
            ) => Task.FromException(new InvalidOperationException("smtp_down"));
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

        private static RevolutOrderCompletedApplyRequest CompletedRequest(
            string orderId
        ) => new(
            OrderId: orderId,
            OrderState: "completed",
            BillingReason: null,
            SubscriptionId: null,
            RawWebhookBody: "{}",
            RawOrderBody: "{}"
        );

        private async Task<(Guid ShopOrderId, int RestaurantId)> SeedAwaitingShopOrderAsync(
            ApplicationDbContext context,
            string revolutOrderId
        )
        {
            var (account, owner) = await SeedPilotAccountAsync(context);
            var location = new RestaurantLocation
            {
                RestaurantId = account.RestaurantId,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            var shopOrder = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"SHOP-{revolutOrderId}",
                RestaurantId = account.RestaurantId,
                LocationId = location.Id,
                LocationNameSnapshot = "Main",
                PlacedByUserId = owner.Id,
                PlacedByNameSnapshot = "Apply Owner",
                MaterialsNetPence = 1000,
                VatPence = 200,
                DeliveryNetPence = 0,
                GrossPence = 1200,
                ShipToContactName = "Apply Owner",
                ShipToAddressLine1 = "1 High Street",
                ShipToPostcode = "AB1 2CD",
                CreatedAtUtc = _now,
                UpdatedAtUtc = _now,
            };
            context.ShopOrders.Add(shopOrder);
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = revolutOrderId,
                    RestaurantId = account.RestaurantId,
                    ShopOrderId = shopOrder.Id,
                    Purpose = RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = "https://checkout.revolut.test/shop",
                    IdempotencyKey = $"k_{revolutOrderId}",
                    IsOpen = true,
                    NetAmountMinor = 1000,
                    VatAmountMinor = 200,
                    GrossAmountMinor = 1200,
                    CreatedAtUtc = _now,
                }
            );
            await context.SaveChangesAsync();
            return (shopOrder.Id, account.RestaurantId);
        }

        private sealed class RecordingShopOrderEmail : IShopOrderEmailNotifier
        {
            public List<Guid> Confirmed { get; } = [];

            public Task TryNotifyConfirmedAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            )
            {
                Confirmed.Add(shopOrderId);
                return Task.CompletedTask;
            }

            public Task TryNotifyDispatchedAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }

        private sealed class RecordingInvoiceEmailDelivery : ITummlyVatInvoiceEmailDelivery
        {
            public List<(TummlyVatInvoice Invoice, bool WasNewlyMinted)> Calls { get; } = [];

            public Task DeliverIfNewAsync(
                TummlyVatInvoice invoice,
                bool wasNewlyMinted,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((invoice, wasNewlyMinted));
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
