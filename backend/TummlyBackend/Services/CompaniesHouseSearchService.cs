using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Companies;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class CompaniesHouseSearchService : ICompaniesHouseSearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly CompaniesHouseSettings _settings;
        private readonly ILogger<CompaniesHouseSearchService> _logger;

        public CompaniesHouseSearchService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<CompaniesHouseSettings> settings,
            ILogger<CompaniesHouseSearchService> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CompanySuggestionDto>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default
        )
        {
            var normalizedQuery = NormalizeQuery(query);

            if (normalizedQuery.Length < 4)
            {
                return Array.Empty<CompanySuggestionDto>();
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("Companies House API key is not configured.");
                throw new InvalidOperationException(
                    "Companies House search is not configured."
                );
            }

            var cacheKey = $"companies_suggest:{normalizedQuery}";

            if (
                _cache.TryGetValue(cacheKey, out IReadOnlyList<CompanySuggestionDto>? cached)
                && cached is not null
            )
            {
                return cached;
            }

            var client = _httpClientFactory.CreateClient("CompaniesHouse");
            var limit = Math.Clamp(_settings.SuggestLimit, 1, 20);
            var url =
                $"search/companies?q={Uri.EscapeDataString(normalizedQuery)}"
                + $"&items_per_page={limit}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = BuildBasicAuthHeader(_settings.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Companies House search failed with status {StatusCode}",
                    response.StatusCode
                );

                throw new HttpRequestException(
                    $"Companies House search failed with status {(int)response.StatusCode}."
                );
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (
                !document.RootElement.TryGetProperty("items", out var itemsElement)
                || itemsElement.ValueKind != JsonValueKind.Array
            )
            {
                return Array.Empty<CompanySuggestionDto>();
            }

            var parsed = new List<CompanySuggestionDto>();

            foreach (var item in itemsElement.EnumerateArray())
            {
                var suggestion = ParseSuggestion(item);

                if (suggestion is null)
                {
                    continue;
                }

                parsed.Add(suggestion);
            }

            var suggestions = OrderPreferringActive(parsed)
                .Take(limit)
                .ToList();

            _cache.Set(
                cacheKey,
                (IReadOnlyList<CompanySuggestionDto>)suggestions,
                TimeSpan.FromMinutes(_settings.SuggestCacheMinutes)
            );

            return suggestions;
        }

        private static IEnumerable<CompanySuggestionDto> OrderPreferringActive(
            IEnumerable<CompanySuggestionDto> suggestions
        )
        {
            return suggestions
                .OrderByDescending(item =>
                    string.Equals(item.CompanyStatus, "active", StringComparison.OrdinalIgnoreCase)
                )
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase);
        }

        private static CompanySuggestionDto? ParseSuggestion(JsonElement item)
        {
            var companyNumber = item.TryGetProperty("company_number", out var numberElement)
                ? numberElement.GetString()
                : null;
            var title = item.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(companyNumber) || string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var addressSnippet = item.TryGetProperty("address_snippet", out var addressElement)
                ? addressElement.GetString() ?? string.Empty
                : string.Empty;
            var companyStatus = item.TryGetProperty("company_status", out var statusElement)
                ? statusElement.GetString() ?? string.Empty
                : string.Empty;

            return new CompanySuggestionDto
            {
                CompanyNumber = companyNumber.Trim(),
                Title = title.Trim(),
                AddressSnippet = addressSnippet.Trim(),
                CompanyStatus = companyStatus.Trim(),
            };
        }

        private static AuthenticationHeaderValue BuildBasicAuthHeader(string apiKey)
        {
            var token = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{apiKey}:")
            );

            return new AuthenticationHeaderValue("Basic", token);
        }

        private static string NormalizeQuery(string query)
        {
            return query.Trim().ToLowerInvariant();
        }
    }
}
