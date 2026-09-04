using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RevolutMerchantClientTests
    {
        [Fact]
        public async Task CreateCustomerAsync_DoesNotCallHttp_WhenVatNotReady()
        {
            var handler = new CountingHandler();
            var client = CreateClient(
                handler,
                vat: new TummlySellerVatSettings(),
                revolut: FullRevolut()
            );

            var ex = await Assert.ThrowsAsync<RevolutMerchantNotReadyException>(
                () =>
                    client.CreateCustomerAsync(
                        new RevolutCreateCustomerRequest("a@example.com")
                    )
            );

            Assert.Equal(RevolutMerchantCreateGate.VatNotReady, ex.Code);
            Assert.Equal(0, handler.SendCount);
        }

        [Fact]
        public async Task CreateSubscriptionAsync_DoesNotCallHttp_WhenVariationMissing()
        {
            var handler = new CountingHandler();
            var revolut = FullRevolut();
            revolut.PlanVariations.Clear();
            var client = CreateClient(handler, FullVat(), revolut);

            var ex = await Assert.ThrowsAsync<RevolutMerchantNotReadyException>(
                () =>
                    client.CreateSubscriptionAsync(
                        new RevolutCreateSubscriptionRequest(
                            "cust_1",
                            RevolutPlanVariationKeys.StarterMonthly
                        )
                    )
            );

            Assert.Equal(
                RevolutMerchantCreateGate.PlanVariationMissing,
                ex.Code
            );
            Assert.Equal(0, handler.SendCount);
        }

        [Fact]
        public async Task CreateOrderAsync_CallsHttp_WhenOnlyVatAndRevolutReady()
        {
            var handler = new CountingHandler
            {
                ResponseFactory = () =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            """{"id":"ord_1"}"""
                        ),
                    },
            };
            var revolut = FullRevolut();
            revolut.PlanVariations.Clear();
            var client = CreateClient(handler, FullVat(), revolut);

            var result = await client.CreateOrderAsync(
                new RevolutCreateOrderRequest(100, "GBP")
            );

            Assert.True(result.Succeeded);
            Assert.Equal("ord_1", result.Id);
            Assert.Equal(1, handler.SendCount);
        }

        [Fact]
        public async Task CreateOrderAsync_PostsModernOrdersApi_WithRedirectAndQuantityObject()
        {
            HttpRequestMessage? captured = null;
            string? body = null;
            var handler = new CountingHandler
            {
                ResponseFactory = () =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            """{"id":"ord_topup","redirect_url":"https://qa.tummly.com/return","checkout_url":"https://checkout.example/x"}"""
                        ),
                    },
                OnSend = request =>
                {
                    captured = request;
                    body = request.Content is null
                        ? null
                        : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                },
            };
            var revolut = FullRevolut();
            revolut.PlanVariations.Clear();
            var client = CreateClient(handler, FullVat(), revolut);

            var result = await client.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: 1800,
                    Currency: "GBP",
                    RedirectUrl: "https://qa.tummly.com/single-dashboard/settings/billing-credits?tab=credits-usage&topUpOutcome=success",
                    Description: "AI credit pack (500)",
                    LineItems:
                    [
                        new RevolutOrderLineItem(
                            Name: "AI credit pack (500)",
                            UnitPriceAmount: 1500,
                            Quantity: 1,
                            TotalAmount: 1800,
                            Taxes:
                            [
                                new RevolutOrderLineItemTax(
                                    Name: "VAT",
                                    Percentage: "20.00",
                                    Amount: 300
                                ),
                            ],
                            ExternalId: "tummly_ai_500_gbp_v3"
                        ),
                    ]
                )
            );

            Assert.True(result.Succeeded);
            Assert.NotNull(captured);
            Assert.EndsWith(
                "/api/orders",
                captured!.RequestUri!.AbsolutePath
            );
            Assert.DoesNotContain("/api/1.0/orders", captured.RequestUri!.AbsolutePath);
            Assert.Contains("redirect_url", body);
            Assert.Contains("topUpOutcome=success", body);
            Assert.Contains("\"quantity\":{\"value\":1}", body!.Replace(" ", ""));
            Assert.Contains("\"type\":\"service\"", body.Replace(" ", ""));
        }

        [Fact]
        public async Task GetOrderAsync_UsesOrdersApi_AndParsesSubscriptionData()
        {
            HttpRequestMessage? captured = null;
            var handler = new CountingHandler
            {
                ResponseFactory = () =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            """
                            {
                              "id":"ord_1",
                              "state":"completed",
                              "type":"payment",
                              "amount":4680,
                              "checkout_url":"https://checkout.example/link",
                              "subscription_data":{
                                "subscription_id":"sub_1",
                                "billing_reason":"cycle_billing"
                              }
                            }
                            """
                        ),
                    },
                OnSend = request => captured = request,
            };
            var client = CreateClient(handler, FullVat(), FullRevolut());

            var result = await client.GetOrderAsync("ord_1");

            Assert.True(result.Succeeded);
            Assert.Equal("ord_1", result.Id);
            Assert.Equal("completed", result.State);
            Assert.Equal("cycle_billing", result.BillingReason);
            Assert.Equal("sub_1", result.SubscriptionId);
            Assert.Equal(4680, result.AmountMinor);
            Assert.Equal(
                "https://checkout.example/link",
                result.CheckoutUrl
            );
            Assert.NotNull(captured);
            Assert.EndsWith(
                "/api/orders/ord_1",
                captured!.RequestUri!.AbsolutePath
            );
        }

        private static IRevolutMerchantClient CreateClient(
            CountingHandler handler,
            TummlySellerVatSettings vat,
            RevolutSettings revolut
        )
        {
            var services = new ServiceCollection();
            services.AddSingleton(Options.Create(vat));
            services.AddSingleton(Options.Create(revolut));
            services.AddSingleton<IRevolutMerchantCreateGate, RevolutMerchantCreateGate>();
            services.AddSingleton<IRevolutMerchantClient, RevolutMerchantClient>();
            services
                .AddHttpClient(RevolutMerchantClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(
                        revolut.ApiBaseUrl.TrimEnd('/') + "/"
                    );
                });

            return services.BuildServiceProvider()
                .GetRequiredService<IRevolutMerchantClient>();
        }

        private static TummlySellerVatSettings FullVat()
        {
            return new TummlySellerVatSettings
            {
                RegistrationNumber = "GB123",
                EffectiveDate = "2024-01-01",
                LegalName = "Tummly Ltd",
                RegisteredAddress = "1 High Street",
            };
        }

        private static RevolutSettings FullRevolut()
        {
            return new RevolutSettings
            {
                SecretKey = "sk_test",
                WebhookSigningSecret = "whsec",
                ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                ApiVersion = RevolutSettings.DefaultApiVersion,
                PlanVariations = new Dictionary<string, string>
                {
                    [RevolutPlanVariationKeys.StarterMonthly] =
                        "11111111-1111-1111-1111-111111111111",
                },
            };
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            public int SendCount { get; private set; }

            public Func<HttpResponseMessage>? ResponseFactory { get; set; }

            public Action<HttpRequestMessage>? OnSend { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                SendCount++;
                OnSend?.Invoke(request);
                var response =
                    ResponseFactory?.Invoke()
                    ?? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}"),
                    };
                return Task.FromResult(response);
            }
        }
    }
}
