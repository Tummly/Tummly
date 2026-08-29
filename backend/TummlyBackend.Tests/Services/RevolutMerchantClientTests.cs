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

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                SendCount++;
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
