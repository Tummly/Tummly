using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="EmailService"/> Resend payload — RFC 8058 <c>List-Unsubscribe</c> headers
    /// on campaign and guest-response mail.
    /// </summary>
    public class EmailServiceHeadersTests
    {
        private const string OneClickUrl =
            "https://api.tummly.test/api/public/preferences/one-click?t=payload.sig";

        [Fact]
        public async Task SendCampaignGuestEmailAsync_WithOneClickUrl_SendsListUnsubscribeHeaders()
        {
            var handler = new CapturingHandler();
            var service = CreateService(handler);

            await service.SendCampaignGuestEmailAsync(
                "guest@example.test",
                "Spring menu",
                "Burger House",
                null,
                "12 High Street, London",
                "Hello",
                oneClickUnsubscribeUrl: OneClickUrl
            );

            var headers = ReadHeaders(handler);
            Assert.Equal($"<{OneClickUrl}>", headers["List-Unsubscribe"]);
            Assert.Equal("List-Unsubscribe=One-Click", headers["List-Unsubscribe-Post"]);
        }

        [Fact]
        public async Task SendGuestResponseEmailAsync_WithOneClickUrl_SendsListUnsubscribeHeaders()
        {
            var handler = new CapturingHandler();
            var service = CreateService(handler);

            await service.SendGuestResponseEmailAsync(
                "guest@example.test",
                "Thanks for your visit",
                "Burger House",
                null,
                "12 High Street, London",
                "Hello",
                oneClickUnsubscribeUrl: OneClickUrl
            );

            var headers = ReadHeaders(handler);
            Assert.Equal($"<{OneClickUrl}>", headers["List-Unsubscribe"]);
            Assert.Equal("List-Unsubscribe=One-Click", headers["List-Unsubscribe-Post"]);
        }

        [Fact]
        public async Task SendCampaignGuestEmailAsync_WithoutOneClickUrl_SendsNoHeaders()
        {
            var handler = new CapturingHandler();
            var service = CreateService(handler);

            await service.SendCampaignGuestEmailAsync(
                "guest@example.test",
                "Spring menu",
                "Burger House",
                null,
                "12 High Street, London",
                "Hello"
            );

            Assert.NotNull(handler.LastBody);
            using var document = JsonDocument.Parse(handler.LastBody!);
            Assert.False(document.RootElement.TryGetProperty("headers", out _));
        }

        private static Dictionary<string, string> ReadHeaders(CapturingHandler handler)
        {
            Assert.NotNull(handler.LastBody);
            using var document = JsonDocument.Parse(handler.LastBody!);
            return document.RootElement
                .GetProperty("headers")
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.GetString()!);
        }

        private static EmailService CreateService(CapturingHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.resend.com/"),
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
                    }
                )
                .Build();

            var settings = Options.Create(
                new EmailSettings
                {
                    ApiKey = "re_test",
                    SenderName = "Tummly",
                    SenderEmail = "hello@tummly.test",
                }
            );

            return new EmailService(
                settings,
                new StubHttpClientFactory(httpClient),
                configuration,
                new StubWebHostEnvironment(),
                new DemoSideEffectGuard(
                    Microsoft.Extensions.Options.Options.Create(
                        new DemoSessionOptions
                        {
                            Enabled = false,
                            SuppressSideEffects = false,
                        }
                    )
                ),
                new Microsoft.AspNetCore.Http.HttpContextAccessor()
            );
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

        /// <summary>Captures the Resend request body and answers with a provider id.</summary>
        private sealed class CapturingHandler : HttpMessageHandler
        {
            public string? LastBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                LastBody = request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"id":"email_test"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            }
        }

        private sealed class StubWebHostEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "Tests";

            public string EnvironmentName { get; set; } = "Testing";

            public string ContentRootPath { get; set; } = ".";

            public string WebRootPath { get; set; } = ".";

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
