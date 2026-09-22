using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Tests.Integration
{
    public class ExternalAuthEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public ExternalAuthEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Start_Redirects302_WhenGoogleConfigured()
        {
            await using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ExternalAuth:Google:ClientId"] =
                                "test-google-client-id",
                            ["ExternalAuth:Google:RedirectUri"] =
                                "https://api.example/api/auth/external/google/callback",
                        }
                    );
                });
            });

            var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                }
            );

            var response = await client.GetAsync(
                "/api/auth/external/google/start?returnPath=/login"
            );

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var location = response.Headers.Location?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(location));
            Assert.Contains(
                "accounts.google.com",
                location,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.Contains(
                "client_id=test-google-client-id",
                location,
                StringComparison.Ordinal
            );
        }
    }
}
