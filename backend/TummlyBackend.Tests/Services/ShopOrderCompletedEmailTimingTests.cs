using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// The webhook sends the invoice and the Shop confirmation email once, after
    /// its transaction commits, never while the billing lock is held.
    /// </summary>
    public class ShopOrderCompletedEmailTimingTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public ShopOrderCompletedEmailTimingTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task OrderCompletedWebhook_SendsEachEmailOnce_AfterCommit()
        {
            var invoiceEmail = new RecordingInvoiceEmail();
            var shopEmail = new RecordingShopEmail();
            using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ITummlyVatInvoiceEmailDelivery>();
                    services.AddSingleton<ITummlyVatInvoiceEmailDelivery>(invoiceEmail);
                    services.RemoveAll<IShopOrderEmailNotifier>();
                    services.AddSingleton<IShopOrderEmailNotifier>(shopEmail);
                })
            );
            using var client = factory.CreateClient();

            ShopCapacityWorkspace workspace;
            Guid orderId;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                workspace = await ShopCapacityTestSeed.SeedWorkspaceAsync(
                    db,
                    scope.ServiceProvider.GetRequiredService<IJwtService>()
                );
                // Table Tent already has an Active code, so no slot is involved.
                await ShopCapacityTestSeed.MakeOrderableAsync(db, workspace, "table-tents");
                orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                    db,
                    workspace,
                    "table-tents"
                );
            }

            using var pay = await client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );
            Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
            string revolutOrderId;
            using (var scope = factory.Services.CreateScope())
            {
                revolutOrderId = await scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>()
                    .RevolutOrderIntents
                    .Where(row => row.ShopOrderId == orderId)
                    .Select(row => row.OrderId)
                    .SingleAsync();
            }

            _factory.Merchant.Orders[revolutOrderId] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: revolutOrderId,
                State: "completed",
                AmountMinor: 1200,
                CheckoutUrl: FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                PaymentMethodSummary: "Paid"
            );
            var body = $$"""{"event":"ORDER_COMPLETED","order_id":"{{revolutOrderId}}"}""";
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/revolut")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation("Revolut-Request-Timestamp", "1710000000");
            request.Headers.TryAddWithoutValidation(
                "Revolut-Signature",
                RevolutWebhookSignature.SignForTests("whsec_placeholder", "1710000000", body)
            );

            using var response = await client.SendAsync(request);

            Assert.True(response.IsSuccessStatusCode);
            var invoiceCall = Assert.Single(invoiceEmail.Calls);
            Assert.True(invoiceCall.WasNewlyMinted);
            Assert.Equal(orderId, Assert.Single(shopEmail.Confirmed));
        }

        private sealed class RecordingInvoiceEmail : ITummlyVatInvoiceEmailDelivery
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

        private sealed class RecordingShopEmail : IShopOrderEmailNotifier
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
    }
}
