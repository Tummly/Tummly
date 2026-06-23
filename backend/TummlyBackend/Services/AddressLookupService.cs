using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Address;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class AddressLookupService : IAddressLookupService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IdealPostcodesSettings _settings;
        private readonly ILogger<AddressLookupService> _logger;

        public AddressLookupService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<IdealPostcodesSettings> settings,
            ILogger<AddressLookupService> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AddressSuggestionDto>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default
        )
        {
            var normalizedQuery = NormalizeQuery(query);

            if (normalizedQuery.Length < 4)
            {
                return Array.Empty<AddressSuggestionDto>();
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("Ideal Postcodes API key is not configured.");
                return Array.Empty<AddressSuggestionDto>();
            }

            var cacheKey = $"address_suggest:{normalizedQuery}";

            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<AddressSuggestionDto>? cached) &&
                cached is not null)
            {
                return cached;
            }

            var client = _httpClientFactory.CreateClient("IdealPostcodes");

            var autocompleteUrl =
                $"autocomplete/addresses?api_key={Uri.EscapeDataString(_settings.ApiKey)}" +
                $"&query={Uri.EscapeDataString(normalizedQuery)}" +
                $"&limit={_settings.AutocompleteLimit}";

            using var autocompleteResponse = await client.GetAsync(
                autocompleteUrl,
                cancellationToken
            );

            if (!autocompleteResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ideal Postcodes autocomplete failed with status {StatusCode}",
                    autocompleteResponse.StatusCode
                );

                return Array.Empty<AddressSuggestionDto>();
            }

            await using var autocompleteStream =
                await autocompleteResponse.Content.ReadAsStreamAsync(cancellationToken);

            using var autocompleteDocument =
                await JsonDocument.ParseAsync(autocompleteStream, cancellationToken: cancellationToken);

            var hits = autocompleteDocument.RootElement
                .GetProperty("result")
                .GetProperty("hits");

            var suggestions = new List<AddressSuggestionDto>();

            foreach (var hit in hits.EnumerateArray().Take(_settings.AutocompleteLimit))
            {
                if (!hit.TryGetProperty("id", out var idElement) ||
                    !hit.TryGetProperty("suggestion", out var suggestionElement))
                {
                    continue;
                }

                var id = idElement.GetString();

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var label = suggestionElement.GetString() ?? string.Empty;

                suggestions.Add(new AddressSuggestionDto
                {
                    Id = id,
                    Label = label,
                });
            }

            _cache.Set(
                cacheKey,
                suggestions,
                TimeSpan.FromMinutes(_settings.SuggestCacheMinutes)
            );

            return suggestions;
        }

        public async Task<AddressResolveResultDto?> ResolvePostcodeAsync(
            string postcode,
            string? addressHint = null,
            CancellationToken cancellationToken = default
        )
        {
            if (!UkPostcode.IsValidFormat(postcode))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("Ideal Postcodes API key is not configured.");
                return null;
            }

            var normalizedPostcode = UkPostcode.NormalizeForLookup(postcode);
            var cacheKey = $"address_resolve:{normalizedPostcode}";

            if (!_cache.TryGetValue(cacheKey, out AddressResolveResultDto? cached))
            {
                if (!string.IsNullOrWhiteSpace(addressHint))
                {
                    cached = TryResolveFromPremiseIndex(
                        normalizedPostcode,
                        addressHint
                    );
                }

                if (cached is null)
                {
                    cached = await FetchPostcodeResultAsync(
                        normalizedPostcode,
                        cancellationToken
                    );

                    if (cached is null)
                    {
                        return null;
                    }

                    IndexPremises(
                        cached.Premises,
                        TimeSpan.FromHours(_settings.ResolveCacheHours)
                    );

                    _cache.Set(
                        cacheKey,
                        cached,
                        TimeSpan.FromHours(_settings.ResolveCacheHours)
                    );
                }
            }

            if (cached is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(addressHint))
            {
                return cached;
            }

            var bestAddress = AddressFormatting.PickBestMatch(
                cached.Premises.Select(premise => premise.Address),
                addressHint
            );

            if (string.IsNullOrWhiteSpace(bestAddress))
            {
                return cached;
            }

            return new AddressResolveResultDto
            {
                Postcode = cached.Postcode,
                Address = bestAddress,
                Premises = cached.Premises,
                MultiplePremises = cached.MultiplePremises,
                UsedBestMatch = !string.Equals(
                    bestAddress,
                    cached.Address,
                    StringComparison.OrdinalIgnoreCase
                ),
            };
        }

        private async Task<AddressResolveResultDto?> FetchPostcodeResultAsync(
            string normalizedPostcode,
            CancellationToken cancellationToken
        )
        {
            var client = _httpClientFactory.CreateClient("IdealPostcodes");

            var url =
                $"postcodes/{Uri.EscapeDataString(normalizedPostcode)}" +
                $"?api_key={Uri.EscapeDataString(_settings.ApiKey)}";

            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ideal Postcodes postcode lookup failed with status {StatusCode}",
                    response.StatusCode
                );

                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("result", out var resultElement) ||
                resultElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var premises = resultElement
                .EnumerateArray()
                .Select(ParsePremise)
                .Where(premise => !string.IsNullOrWhiteSpace(premise.Address))
                .ToList();

            if (premises.Count == 0)
            {
                return null;
            }

            var displayPostcode = UkPostcode.FormatForDisplay(normalizedPostcode);
            var primaryAddress = premises[0].Address;

            return new AddressResolveResultDto
            {
                Postcode = displayPostcode,
                Address = primaryAddress,
                Premises = premises,
                MultiplePremises = premises.Count > 1,
                UsedBestMatch = false,
            };
        }

        public async Task<AddressPremiseDto?> ResolveSuggestionAsync(
            string suggestionId,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(suggestionId))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("Ideal Postcodes API key is not configured.");
                return null;
            }

            var resolveCacheKey = $"address_resolve_suggestion:{suggestionId}";

            if (_cache.TryGetValue(resolveCacheKey, out AddressPremiseDto? cached))
            {
                return cached;
            }

            var client = _httpClientFactory.CreateClient("IdealPostcodes");

            var url =
                $"autocomplete/addresses/{Uri.EscapeDataString(suggestionId)}/gbr" +
                $"?api_key={Uri.EscapeDataString(_settings.ApiKey)}";

            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("result", out var resultElement))
            {
                return null;
            }

            var premise = ParsePremise(resultElement);
            var cacheLifetime = TimeSpan.FromHours(_settings.ResolveCacheHours);

            _cache.Set(
                resolveCacheKey,
                premise,
                cacheLifetime
            );

            IndexPremise(premise, cacheLifetime);

            return premise;
        }

        private void IndexPremise(
            AddressPremiseDto premise,
            TimeSpan cacheLifetime
        )
        {
            if (string.IsNullOrWhiteSpace(premise.Postcode) ||
                string.IsNullOrWhiteSpace(premise.Address))
            {
                return;
            }

            var normalizedPostcode = UkPostcode.NormalizeForLookup(premise.Postcode);
            var normalizedAddress = AddressPremiseIndex.NormalizeAddressKey(premise.Address);

            if (string.IsNullOrWhiteSpace(normalizedAddress))
            {
                return;
            }

            var premiseKey = AddressPremiseIndex.BuildPremiseKey(
                normalizedPostcode,
                normalizedAddress
            );

            _cache.Set(premiseKey, premise, cacheLifetime);
            AddPremiseToIndex(normalizedPostcode, normalizedAddress, cacheLifetime);
        }

        private void IndexPremises(
            IEnumerable<AddressPremiseDto> premises,
            TimeSpan cacheLifetime
        )
        {
            foreach (var premise in premises)
            {
                IndexPremise(premise, cacheLifetime);
            }
        }

        private void AddPremiseToIndex(
            string normalizedPostcode,
            string normalizedAddress,
            TimeSpan cacheLifetime
        )
        {
            var indexKey = AddressPremiseIndex.BuildIndexKey(normalizedPostcode);
            var addressKeys = _cache.GetOrCreate(
                indexKey,
                _ => new HashSet<string>(StringComparer.Ordinal)
            )!;

            lock (addressKeys)
            {
                addressKeys.Add(normalizedAddress);
            }

            _cache.Set(indexKey, addressKeys, cacheLifetime);
        }

        private AddressResolveResultDto? TryResolveFromPremiseIndex(
            string normalizedPostcode,
            string addressHint
        )
        {
            var indexKey = AddressPremiseIndex.BuildIndexKey(normalizedPostcode);

            if (!_cache.TryGetValue(indexKey, out HashSet<string>? addressKeys) ||
                addressKeys is null ||
                addressKeys.Count == 0)
            {
                return null;
            }

            string[] keysSnapshot;

            lock (addressKeys)
            {
                keysSnapshot = addressKeys.ToArray();
            }

            var premisesByAddressKey =
                new Dictionary<string, AddressPremiseDto>(StringComparer.Ordinal);

            foreach (var normalizedAddress in keysSnapshot)
            {
                var premiseKey = AddressPremiseIndex.BuildPremiseKey(
                    normalizedPostcode,
                    normalizedAddress
                );

                if (_cache.TryGetValue(premiseKey, out AddressPremiseDto? premise) &&
                    premise is not null)
                {
                    premisesByAddressKey[normalizedAddress] = premise;
                }
            }

            return AddressPremiseIndex.TryResolveFromIndex(
                premisesByAddressKey,
                UkPostcode.FormatForDisplay(normalizedPostcode),
                addressHint
            );
        }

        private static AddressPremiseDto ParsePremise(JsonElement element)
        {
            var line1 = element.TryGetProperty("line_1", out var line1Element)
                ? line1Element.GetString()
                : null;
            var line2 = element.TryGetProperty("line_2", out var line2Element)
                ? line2Element.GetString()
                : null;
            var postTown = element.TryGetProperty("post_town", out var postTownElement)
                ? postTownElement.GetString()
                : null;
            var postcode = element.TryGetProperty("postcode", out var postcodeElement)
                ? postcodeElement.GetString()
                : null;

            return new AddressPremiseDto
            {
                Address = AddressFormatting.FormatStreetAndTown(line1, line2, postTown),
                Postcode = string.IsNullOrWhiteSpace(postcode)
                    ? string.Empty
                    : UkPostcode.FormatForDisplay(postcode),
            };
        }

        private static string NormalizeQuery(string query)
        {
            return query.Trim().ToLowerInvariant();
        }
    }
}
