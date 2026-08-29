using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class RevolutWebhookEndpointsTests
    {
        private const string SigningSecret = WebhookTestSigningSecret.Value;

        [Fact]
        public async Task PostWebhook_BadSignature_Returns401_AndCreatesNoClaim()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_bad_sig");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/webhooks/revolut"
            )
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(
                "Revolut-Request-Timestamp",
                "1710000000"
            );
            request.Headers.TryAddWithoutValidation(
                "Revolut-Signature",
                "v1=deadbeef"
            );

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.RevolutWebhookEventClaims.CountAsync());
        }

        [Fact]
        public async Task PostWebhook_ReplaySameClaim_Returns204_WithoutSecondWrite()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            factory.Merchant.Orders["ord_replay"] = CompletedOrder("ord_replay");
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_replay");

            var first = await SendSignedAsync(client, body);
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await SendSignedAsync(client, body);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.RevolutWebhookEventClaims.CountAsync());
            Assert.Equal(
                1,
                await db.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "ORDER_COMPLETED"
                    && row.ObjectId == "ord_replay"
                )
            );
            Assert.Equal(1, factory.Merchant.GetOrderCallCount);
        }

        [Fact]
        public async Task PostWebhook_PrematureCompleted_Returns503_AndCreatesNoClaim()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            factory.Merchant.Orders["ord_pending"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_pending",
                State: "pending",
                RawBody: """{"id":"ord_pending","state":"pending"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_pending");

            var response = await SendSignedAsync(client, body);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.RevolutWebhookEventClaims.CountAsync());
        }

        [Fact]
        public async Task PostWebhook_TerminalNonCompleted_ClaimsSkippedTerminal_Returns204()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            factory.Merchant.Orders["ord_failed"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_failed",
                State: "failed",
                RawBody: """{"id":"ord_failed","state":"failed"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_failed");

            var response = await SendSignedAsync(client, body);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var claim = await db.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal("ORDER_COMPLETED", claim.Event);
            Assert.Equal("ord_failed", claim.ObjectId);
            Assert.Equal(
                RevolutWebhookClaimDispositions.SkippedTerminal,
                claim.Disposition
            );
        }

        [Fact]
        public async Task PostWebhook_SetupIntent_ActivatesOnce_AndMintsIncluded()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPilotWithPendingAsync(factory, "ord_setup_once");
            factory.Merchant.Orders["ord_setup_once"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_setup_once",
                State: "completed",
                BillingReason: "setup_intent",
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_setup_once","state":"completed"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_setup_once");

            var response = await SendSignedAsync(client, body);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Starter, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Active, account.BillingStatus);
            Assert.Equal(BillingCycles.Monthly, account.BillingCycle);
            Assert.NotNull(account.RenewalDateUtc);
            var claim = await db.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(RevolutWebhookClaimDispositions.Applied, claim.Disposition);
            Assert.True(
                await db.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                ) >= 1
            );
            Assert.False(
                await db.RevolutPendingPaySessions
                    .Where(row => row.Id == seeded.PendingId)
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task PostWebhook_SetupIntent_MintsTmInvoice_AndPatchesMerchantReference()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPilotWithPendingAsync(factory, "ord_tm_mint");
            factory.Merchant.Orders["ord_tm_mint"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_tm_mint",
                State: "completed",
                BillingReason: "setup_intent",
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_tm_mint","state":"completed"}"""
            );
            var client = factory.CreateClient();

            var response = await SendSignedAsync(
                client,
                OrderCompletedBody("ord_tm_mint")
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoice = await db.TummlyVatInvoices.SingleAsync(row =>
                row.RestaurantId == seeded.RestaurantId
            );
            Assert.StartsWith("TM-", invoice.DocumentNumber);
            Assert.Equal("ord_tm_mint", invoice.RevolutOrderId);
            Assert.DoesNotContain(
                invoice.DocumentNumber,
                "ord_tm_mint",
                StringComparison.Ordinal
            );
            Assert.Single(factory.Merchant.MerchantReferencePatches);
            Assert.Equal(
                ("ord_tm_mint", invoice.DocumentNumber),
                factory.Merchant.MerchantReferencePatches[0]
            );

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/billing-credits"
            );
            listRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                seeded.OwnerJwt
            );
            var listResponse = await client.SendAsync(listRequest);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            using var listBody = JsonDocument.Parse(
                await listResponse.Content.ReadAsStringAsync()
            );
            var invoices = listBody.RootElement.GetProperty("invoices");
            Assert.Equal(1, invoices.GetArrayLength());
            Assert.Equal(
                invoice.DocumentNumber,
                invoices[0].GetProperty("invoiceNo").GetString()
            );

            using var pdfRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/billing-credits/invoices/{invoice.DocumentNumber}/pdf"
            );
            pdfRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                seeded.OwnerJwt
            );
            var pdfResponse = await client.SendAsync(pdfRequest);
            Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
            Assert.Equal(
                "application/pdf",
                pdfResponse.Content.Headers.ContentType?.MediaType
            );
            var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
            Assert.StartsWith(
                "%PDF",
                System.Text.Encoding.ASCII.GetString(pdfBytes)
            );
        }

        [Fact]
        public async Task PostWebhook_SetupIntent_Replay_DoesNotMintSecondInvoice()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPilotWithPendingAsync(factory, "ord_tm_replay");
            factory.Merchant.Orders["ord_tm_replay"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_tm_replay",
                State: "completed",
                BillingReason: "setup_intent",
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_tm_replay","state":"completed"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_tm_replay");

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await db.TummlyVatInvoices.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                )
            );
            Assert.Single(factory.Merchant.MerchantReferencePatches);
        }

        [Fact]
        public async Task PostWebhook_SetupIntent_Replay_Returns204_WithoutSecondMint()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPilotWithPendingAsync(factory, "ord_setup_replay");
            factory.Merchant.Orders["ord_setup_replay"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_setup_replay",
                State: "completed",
                BillingReason: "setup_intent",
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_setup_replay","state":"completed"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_setup_replay");

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.RevolutWebhookEventClaims.CountAsync());
            var mintCount = await db.CreditLedgerEntries.CountAsync(row =>
                row.RestaurantId == seeded.RestaurantId
                && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
            );
            Assert.Equal(3, mintCount);
            Assert.Equal(1, factory.Merchant.GetOrderCallCount);
        }

        [Fact]
        public async Task PostWebhook_PlanUpgradeProration_AppliesOnce_ChangePlanAndTm()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidUpgradeIntentAsync(factory, "ord_up_once");
            factory.Merchant.Orders["ord_up_once"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_up_once",
                State: "completed",
                BillingReason: null,
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_up_once","state":"completed"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_up_once");

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, account.SubscriptionPlan);
            Assert.Equal(1, await db.RevolutWebhookEventClaims.CountAsync());
            Assert.Equal(
                RevolutWebhookClaimDispositions.Applied,
                (await db.RevolutWebhookEventClaims.SingleAsync()).Disposition
            );
            Assert.Equal(1, factory.Merchant.ChangeSubscriptionPlanCallCount);
            Assert.Equal(
                1,
                await db.TummlyVatInvoices.CountAsync(row =>
                    row.RevolutOrderId == "ord_up_once"
                )
            );
            Assert.False(
                await db.RevolutOrderIntents
                    .Where(row => row.OrderId == "ord_up_once")
                    .Select(row => row.IsOpen)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task PostWebhook_UnknownBillingReason_Skips_NoMint()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPilotWithPendingAsync(factory, "ord_unknown");
            factory.Merchant.Orders["ord_unknown"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_unknown",
                State: "completed",
                BillingReason: "weird_reason",
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_unknown","state":"completed"}"""
            );
            var client = factory.CreateClient();
            var body = OrderCompletedBody("ord_unknown");

            var response = await SendSignedAsync(client, body);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var claim = await db.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(
                RevolutWebhookClaimDispositions.SkippedUnknownBillingReason,
                claim.Disposition
            );
            var account = await db.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Pilot, account.BillingStatus);
            Assert.Equal(
                0,
                await db.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task PostWebhook_FinalSettlement_RecordsOnly_NoMint()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPilotWithPendingAsync(factory, "ord_final");
            factory.Merchant.Orders["ord_final"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_final",
                State: "completed",
                BillingReason: "final_settlement",
                SubscriptionId: seeded.SubscriptionId,
                RawBody: """{"id":"ord_final","state":"completed"}"""
            );
            var client = factory.CreateClient();

            var response = await SendSignedAsync(
                client,
                OrderCompletedBody("ord_final")
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var claim = await db.RevolutWebhookEventClaims.SingleAsync();
            Assert.Equal(
                RevolutWebhookClaimDispositions.Recorded,
                claim.Disposition
            );
            Assert.Equal(
                0,
                await db.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task PostWebhook_SubscriptionOverdue_StartsDunningOnce_AndPersistsOrderId()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_overdue_once",
                "ord_cycle_due"
            );
            factory.Merchant.Subscriptions["sub_overdue_once"] =
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: "sub_overdue_once",
                    State: "overdue",
                    CurrentCycleId: "cyc_1",
                    PaymentMethodId: "pm_card_1"
                );
            factory.Merchant.Cycles[("sub_overdue_once", "cyc_1")] =
                new RevolutSubscriptionCycleRetrieveResult(
                    Succeeded: true,
                    Id: "cyc_1",
                    OrderId: "ord_cycle_due"
                );
            factory.Merchant.Orders["ord_cycle_due"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_cycle_due",
                State: "pending",
                SubscriptionId: "sub_overdue_once"
            );
            var client = factory.CreateClient();
            var body =
                """{"event":"SUBSCRIPTION_OVERDUE","subscription_id":"sub_overdue_once"}""";

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, body)).StatusCode
            );

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);
            Assert.NotNull(account.DunningEpisodeStartedAt);
            Assert.Equal("ord_cycle_due", account.DunningOutstandingOrderId);
            Assert.Equal(
                1,
                await db.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "SUBSCRIPTION_OVERDUE"
                    && row.ObjectId == "sub_overdue_once"
                )
            );
            Assert.True(factory.Merchant.GetSubscriptionCallCount >= 1);
            Assert.Contains("ord_cycle_due", factory.Merchant.PayOrderIds);
        }

        [Fact]
        public async Task PostWebhook_OrderCompleted_WhileDunningOpen_RecoversEpisode()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_recover",
                "ord_cycle_recover"
            );
            await using (var seedScope = factory.Services.CreateAsyncScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var account = await db.BillingAccounts.SingleAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                );
                account.BillingStatus = BillingStatuses.PastDue;
                account.DunningEpisodeStartedAt = DateTime.UtcNow.AddDays(-1);
                account.DunningFiredSteps = "0";
                account.DunningOutstandingOrderId = "ord_cycle_recover";
                await db.SaveChangesAsync();
            }

            factory.Merchant.Orders["ord_cycle_recover"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_cycle_recover",
                State: "completed",
                BillingReason: "final_settlement",
                SubscriptionId: "sub_recover",
                RawBody: """{"id":"ord_cycle_recover","state":"completed"}"""
            );
            var client = factory.CreateClient();

            var response = await SendSignedAsync(
                client,
                OrderCompletedBody("ord_cycle_recover")
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var assertDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var recovered = await assertDb.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingStatuses.Active, recovered.BillingStatus);
            Assert.Null(recovered.DunningEpisodeStartedAt);
            Assert.Null(recovered.DunningFiredSteps);
            Assert.Null(recovered.DunningOutstandingOrderId);
        }

        [Fact]
        public async Task PostWebhook_SubscriptionCancelled_SyncOnly_LeavesDunningOpen()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_cancelled",
                "ord_cycle_cancel"
            );
            await using (var seedScope = factory.Services.CreateAsyncScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pastDue = await db.BillingAccounts.SingleAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                );
                pastDue.BillingStatus = BillingStatuses.PastDue;
                pastDue.DunningEpisodeStartedAt = DateTime.UtcNow.AddDays(-2);
                pastDue.DunningOutstandingOrderId = "ord_cycle_cancel";
                await db.SaveChangesAsync();
            }

            var client = factory.CreateClient();
            var body =
                """{"event":"SUBSCRIPTION_CANCELLED","subscription_id":"sub_cancelled"}""";

            var response = await SendSignedAsync(client, body);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var dbAssert = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbAssert.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);
            Assert.NotNull(account.DunningEpisodeStartedAt);
            Assert.Equal("ord_cycle_cancel", account.DunningOutstandingOrderId);
            Assert.Equal(
                1,
                await dbAssert.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "SUBSCRIPTION_CANCELLED"
                )
            );
            Assert.Equal(0, factory.Merchant.CancelSubscriptionCallCount);
        }

        [Fact]
        public async Task PostWebhook_SubscriptionFinished_SyncOnly_LeavesDunningOpen()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_finished",
                "ord_cycle_finished"
            );
            await using (var seedScope = factory.Services.CreateAsyncScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pastDue = await db.BillingAccounts.SingleAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                );
                pastDue.BillingStatus = BillingStatuses.PastDue;
                pastDue.DunningEpisodeStartedAt = DateTime.UtcNow.AddDays(-2);
                pastDue.DunningOutstandingOrderId = "ord_cycle_finished";
                await db.SaveChangesAsync();
            }

            var client = factory.CreateClient();
            var body =
                """{"event":"SUBSCRIPTION_FINISHED","subscription_id":"sub_finished"}""";

            var response = await SendSignedAsync(client, body);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var dbAssert = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbAssert.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);
            Assert.NotNull(account.DunningEpisodeStartedAt);
            Assert.Equal("ord_cycle_finished", account.DunningOutstandingOrderId);
            Assert.Equal(
                1,
                await dbAssert.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "SUBSCRIPTION_FINISHED"
                )
            );
            Assert.Equal(0, factory.Merchant.CancelSubscriptionCallCount);
        }

        [Fact]
        public async Task PostWebhook_SubscriptionOverdue_AfterRecover_StartsNewEpisode()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_overdue_again",
                "ord_cycle_again"
            );
            factory.Merchant.Subscriptions["sub_overdue_again"] =
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: "sub_overdue_again",
                    State: "overdue",
                    CurrentCycleId: "cyc_again",
                    PaymentMethodId: "pm_card_1"
                );
            factory.Merchant.Cycles[("sub_overdue_again", "cyc_again")] =
                new RevolutSubscriptionCycleRetrieveResult(
                    Succeeded: true,
                    Id: "cyc_again",
                    OrderId: "ord_cycle_again"
                );
            factory.Merchant.Orders["ord_cycle_again"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_cycle_again",
                State: "pending",
                SubscriptionId: "sub_overdue_again"
            );
            var client = factory.CreateClient();
            var overdueBody =
                """{"event":"SUBSCRIPTION_OVERDUE","subscription_id":"sub_overdue_again"}""";

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, overdueBody)).StatusCode
            );

            await using (var clearScope = factory.Services.CreateAsyncScope())
            {
                var db = clearScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var lifecycle = clearScope.ServiceProvider.GetRequiredService<IBillingAccountLifecycle>();
                await lifecycle.RecoverDunningAsync(
                    seeded.RestaurantId,
                    DateTime.UtcNow
                );
                var cleared = await db.BillingAccounts.AsNoTracking().SingleAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                );
                Assert.Null(cleared.DunningEpisodeStartedAt);
            }

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendSignedAsync(client, overdueBody)).StatusCode
            );

            await using var scope = factory.Services.CreateAsyncScope();
            var assertDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await assertDb.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingStatuses.PastDue, account.BillingStatus);
            Assert.NotNull(account.DunningEpisodeStartedAt);
            Assert.Equal("ord_cycle_again", account.DunningOutstandingOrderId);
            Assert.Equal(
                1,
                await assertDb.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "SUBSCRIPTION_OVERDUE"
                    && row.ObjectId == "sub_overdue_again"
                )
            );
        }

        [Fact]
        public async Task PostWebhook_OrderCompleted_PaymentMethodUpdate_WhileDunning_PaysNotRecover()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_pm_update",
                "ord_cycle_outstanding"
            );
            await using (var seedScope = factory.Services.CreateAsyncScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var account = await db.BillingAccounts.SingleAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                );
                account.BillingStatus = BillingStatuses.PastDue;
                account.DunningEpisodeStartedAt = DateTime.UtcNow.AddDays(-1);
                account.DunningFiredSteps = "0";
                account.DunningOutstandingOrderId = "ord_cycle_outstanding";
                await db.SaveChangesAsync();
            }

            factory.Merchant.Orders["ord_pm_update"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_pm_update",
                State: "completed",
                BillingReason: null,
                SubscriptionId: "sub_pm_update",
                RawBody: """{"id":"ord_pm_update","state":"completed"}"""
            );
            factory.Merchant.Orders["ord_cycle_outstanding"] =
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: "ord_cycle_outstanding",
                    State: "pending",
                    SubscriptionId: "sub_pm_update"
                );
            factory.Merchant.Subscriptions["sub_pm_update"] =
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: "sub_pm_update",
                    State: "overdue",
                    CurrentCycleId: "cyc_pm",
                    PaymentMethodId: "pm_new"
                );
            var client = factory.CreateClient();
            var payCountBefore = factory.Merchant.PayOrderCallCount;

            var response = await SendSignedAsync(
                client,
                OrderCompletedBody("ord_pm_update")
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var assertDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stillOpen = await assertDb.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(BillingStatuses.PastDue, stillOpen.BillingStatus);
            Assert.NotNull(stillOpen.DunningEpisodeStartedAt);
            Assert.Equal("ord_cycle_outstanding", stillOpen.DunningOutstandingOrderId);
            Assert.True(factory.Merchant.PayOrderCallCount > payCountBefore);
            Assert.Contains("ord_cycle_outstanding", factory.Merchant.PayOrderIds);
        }

        [Fact]
        public async Task PostWebhook_DisputeActionRequired_DrainsTopupOnce()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_disp_topup",
                "ord_setup_disp_topup"
            );
            const string paymentOrderId = "ord_topup_disp";
            const string disputeId = "disp_topup_once";
            await SeedTopupAllocationAsync(
                factory,
                seeded.RestaurantId,
                paymentOrderId,
                quantity: 100
            );
            factory.Merchant.Disputes[disputeId] = new RevolutDisputeRetrieveResult(
                Succeeded: true,
                Id: disputeId,
                PaymentOrderId: paymentOrderId,
                AmountMinor: 1200,
                Currency: "GBP"
            );
            var client = factory.CreateClient();
            var body = DisputeBody("DISPUTE_ACTION_REQUIRED", disputeId);

            var first = await SendSignedAsync(client, body);
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await SendSignedAsync(client, body);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts.AsNoTracking().SingleAsync(row =>
                row.RestaurantId == seeded.RestaurantId
            );
            Assert.True(account.ChargebackRestricted);
            Assert.Equal(
                1,
                await db.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Refund
                    && row.SourcePaymentRef == paymentOrderId
                    && row.CorrectionSource == CorrectionSources.Dispute
                )
            );
            Assert.Equal(
                1,
                await db.RevolutWebhookEventClaims.CountAsync(row =>
                    row.Event == "DISPUTE_ACTION_REQUIRED"
                    && row.ObjectId == disputeId
                )
            );
            Assert.Equal(1, factory.Merchant.GetDisputeCallCount);
        }

        [Fact]
        public async Task PostWebhook_DisputeActionRequired_PlanInvoice_OpensOverlayOnly_NoIncludedRefund()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_disp_plan",
                "ord_setup_disp_plan"
            );
            const string paymentOrderId = "ord_cycle_disp_plan";
            const string disputeId = "disp_plan_only";
            await SeedIncludedAllocationAsync(
                factory,
                seeded.RestaurantId,
                quantity: 500
            );
            await SeedTmInvoiceAsync(
                factory,
                seeded.RestaurantId,
                paymentOrderId
            );
            factory.Merchant.Disputes[disputeId] = new RevolutDisputeRetrieveResult(
                Succeeded: true,
                Id: disputeId,
                PaymentOrderId: paymentOrderId
            );
            var client = factory.CreateClient();

            var response = await SendSignedAsync(
                client,
                DisputeBody("DISPUTE_ACTION_REQUIRED", disputeId)
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts.AsNoTracking().SingleAsync(row =>
                row.RestaurantId == seeded.RestaurantId
            );
            Assert.True(account.ChargebackRestricted);
            Assert.Equal(
                0,
                await db.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Refund
                )
            );
            Assert.Equal(
                1,
                await db.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                )
            );
        }

        [Fact]
        public async Task PostWebhook_DisputeWon_RestoresTopupAndClearsOverlay()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_disp_won",
                "ord_setup_disp_won"
            );
            const string paymentOrderId = "ord_topup_won";
            const string disputeId = "disp_won";
            await SeedTopupAllocationAsync(
                factory,
                seeded.RestaurantId,
                paymentOrderId,
                quantity: 80
            );
            factory.Merchant.Disputes[disputeId] = new RevolutDisputeRetrieveResult(
                Succeeded: true,
                Id: disputeId,
                PaymentOrderId: paymentOrderId
            );
            var client = factory.CreateClient();

            Assert.Equal(
                HttpStatusCode.NoContent,
                (
                    await SendSignedAsync(
                        client,
                        DisputeBody("DISPUTE_ACTION_REQUIRED", disputeId)
                    )
                ).StatusCode
            );

            var won = await SendSignedAsync(
                client,
                DisputeBody("DISPUTE_WON", disputeId)
            );
            Assert.Equal(HttpStatusCode.NoContent, won.StatusCode);

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts.AsNoTracking().SingleAsync(row =>
                row.RestaurantId == seeded.RestaurantId
            );
            Assert.False(account.ChargebackRestricted);
            Assert.Equal(
                0,
                await db.TummlyVatInvoices.CountAsync(row =>
                    row.DocumentPrefix == TummlyDocumentSequence.PrefixTcn
                )
            );
            Assert.True(
                await db.CreditLedgerEntries.AnyAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.SourcePaymentRef == paymentOrderId
                    && row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                )
            );
        }

        [Fact]
        public async Task PostWebhook_DisputeLost_MintsTcn()
        {
            await using var factory = new RevolutWebhookWebApplicationFactory();
            var seeded = await SeedPaidWithSubscriptionAsync(
                factory,
                "sub_disp_lost",
                "ord_setup_disp_lost"
            );
            const string paymentOrderId = "ord_topup_lost";
            const string disputeId = "disp_lost_tcn";
            await SeedTopupAllocationAsync(
                factory,
                seeded.RestaurantId,
                paymentOrderId,
                quantity: 50
            );
            await SeedTmInvoiceAsync(
                factory,
                seeded.RestaurantId,
                paymentOrderId,
                netPence: 1000,
                vatPence: 200,
                grossPence: 1200
            );
            factory.Merchant.Disputes[disputeId] = new RevolutDisputeRetrieveResult(
                Succeeded: true,
                Id: disputeId,
                PaymentOrderId: paymentOrderId,
                AmountMinor: 1200
            );
            var client = factory.CreateClient();

            Assert.Equal(
                HttpStatusCode.NoContent,
                (
                    await SendSignedAsync(
                        client,
                        DisputeBody("DISPUTE_ACTION_REQUIRED", disputeId)
                    )
                ).StatusCode
            );

            var lost = await SendSignedAsync(
                client,
                DisputeBody("DISPUTE_LOST", disputeId)
            );
            Assert.Equal(HttpStatusCode.NoContent, lost.StatusCode);

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tcn = await db.TummlyVatInvoices.SingleAsync(row =>
                row.DocumentPrefix == TummlyDocumentSequence.PrefixTcn
            );
            Assert.Equal(disputeId, tcn.RevolutOrderId);
            Assert.StartsWith("TCN-", tcn.DocumentNumber);
            Assert.Equal(
                TummlyVatInvoice.PaymentStatusCreditNote,
                tcn.PaymentStatus
            );

            var replay = await SendSignedAsync(
                client,
                DisputeBody("DISPUTE_LOST", disputeId)
            );
            Assert.Equal(HttpStatusCode.NoContent, replay.StatusCode);
            Assert.Equal(
                1,
                await db.TummlyVatInvoices.CountAsync(row =>
                    row.DocumentPrefix == TummlyDocumentSequence.PrefixTcn
                )
            );
        }

        private static async Task SeedTopupAllocationAsync(
            RevolutWebhookWebApplicationFactory factory,
            int restaurantId,
            string paymentOrderId,
            int quantity
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var ledger = scope.ServiceProvider.GetRequiredService<ICreditLedger>();
            var result = await ledger.MintTopupAllocationAsync(
                new CreditLedgerMintTopupRequest
                {
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Email,
                    Quantity = quantity,
                    SourcePaymentRef = paymentOrderId,
                }
            );
            Assert.True(result.Succeeded, result.Code);
        }

        private static async Task SeedIncludedAllocationAsync(
            RevolutWebhookWebApplicationFactory factory,
            int restaurantId,
            int quantity
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var allocationId = Guid.NewGuid();
            db.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = allocationId,
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                    Quantity = quantity,
                    AllocationId = allocationId,
                    CreatedAtUtc = now,
                    PeriodStartUtc = now,
                    ExpiresAtUtc = now.AddMonths(1),
                }
            );
            await db.SaveChangesAsync();
        }

        private static async Task SeedTmInvoiceAsync(
            RevolutWebhookWebApplicationFactory factory,
            int restaurantId,
            string revolutOrderId,
            int netPence = 5000,
            int vatPence = 1000,
            int grossPence = 6000
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            db.TummlyVatInvoices.Add(
                new TummlyVatInvoice
                {
                    Id = Guid.NewGuid(),
                    DocumentNumber = $"TM-2026-{Guid.NewGuid().ToString("N")[..6]}",
                    DocumentPrefix = TummlyDocumentSequence.PrefixTm,
                    RevolutOrderId = revolutOrderId,
                    RestaurantId = restaurantId,
                    InvoiceDateUtc = now,
                    TaxPointUtc = now,
                    LineDescription = "Plan invoice",
                    Quantity = 1,
                    NetPence = netPence,
                    VatRateBps = 2000,
                    VatPence = vatPence,
                    GrossPence = grossPence,
                    Currency = TummlyVatInvoice.CurrencyGbp,
                    PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                    CustomerBusinessName = "Test",
                    SellerLegalName = "Tummly Ltd",
                    SellerRegisteredAddress = "1 Example Road",
                    SellerVatRegistrationNumber = "GB123456789",
                }
            );
            await db.SaveChangesAsync();
        }

        private static async Task<SeededPending> SeedPilotWithPendingAsync(
            RevolutWebhookWebApplicationFactory factory,
            string setupOrderId
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pricebook = scope.ServiceProvider.GetRequiredService<IPricebookCatalog>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            var now = DateTime.UtcNow;
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Webhook Owner",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
                CreatedAt = now,
            };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Webhook Pilot Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = now,
            };
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;
            db.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = now,
                }
            );
            db.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    RestaurantId = restaurant.Id,
                    UserId = owner.Id,
                    PermissionRole = PermissionRoles.Owner,
                    Status = MembershipStatus.Active,
                }
            );

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                pricebook.CurrentPricebookId
            );
            db.BillingAccounts.Add(billing);

            var subscriptionId = $"sub_{setupOrderId}";
            var pendingId = Guid.NewGuid();
            db.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = pendingId,
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Starter,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = subscriptionId,
                    SetupOrderId = setupOrderId,
                    CheckoutUrl = "https://checkout.revolut.test/pay",
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    IsOpen = true,
                    CreatedAtUtc = now,
                }
            );
            await db.SaveChangesAsync();

            var ownerJwt = jwtService.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );
            return new SeededPending(
                restaurant.Id,
                pendingId,
                subscriptionId,
                ownerJwt
            );
        }

        private static async Task<SeededPending> SeedPaidUpgradeIntentAsync(
            RevolutWebhookWebApplicationFactory factory,
            string orderId
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pricebook = scope.ServiceProvider.GetRequiredService<IPricebookCatalog>();
            var now = DateTime.UtcNow;
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Upgrade Webhook Owner",
                Role = "Owner",
                CreatedAt = now,
            };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Webhook Upgrade Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = now,
            };
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                pricebook.CurrentPricebookId
            );
            billing.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            billing.BillingStatus = BillingStatuses.Active;
            billing.BillingCycle = BillingCycles.Monthly;
            billing.RevolutCustomerId = "cust_webhook_up";
            billing.RenewalDateUtc = now.Date.AddDays(15);
            db.BillingAccounts.Add(billing);

            var subscriptionId = $"sub_{orderId}";
            db.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    TargetPlan = BillingSubscriptionPlans.Starter,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = subscriptionId,
                    SetupOrderId = $"ord_setup_{orderId}",
                    CheckoutUrl = "https://checkout.revolut.test/old",
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    IsOpen = false,
                    CreatedAtUtc = now.AddDays(-10),
                }
            );
            var intentId = Guid.NewGuid();
            db.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = intentId,
                    OrderId = orderId,
                    RestaurantId = restaurant.Id,
                    Purpose = RevolutOrderIntentPurposes.PlanUpgradeProration,
                    TargetPlan = BillingSubscriptionPlans.Growth,
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = subscriptionId,
                    CheckoutUrl = "https://checkout.revolut.test/up",
                    IdempotencyKey = Guid.NewGuid().ToString("D"),
                    IsOpen = true,
                    NetAmountMinor = 3000,
                    VatAmountMinor = 600,
                    GrossAmountMinor = 3600,
                    CreatedAtUtc = now,
                }
            );
            await db.SaveChangesAsync();

            return new SeededPending(
                restaurant.Id,
                intentId,
                subscriptionId,
                OwnerJwt: string.Empty
            );
        }

        private static async Task<SeededPending> SeedPaidWithSubscriptionAsync(
            RevolutWebhookWebApplicationFactory factory,
            string subscriptionId,
            string setupOrderId
        )
        {
            var seeded = await SeedPilotWithPendingAsync(factory, setupOrderId);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await db.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == seeded.RestaurantId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Starter;
            account.BillingCycle = BillingCycles.Monthly;
            account.BillingStatus = BillingStatuses.Active;
            account.RevolutCustomerId = $"cust_{subscriptionId}";
            account.RenewalDateUtc = DateTime.UtcNow.AddMonths(1);
            var pending = await db.RevolutPendingPaySessions.SingleAsync(row =>
                row.Id == seeded.PendingId
            );
            pending.RevolutSubscriptionId = subscriptionId;
            pending.IsOpen = false;
            await db.SaveChangesAsync();
            return seeded with { SubscriptionId = subscriptionId };
        }

        private sealed record SeededPending(
            int RestaurantId,
            Guid PendingId,
            string SubscriptionId,
            string OwnerJwt
        );

        private static async Task<HttpResponseMessage> SendSignedAsync(
            HttpClient client,
            string body
        )
        {
            const string timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                SigningSecret,
                timestamp,
                body
            );
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/webhooks/revolut"
            )
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(
                "Revolut-Request-Timestamp",
                timestamp
            );
            request.Headers.TryAddWithoutValidation(
                "Revolut-Signature",
                signature
            );
            return await client.SendAsync(request);
        }

        private static string OrderCompletedBody(string orderId)
        {
            return $$"""{"event":"ORDER_COMPLETED","order_id":"{{orderId}}"}""";
        }

        private static string DisputeBody(string eventName, string disputeId)
        {
            return $$"""{"event":"{{eventName}}","dispute_id":"{{disputeId}}"}""";
        }

        private static RevolutOrderRetrieveResult CompletedOrder(string orderId)
        {
            return new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: orderId,
                State: "completed",
                BillingReason: null,
                RawBody: $$"""{"id":"{{orderId}}","state":"completed"}"""
            );
        }
    }

    internal sealed class RevolutWebhookWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public StubRevolutMerchantClient Merchant { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Revolut:WebhookSigningSecret"] =
                            WebhookTestSigningSecret.Value,
                        ["Revolut:SecretKey"] = "sk_test_placeholder",
                        ["Revolut:ApiBaseUrl"] =
                            RevolutSettings.SandboxApiBaseUrl,
                        ["Revolut:ApiVersion"] =
                            RevolutSettings.DefaultApiVersion,
                        [TummlySellerVatSettings.RegistrationNumberKey] =
                            "GB123456789",
                        [TummlySellerVatSettings.EffectiveDateKey] =
                            "2024-01-01",
                        [TummlySellerVatSettings.LegalNameKey] = "Tummly Ltd",
                        [TummlySellerVatSettings.RegisteredAddressKey] =
                            "1 Example Road, London",
                    }
                );
            });

            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d =>
                        d.ServiceType
                            == typeof(DbContextOptions<ApplicationDbContext>)
                        || d.ServiceType == typeof(ApplicationDbContext)
                    )
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(w =>
                        w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    );
                });

                services.RemoveAll<IRevolutMerchantClient>();
                services.AddSingleton<IRevolutMerchantClient>(Merchant);
            });
        }
    }

    /// <summary>Holds the signing secret used by the factory config.</summary>
    internal static class WebhookTestSigningSecret
    {
        public const string Value = "whsec_test_ticket_15";
    }

    internal sealed class StubRevolutMerchantClient : IRevolutMerchantClient
    {
        public Dictionary<string, RevolutOrderRetrieveResult> Orders { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, RevolutDisputeRetrieveResult> Disputes { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, RevolutSubscriptionRetrieveResult> Subscriptions { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<
            (string SubscriptionId, string CycleId),
            RevolutSubscriptionCycleRetrieveResult
        > Cycles { get; } = new();

        public int GetOrderCallCount { get; private set; }

        public int GetDisputeCallCount { get; private set; }

        public int GetSubscriptionCallCount { get; private set; }

        public int PayOrderCallCount { get; private set; }

        public int CancelSubscriptionCallCount { get; private set; }

        public List<string> PayOrderIds { get; } = [];

        public void EnsureReadyForCreate(string? planVariationLookupKey = null)
        {
        }

        public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
            string email,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
            RevolutCreateSubscriptionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public Task<RevolutMerchantCreateResult> CreateOrderAsync(
            RevolutCreateOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public int ChangeSubscriptionPlanCallCount { get; private set; }

        public string? LastChangePlanSubscriptionId { get; private set; }

        public string? LastChangePlanLookupKey { get; private set; }

        public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        )
        {
            ChangeSubscriptionPlanCallCount++;
            LastChangePlanSubscriptionId = subscriptionId;
            LastChangePlanLookupKey = planVariationLookupKey;
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
        )
        {
            CancelSubscriptionCallCount++;
            return Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
            );
        }

        public Task<RevolutOrderRetrieveResult> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        )
        {
            GetOrderCallCount++;
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

        public Task<RevolutDisputeRetrieveResult> GetDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        )
        {
            GetDisputeCallCount++;
            if (Disputes.TryGetValue(disputeId, out var dispute))
            {
                return Task.FromResult(dispute);
            }

            return Task.FromResult(
                new RevolutDisputeRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_found"
                )
            );
        }

        public List<(string OrderId, string Reference)> MerchantReferencePatches
        { get; } = [];

        public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
            string orderId,
            string merchantReference,
            CancellationToken cancellationToken = default
        )
        {
            MerchantReferencePatches.Add((orderId, merchantReference));
            return Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
            );
        }

        public Task<RevolutSubscriptionRetrieveResult> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        )
        {
            GetSubscriptionCallCount++;
            if (Subscriptions.TryGetValue(subscriptionId, out var subscription))
            {
                return Task.FromResult(subscription);
            }

            return Task.FromResult(
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_found"
                )
            );
        }

        public Task<RevolutSubscriptionCycleRetrieveResult> GetSubscriptionCycleAsync(
            string subscriptionId,
            string cycleId,
            CancellationToken cancellationToken = default
        )
        {
            if (Cycles.TryGetValue((subscriptionId, cycleId), out var cycle))
            {
                return Task.FromResult(cycle);
            }

            return Task.FromResult(
                new RevolutSubscriptionCycleRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_found"
                )
            );
        }

        public Task<RevolutMerchantCreateResult> PayOrderAsync(
            RevolutPayOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            PayOrderCallCount++;
            PayOrderIds.Add(request.OrderId);
            return Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: request.OrderId)
            );
        }
    }
}
