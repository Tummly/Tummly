using System.Net;

namespace TummlyBackend.Tests.Integration
{
    public class WorkspaceSummaryRemovedEndpointTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public WorkspaceSummaryRemovedEndpointTests(
            TummlyWebApplicationFactory factory
        )
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task WorkspaceSummary_IsNotRouted_WithoutJwt()
        {
            var response = await _client.GetAsync(
                "/api/workspace/summary?locationId=1"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
