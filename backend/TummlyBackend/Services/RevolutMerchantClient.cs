using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class RevolutMerchantClient : IRevolutMerchantClient
    {
        public const string HttpClientName = "RevolutMerchant";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRevolutMerchantCreateGate _gate;
        private readonly RevolutSettings _settings;

        public RevolutMerchantClient(
            IHttpClientFactory httpClientFactory,
            IRevolutMerchantCreateGate gate,
            IOptions<RevolutSettings> settings
        )
        {
            _httpClientFactory = httpClientFactory;
            _gate = gate;
            _settings = settings.Value;
        }

        public void EnsureReadyForCreate(string? planVariationLookupKey = null)
        {
            var code = _gate.Evaluate(planVariationLookupKey);
            if (code != null)
            {
                throw new RevolutMerchantNotReadyException(code);
            }
        }

        public async Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        )
        {
            EnsureReadyForCreate(planVariationLookupKey: null);
            return await PostCreateAsync(
                "api/1.0/customers",
                new
                {
                    email = request.Email,
                    full_name = request.FullName,
                },
                cancellationToken
            );
        }

        public async Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
            RevolutCreateSubscriptionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            EnsureReadyForCreate(request.PlanVariationLookupKey);
            if (
                !_settings.TryGetPlanVariationId(
                    request.PlanVariationLookupKey,
                    out var variationId
                )
            )
            {
                throw new RevolutMerchantNotReadyException(
                    RevolutMerchantCreateGate.PlanVariationMissing
                );
            }

            return await PostCreateAsync(
                "api/1.0/subscriptions",
                new
                {
                    customer_id = request.CustomerId,
                    plan_variation_id = variationId,
                    setup_order_redirect_url = request.SetupOrderRedirectUrl,
                },
                cancellationToken
            );
        }

        public async Task<RevolutMerchantCreateResult> CreateOrderAsync(
            RevolutCreateOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            EnsureReadyForCreate(request.PlanVariationLookupKey);
            return await PostCreateAsync(
                "api/1.0/orders",
                new
                {
                    amount = request.AmountMinor,
                    currency = request.Currency,
                },
                cancellationToken
            );
        }

        private async Task<RevolutMerchantCreateResult> PostCreateAsync(
            string relativePath,
            object body,
            CancellationToken cancellationToken
        )
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                relativePath
            );
            message.Content = JsonContent.Create(body, options: JsonOptions);
            ApplyAuthHeaders(message);

            using var response = await client.SendAsync(
                message,
                cancellationToken
            );
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            string? id = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                if (
                    doc.RootElement.TryGetProperty("id", out var idElement)
                    && idElement.ValueKind == JsonValueKind.String
                )
                {
                    id = idElement.GetString();
                }
            }

            return new RevolutMerchantCreateResult(
                Succeeded: true,
                Id: id,
                RawBody: raw
            );
        }

        private void ApplyAuthHeaders(HttpRequestMessage message)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _settings.SecretKey.Trim()
            );
            message.Headers.TryAddWithoutValidation(
                "Revolut-Api-Version",
                _settings.ApiVersion.Trim()
            );
        }
    }
}
