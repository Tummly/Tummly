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
        private const string SigningSecret =
            RevolutWebhookEndpointsTests_SigningSecret.Value;

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
                BillingReason: "setup_intent",
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
                            RevolutWebhookEndpointsTests_SigningSecret.Value,
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
    internal static class RevolutWebhookEndpointsTests_SigningSecret
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
