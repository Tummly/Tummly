using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Address;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AddressLookupServiceTests
    {
        [Fact]
        public void TryResolveFromIndex_returns_match_when_hint_overlaps_indexed_premise()
        {
            var premises = new Dictionary<string, AddressPremiseDto>(StringComparer.Ordinal)
            {
                [AddressPremiseIndex.NormalizeAddressKey("125 High Street, Manchester")] =
                    new AddressPremiseDto
                    {
                        Address = "125 High Street, Manchester",
                        Postcode = "M1 4AB",
                    },
            };

            var result = AddressPremiseIndex.TryResolveFromIndex(
                premises,
                "M1 4AB",
                "125 High Street"
            );

            Assert.NotNull(result);
            Assert.Equal("125 High Street, Manchester", result!.Address);
            Assert.False(result.MultiplePremises);
        }

        [Fact]
        public async Task ResolvePostcodeAsync_uses_premise_index_when_hint_matches_without_postcode_api()
        {
            var handler = new RecordingHttpMessageHandler(
                suggestionResponse: """
                    {
                      "result": {
                        "line_1": "125 High Street",
                        "post_town": "Manchester",
                        "postcode": "M1 4AB"
                      }
                    }
                    """,
                postcodeResponse: """
                    {
                      "result": [
                        {
                          "line_1": "125 High Street",
                          "post_town": "Manchester",
                          "postcode": "M1 4AB"
                        }
                      ]
                    }
                    """
            );

            var service = CreateService(handler);

            var suggestion = await service.ResolveSuggestionAsync("paf_125");
            Assert.NotNull(suggestion);

            var resolved = await service.ResolvePostcodeAsync(
                "M1 4AB",
                "125 High Street, Manchester"
            );

            Assert.NotNull(resolved);
            Assert.Equal("125 High Street, Manchester", resolved!.Address);
            Assert.Equal(0, handler.PostcodeRequestCount);
            Assert.Equal(1, handler.SuggestionRequestCount);
        }

        [Fact]
        public async Task ResolvePostcodeAsync_without_hint_still_calls_postcode_api()
        {
            var handler = new RecordingHttpMessageHandler(
                suggestionResponse: """
                    {
                      "result": {
                        "line_1": "125 High Street",
                        "post_town": "Manchester",
                        "postcode": "M1 4AB"
                      }
                    }
                    """,
                postcodeResponse: """
                    {
                      "result": [
                        {
                          "line_1": "125 High Street",
                          "post_town": "Manchester",
                          "postcode": "M1 4AB"
                        },
                        {
                          "line_1": "127 High Street",
                          "post_town": "Manchester",
                          "postcode": "M1 4AB"
                        }
                      ]
                    }
                    """
            );

            var service = CreateService(handler);

            await service.ResolveSuggestionAsync("paf_125");

            var resolved = await service.ResolvePostcodeAsync("M1 4AB");

            Assert.NotNull(resolved);
            Assert.Equal(1, handler.PostcodeRequestCount);
            Assert.True(resolved!.MultiplePremises);
        }

        private static AddressLookupService CreateService(RecordingHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.ideal-postcodes.co.uk/v1/"),
            };

            var settings = Options.Create(
                new IdealPostcodesSettings
                {
                    ApiKey = "ak_test",
                    AutocompleteLimit = 3,
                }
            );

            return new AddressLookupService(
                new StubHttpClientFactory(httpClient),
                new MemoryCache(new MemoryCacheOptions()),
                settings,
                NullLogger<AddressLookupService>.Instance
            );
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _suggestionResponse;
            private readonly string _postcodeResponse;

            public RecordingHttpMessageHandler(
                string suggestionResponse,
                string postcodeResponse
            )
            {
                _suggestionResponse = suggestionResponse;
                _postcodeResponse = postcodeResponse;
            }

            public int SuggestionRequestCount { get; private set; }

            public int PostcodeRequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/autocomplete/addresses/", StringComparison.Ordinal))
                {
                    SuggestionRequestCount += 1;
                    return Task.FromResult(JsonResponse(_suggestionResponse));
                }

                if (path.Contains("/postcodes/", StringComparison.Ordinal))
                {
                    PostcodeRequestCount += 1;
                    return Task.FromResult(JsonResponse(_postcodeResponse));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private static HttpResponseMessage JsonResponse(string json)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
