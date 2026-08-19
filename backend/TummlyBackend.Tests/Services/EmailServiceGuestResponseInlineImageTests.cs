using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class EmailServiceGuestResponseInlineImageTests
    {
        [Fact]
        public async Task SendGuestResponseEmailAsync_PostsHttpsChromeImages_WithoutCidAttachments()
        {
            var handler = new CapturingHandler();
            var service = CreateService(handler);

            await service.SendGuestResponseEmailAsync(
                "guest@example.test",
                "Thanks for your visit",
                "Burger House",
                "Camden High Street",
                "12 High Street, London",
                "Hi Sarah,\n\nThanks for visiting Burger House.",
                brandLogoUrl: null,
                offer: new GuestResponseEmailOfferBlock(
                    Title: "15% off",
                    Description: "Helper",
                    RedemptionCode: "BURGERCO-4829",
                    ExpiryLabel: "Expires: 31 July 2026"
                )
            );

            Assert.NotNull(handler.LastBody);
            using var document = JsonDocument.Parse(handler.LastBody!);
            var root = document.RootElement;

            var html = root.GetProperty("html").GetString();
            Assert.Contains(
                $"https://app.tummly.test{BaseNonTransactionalEmailTemplate.PublicLogoPath}",
                html
            );
            Assert.Contains(
                $"https://app.tummly.test{BaseNonTransactionalEmailTemplate.PublicTopDecorationPath}",
                html
            );
            Assert.Contains(
                OfferClaimQr.ToPngDataUri("BURGERCO-4829"),
                html
            );
            Assert.DoesNotContain("cid:", html);

            Assert.False(root.TryGetProperty("attachments", out _));
        }

        [Fact]
        public async Task SendGuestResponseEmailAsync_InjectsQaBanner_InsideBody()
        {
            var handler = new CapturingHandler();
            var service = CreateService(
                handler,
                qaRedirectTo: "qa@tummly.test"
            );

            await service.SendGuestResponseEmailAsync(
                "guest@example.test",
                "Thanks for your visit",
                "Burger House",
                null,
                "12 High Street, London",
                "Hello",
                brandLogoUrl: null
            );

            Assert.NotNull(handler.LastBody);
            using var document = JsonDocument.Parse(handler.LastBody!);
            var html = document.RootElement.GetProperty("html").GetString();
            Assert.False(string.IsNullOrWhiteSpace(html));

            var doctypeIndex = html!.IndexOf(
                "<!DOCTYPE",
                StringComparison.OrdinalIgnoreCase
            );
            var bodyIndex = html.IndexOf(
                "<body",
                StringComparison.OrdinalIgnoreCase
            );
            var bannerIndex = html.IndexOf(
                "QA redirect:",
                StringComparison.Ordinal
            );

            Assert.True(doctypeIndex >= 0);
            Assert.True(bodyIndex > doctypeIndex);
            Assert.True(bannerIndex > bodyIndex);
            Assert.Contains("guest@example.test", html);
        }

        private static EmailService CreateService(
            CapturingHandler handler,
            string? qaRedirectTo = null
        )
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
                    QaRedirectTo = qaRedirectTo,
                }
            );

            return new EmailService(
                settings,
                new StubHttpClientFactory(httpClient),
                configuration,
                new StubWebHostEnvironment
                {
                    ContentRootPath = ".",
                }
            );
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient)
            : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

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

            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();

            public IFileProvider WebRootFileProvider { get; set; } =
                new NullFileProvider();
        }
    }
}
