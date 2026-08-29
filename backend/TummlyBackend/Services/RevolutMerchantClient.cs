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

        public async Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
            string email,
            CancellationToken cancellationToken = default
        )
        {
            EnsureReadyForCreate(planVariationLookupKey: null);
            if (
                string.IsNullOrWhiteSpace(_settings.SecretKey)
                || string.IsNullOrWhiteSpace(_settings.ApiVersion)
            )
            {
                return new RevolutListCustomersResult(
                    Succeeded: false,
                    ErrorCode: "revolut_not_ready"
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            var path =
                $"api/1.0/customers?email={Uri.EscapeDataString(email.Trim())}";
            using var message = new HttpRequestMessage(HttpMethod.Get, path);
            ApplyAuthHeaders(message);

            using var response = await client.SendAsync(
                message,
                cancellationToken
            );
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RevolutListCustomersResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            string? firstId = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (
                            item.TryGetProperty("id", out var idElement)
                            && idElement.ValueKind == JsonValueKind.String
                        )
                        {
                            firstId = idElement.GetString();
                            break;
                        }
                    }
                }
                else if (
                    root.TryGetProperty("customers", out var customers)
                    && customers.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var item in customers.EnumerateArray())
                    {
                        if (
                            item.TryGetProperty("id", out var idElement)
                            && idElement.ValueKind == JsonValueKind.String
                        )
                        {
                            firstId = idElement.GetString();
                            break;
                        }
                    }
                }
            }

            return new RevolutListCustomersResult(
                Succeeded: true,
                FirstCustomerId: firstId,
                RawBody: raw
            );
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

        public async Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(subscriptionId)
                || string.IsNullOrWhiteSpace(_settings.SecretKey)
                || string.IsNullOrWhiteSpace(_settings.ApiVersion)
            )
            {
                return new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "revolut_not_ready"
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/1.0/subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}/cancel"
            );
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

            return new RevolutMerchantCreateResult(
                Succeeded: true,
                Id: subscriptionId.Trim(),
                RawBody: raw
            );
        }

        public async Task<RevolutOrderRetrieveResult> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(orderId)
                || string.IsNullOrWhiteSpace(_settings.SecretKey)
                || string.IsNullOrWhiteSpace(_settings.ApiVersion)
            )
            {
                return new RevolutOrderRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_not_ready"
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/1.0/orders/{Uri.EscapeDataString(orderId.Trim())}"
            );
            ApplyAuthHeaders(message);

            using var response = await client.SendAsync(
                message,
                cancellationToken
            );
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RevolutOrderRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            string? id = null;
            string? state = null;
            string? billingReason = null;
            string? subscriptionId = null;
            string? checkoutUrl = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (
                    root.TryGetProperty("id", out var idElement)
                    && idElement.ValueKind == JsonValueKind.String
                )
                {
                    id = idElement.GetString();
                }

                if (
                    root.TryGetProperty("state", out var stateElement)
                    && stateElement.ValueKind == JsonValueKind.String
                )
                {
                    state = stateElement.GetString();
                }

                if (
                    root.TryGetProperty("checkout_url", out var checkoutElement)
                    && checkoutElement.ValueKind == JsonValueKind.String
                )
                {
                    checkoutUrl = checkoutElement.GetString();
                }

                if (
                    root.TryGetProperty(
                        "subscription_data",
                        out var subscriptionData
                    )
                    && subscriptionData.ValueKind == JsonValueKind.Object
                )
                {
                    if (
                        subscriptionData.TryGetProperty(
                            "billing_reason",
                            out var reasonElement
                        )
                        && reasonElement.ValueKind == JsonValueKind.String
                    )
                    {
                        billingReason = reasonElement.GetString();
                    }

                    if (
                        subscriptionData.TryGetProperty(
                            "subscription_id",
                            out var subIdElement
                        )
                        && subIdElement.ValueKind == JsonValueKind.String
                    )
                    {
                        subscriptionId = subIdElement.GetString();
                    }
                }
            }

            return new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: id,
                State: state,
                BillingReason: billingReason,
                SubscriptionId: subscriptionId,
                RawBody: raw,
                CheckoutUrl: checkoutUrl
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
            string? setupOrderId = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (
                    root.TryGetProperty("id", out var idElement)
                    && idElement.ValueKind == JsonValueKind.String
                )
                {
                    id = idElement.GetString();
                }

                if (
                    root.TryGetProperty("setup_order_id", out var setupElement)
                    && setupElement.ValueKind == JsonValueKind.String
                )
                {
                    setupOrderId = setupElement.GetString();
                }
            }

            return new RevolutMerchantCreateResult(
                Succeeded: true,
                Id: id,
                RawBody: raw,
                SetupOrderId: setupOrderId
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
