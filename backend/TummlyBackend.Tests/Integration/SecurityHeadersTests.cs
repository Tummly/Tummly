using Microsoft.AspNetCore.Mvc.Testing;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Baseline API security headers (privacy QA return X4).
    /// </summary>
    public class SecurityHeadersTests : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public SecurityHeadersTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ApiResponse_HasBaselineSecurityHeaders()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal("nosniff", Single(response, "X-Content-Type-Options"));
            Assert.Equal("DENY", Single(response, "X-Frame-Options"));
            Assert.Equal("no-referrer", Single(response, "Referrer-Policy"));
        }

        [Fact]
        public async Task HttpsResponse_HasHsts()
        {
            var client = _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                }
            );

            var response = await client.GetAsync("/health");

            Assert.Equal(
                "max-age=31536000; includeSubDomains",
                Single(response, "Strict-Transport-Security")
            );
        }

        private static string Single(HttpResponseMessage response, string name)
        {
            Assert.True(
                response.Headers.TryGetValues(name, out var values),
                $"Missing header {name}"
            );
            return Assert.Single(values);
        }
    }
}
