using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class CompaniesHouseSearchServiceTests
    {
        [Fact]
        public async Task SuggestAsync_maps_active_companies_first()
        {
            var handler = new RecordingHttpMessageHandler(
                """
                {
                  "items": [
                    {
                      "company_number": "00000002",
                      "title": "BETA DISSOLVED LTD",
                      "company_status": "dissolved",
                      "address_snippet": "2 Old Street, London"
                    },
                    {
                      "company_number": "00000001",
                      "title": "ALPHA ACTIVE LTD",
                      "company_status": "active",
                      "address_snippet": "1 High Street, London"
                    }
                  ]
                }
                """
            );

            var service = CreateService(handler);

            var suggestions = await service.SuggestAsync("alpha");

            Assert.Equal(2, suggestions.Count);
            Assert.Equal("00000001", suggestions[0].CompanyNumber);
            Assert.Equal("ALPHA ACTIVE LTD", suggestions[0].Title);
            Assert.Equal("1 High Street, London", suggestions[0].AddressSnippet);
            Assert.Equal("active", suggestions[0].CompanyStatus);
            Assert.Equal("00000002", suggestions[1].CompanyNumber);
            Assert.Equal(1, handler.RequestCount);
            Assert.NotNull(handler.LastAuthorization);
            Assert.Equal("Basic", handler.LastAuthorization!.Scheme);
        }

        [Fact]
        public async Task SuggestAsync_returns_empty_for_short_queries()
        {
            var handler = new RecordingHttpMessageHandler("{}");
            var service = CreateService(handler);

            var suggestions = await service.SuggestAsync("abc");

            Assert.Empty(suggestions);
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public async Task SuggestAsync_throws_when_api_key_missing()
        {
            var handler = new RecordingHttpMessageHandler("{}");
            var service = CreateService(handler, apiKey: "");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SuggestAsync("acme")
            );

            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public async Task SuggestAsync_uses_cache_for_repeat_queries()
        {
            var handler = new RecordingHttpMessageHandler(
                """
                {
                  "items": [
                    {
                      "company_number": "12345678",
                      "title": "TUMMLY LTD",
                      "company_status": "active",
                      "address_snippet": "10 Kitchen Lane, London"
                    }
                  ]
                }
                """
            );

            var service = CreateService(handler);

            var first = await service.SuggestAsync("tummly");
            var second = await service.SuggestAsync("TUMMLY");

            Assert.Single(first);
            Assert.Single(second);
            Assert.Equal(1, handler.RequestCount);
        }

        private static CompaniesHouseSearchService CreateService(
            RecordingHttpMessageHandler handler,
            string apiKey = "ch_test_key"
        )
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.company-information.service.gov.uk/"),
            };

            var settings = Options.Create(
                new CompaniesHouseSettings
                {
                    ApiKey = apiKey,
                    SuggestLimit = 10,
                }
            );

            return new CompaniesHouseSearchService(
                new StubHttpClientFactory(httpClient),
                new MemoryCache(new MemoryCacheOptions()),
                settings,
                NullLogger<CompaniesHouseSearchService>.Instance
            );
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseBody;

            public RecordingHttpMessageHandler(string responseBody)
            {
                _responseBody = responseBody;
            }

            public int RequestCount { get; private set; }

            public AuthenticationHeaderValue? LastAuthorization { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                RequestCount += 1;
                LastAuthorization = request.Headers.Authorization;

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            _responseBody,
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                );
            }
        }
    }
}
