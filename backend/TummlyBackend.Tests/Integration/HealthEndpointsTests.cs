using System.Net;
using System.Net.Http.Json;

namespace TummlyBackend.Tests.Integration
{
    public sealed class HealthEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HealthEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_returns_ok()
        {
            var response = await _client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<HealthBody>();
            Assert.Equal("healthy", body?.Status);
        }

        [Fact]
        public async Task HealthReady_returns_ok_when_init_succeeded()
        {
            // Testing env marks DatabaseInitState succeeded; in-memory skips pending check.
            var response = await _client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<HealthBody>();
            Assert.Equal("ready", body?.Status);
        }

        private sealed class HealthBody
        {
            public string? Status { get; set; }
        }
    }
}
