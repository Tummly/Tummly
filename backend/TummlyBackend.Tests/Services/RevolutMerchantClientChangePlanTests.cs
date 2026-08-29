using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RevolutMerchantClientChangePlanTests
    {
        [Fact]
        public async Task ChangeSubscriptionPlanAsync_PostsAtCycleEnd_WithMappedVariationId()
        {
            var handler = new CapturingHandler
            {
                ResponseFactory = () =>
                    new HttpResponseMessage(HttpStatusCode.NoContent),
            };
            var client = CreateClient(handler, FullVat(), FullRevolut());

            var result = await client.ChangeSubscriptionPlanAsync(
                "sub_live_1",
                RevolutPlanVariationKeys.StarterMonthly
            );

            Assert.True(result.Succeeded);
            Assert.Equal(1, handler.SendCount);
            Assert.NotNull(handler.LastRequest);
            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.EndsWith(
                "/api/1.0/subscriptions/sub_live_1/change-plan",
                handler.LastRequest.RequestUri!.AbsolutePath
            );

            using var doc = JsonDocument.Parse(handler.LastBody!);
            Assert.Equal(
                "11111111-1111-1111-1111-111111111111",
                doc.RootElement.GetProperty("plan_variation_id").GetString()
            );
            Assert.Equal(
                "at_cycle_end",
                doc.RootElement.GetProperty("scheduled").GetString()
            );
        }

        [Fact]
        public async Task ChangeSubscriptionPlanAsync_DoesNotCallHttp_WhenVariationMissing()
        {
            var handler = new CapturingHandler();
            var revolut = FullRevolut();
            revolut.PlanVariations.Clear();
            var client = CreateClient(handler, FullVat(), revolut);

            var ex = await Assert.ThrowsAsync<RevolutMerchantNotReadyException>(
                () =>
                    client.ChangeSubscriptionPlanAsync(
                        "sub_1",
                        RevolutPlanVariationKeys.StarterMonthly
                    )
            );

            Assert.Equal(
                RevolutMerchantCreateGate.PlanVariationMissing,
                ex.Code
            );
            Assert.Equal(0, handler.SendCount);
        }

        private static IRevolutMerchantClient CreateClient(
            CapturingHandler handler,
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

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public int SendCount { get; private set; }

            public HttpRequestMessage? LastRequest { get; private set; }

            public string? LastBody { get; private set; }

            public Func<HttpResponseMessage>? ResponseFactory { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                SendCount++;
                LastRequest = request;
                if (request.Content != null)
                {
                    LastBody = await request.Content.ReadAsStringAsync(
                        cancellationToken
                    );
                }

                return ResponseFactory?.Invoke()
                    ?? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{}",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    };
            }
        }
    }
}
