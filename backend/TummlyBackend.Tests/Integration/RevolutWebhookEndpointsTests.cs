using System.Net;
using System.Text;
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

        private static async Task<SeededPending> SeedPilotWithPendingAsync(
            RevolutWebhookWebApplicationFactory factory,
            string setupOrderId
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
                FullName = "Webhook Owner",
                Role = "Owner",
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

            return new SeededPending(restaurant.Id, pendingId, subscriptionId);
        }

        private sealed record SeededPending(
            int RestaurantId,
            Guid PendingId,
            string SubscriptionId
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

        public int GetOrderCallCount { get; private set; }

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

        public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
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
    }
}
