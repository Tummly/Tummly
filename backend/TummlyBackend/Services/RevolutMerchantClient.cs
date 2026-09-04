using System.Globalization;
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
                "api/subscriptions",
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
            object? lineItems = null;
            if (request.LineItems is { Count: > 0 })
            {
                // Merchant API /api/orders (not 1.0): quantity is { value }, and
                // redirect_url is honoured for Hosted Checkout return (1.0 drops it).
                lineItems = request.LineItems
                    .Select(item => new
                    {
                        name = item.Name,
                        type = string.IsNullOrWhiteSpace(item.Type)
                            ? "service"
                            : item.Type.Trim(),
                        unit_price_amount = item.UnitPriceAmount,
                        quantity = new { value = item.Quantity },
                        total_amount = item.TotalAmount,
                        external_id = string.IsNullOrWhiteSpace(item.ExternalId)
                            ? null
                            : item.ExternalId.Trim(),
                        taxes = item.Taxes
                            .Select(tax => new
                            {
                                name = tax.Name,
                                percentage = tax.Percentage,
                                amount = tax.Amount,
                            })
                            .ToArray(),
                    })
                    .ToArray();
            }

            return await PostCreateAsync(
                "api/orders",
                new
                {
                    amount = request.AmountMinor,
                    currency = request.Currency,
                    description = request.Description,
                    redirect_url = request.RedirectUrl,
                    customer = string.IsNullOrWhiteSpace(request.CustomerId)
                        ? null
                        : new { id = request.CustomerId },
                    line_items = lineItems,
                },
                cancellationToken
            );
        }

        public async Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        )
        {
            EnsureReadyForCreate(planVariationLookupKey);
            if (
                !_settings.TryGetPlanVariationId(
                    planVariationLookupKey,
                    out var variationId
                )
            )
            {
                throw new RevolutMerchantNotReadyException(
                    RevolutMerchantCreateGate.PlanVariationMissing
                );
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "revolut_subscription_required"
                );
            }

            return await PostCreateAsync(
                $"api/subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}/change-plan",
                new
                {
                    plan_variation_id = variationId,
                    scheduled = "at_cycle_end",
                },
                cancellationToken
            );
        }

        public async Task<RevolutMerchantCreateResult> ScheduleSubscriptionCancelAtCycleEndAsync(
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
                HttpMethod.Patch,
                $"api/subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}"
            );
            message.Content = JsonContent.Create(
                new
                {
                    scheduled_action = new
                    {
                        type = "cancel",
                        reason = "customer_request",
                    },
                },
                options: JsonOptions
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
                $"api/subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}/cancel"
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
            // Merchant Orders API (no 1.0): includes subscription_data.billing_reason
            // + subscription_id. api/1.0/orders omits those fields and webhook apply
            // then skips as unknown billing reason.
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/orders/{Uri.EscapeDataString(orderId.Trim())}"
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
            string? orderType = null;
            string? relatedOrderId = null;
            int? amountMinor = null;
            string? paymentMethodSummary = null;
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
                    root.TryGetProperty("type", out var typeElement)
                    && typeElement.ValueKind == JsonValueKind.String
                )
                {
                    orderType = typeElement.GetString();
                }

                if (
                    root.TryGetProperty(
                        "related_order_id",
                        out var relatedElement
                    )
                    && relatedElement.ValueKind == JsonValueKind.String
                )
                {
                    relatedOrderId = relatedElement.GetString();
                }

                amountMinor = TryReadAmountMinor(root);

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

                if (
                    string.IsNullOrWhiteSpace(subscriptionId)
                    && root.TryGetProperty(
                        "channel_data",
                        out var channelData
                    )
                    && channelData.ValueKind == JsonValueKind.Object
                    && channelData.TryGetProperty(
                        "subscription_id",
                        out var channelSubId
                    )
                    && channelSubId.ValueKind == JsonValueKind.String
                )
                {
                    subscriptionId = channelSubId.GetString();
                }

                paymentMethodSummary = TryReadPaymentMethodSummary(root);
            }

            return new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: id,
                State: state,
                BillingReason: billingReason,
                SubscriptionId: subscriptionId,
                RawBody: raw,
                CheckoutUrl: checkoutUrl,
                OrderType: orderType,
                RelatedOrderId: relatedOrderId,
                AmountMinor: amountMinor,
                PaymentMethodSummary: paymentMethodSummary
            );
        }

        public async Task<RevolutMerchantCreateResult> RefundOrderAsync(
            string orderId,
            int? amountMinor,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(orderId)
                || string.IsNullOrWhiteSpace(idempotencyKey)
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
                $"api/1.0/orders/{Uri.EscapeDataString(orderId.Trim())}/refund"
            );
            ApplyAuthHeaders(message);
            message.Headers.TryAddWithoutValidation(
                "Idempotency-Key",
                idempotencyKey.Trim()
            );

            object body = amountMinor is int amount
                ? new { amount }
                : new { };
            message.Content = JsonContent.Create(body, options: JsonOptions);

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

            string? refundId = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                if (
                    doc.RootElement.TryGetProperty("id", out var idElement)
                    && idElement.ValueKind == JsonValueKind.String
                )
                {
                    refundId = idElement.GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(refundId))
            {
                return new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            return new RevolutMerchantCreateResult(
                Succeeded: true,
                Id: refundId.Trim(),
                RawBody: raw
            );
        }

        public async Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
            string orderId,
            string merchantReference,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(orderId)
                || string.IsNullOrWhiteSpace(merchantReference)
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
                HttpMethod.Patch,
                $"api/1.0/orders/{Uri.EscapeDataString(orderId.Trim())}"
            );
            message.Content = JsonContent.Create(
                new
                {
                    merchant_order_data = new
                    {
                        reference = merchantReference.Trim(),
                    },
                },
                options: JsonOptions
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
                Id: orderId.Trim(),
                RawBody: raw
            );
        }

        public async Task<RevolutSubscriptionRetrieveResult> GetSubscriptionAsync(
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
                return new RevolutSubscriptionRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_not_ready"
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}"
            );
            ApplyAuthHeaders(message);

            using var response = await client.SendAsync(
                message,
                cancellationToken
            );
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RevolutSubscriptionRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            string? id = null;
            string? state = null;
            string? currentCycleId = null;
            string? paymentMethodId = null;
            string? customerId = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                id = ReadStringProp(root, "id");
                state = ReadStringProp(root, "state");
                currentCycleId = ReadStringProp(root, "current_cycle_id");
                paymentMethodId = ReadStringProp(root, "payment_method_id");
                customerId = ReadStringProp(root, "customer_id");
            }

            return new RevolutSubscriptionRetrieveResult(
                Succeeded: true,
                Id: id,
                State: state,
                CurrentCycleId: currentCycleId,
                PaymentMethodId: paymentMethodId,
                CustomerId: customerId,
                RawBody: raw
            );
        }

        public async Task<RevolutSubscriptionCycleRetrieveResult> GetSubscriptionCycleAsync(
            string subscriptionId,
            string cycleId,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(subscriptionId)
                || string.IsNullOrWhiteSpace(cycleId)
                || string.IsNullOrWhiteSpace(_settings.SecretKey)
                || string.IsNullOrWhiteSpace(_settings.ApiVersion)
            )
            {
                return new RevolutSubscriptionCycleRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_not_ready"
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}/cycles/{Uri.EscapeDataString(cycleId.Trim())}"
            );
            ApplyAuthHeaders(message);

            using var response = await client.SendAsync(
                message,
                cancellationToken
            );
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RevolutSubscriptionCycleRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            string? id = null;
            string? orderId = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                id = ReadStringProp(root, "id");
                orderId = ReadStringProp(root, "order_id");
            }

            return new RevolutSubscriptionCycleRetrieveResult(
                Succeeded: true,
                Id: id,
                OrderId: orderId,
                RawBody: raw
            );
        }

        public async Task<RevolutMerchantCreateResult> PayOrderAsync(
            RevolutPayOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(request.OrderId)
                || string.IsNullOrWhiteSpace(request.SavedPaymentMethodId)
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
                $"api/orders/{Uri.EscapeDataString(request.OrderId.Trim())}/payments"
            );
            message.Content = JsonContent.Create(
                new
                {
                    saved_payment_method = new
                    {
                        type = request.SavedPaymentMethodType,
                        id = request.SavedPaymentMethodId,
                        initiator = request.Initiator,
                    },
                },
                options: JsonOptions
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
                Id: request.OrderId.Trim(),
                RawBody: raw
            );
        }

        public async Task<RevolutDisputeRetrieveResult> GetDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        )
        {
            if (
                string.IsNullOrWhiteSpace(disputeId)
                || string.IsNullOrWhiteSpace(_settings.SecretKey)
                || string.IsNullOrWhiteSpace(_settings.ApiVersion)
            )
            {
                return new RevolutDisputeRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_not_ready"
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/disputes/{Uri.EscapeDataString(disputeId.Trim())}"
            );
            ApplyAuthHeaders(message);

            using var response = await client.SendAsync(
                message,
                cancellationToken
            );
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RevolutDisputeRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error",
                    RawBody: raw
                );
            }

            string? id = null;
            string? paymentOrderId = null;
            int? amountMinor = null;
            string? currency = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                id = ReadStringProp(root, "id");
                currency = ReadStringProp(root, "currency");
                if (
                    root.TryGetProperty("amount", out var amountElement)
                    && amountElement.ValueKind == JsonValueKind.Number
                    && amountElement.TryGetInt32(out var amount)
                )
                {
                    amountMinor = amount;
                }

                if (
                    root.TryGetProperty("payment", out var payment)
                    && payment.ValueKind == JsonValueKind.Object
                )
                {
                    paymentOrderId = ReadStringProp(payment, "order_id");
                }
            }

            return new RevolutDisputeRetrieveResult(
                Succeeded: true,
                Id: id,
                PaymentOrderId: paymentOrderId,
                AmountMinor: amountMinor,
                Currency: currency,
                RawBody: raw
            );
        }

        public async Task<RevolutMerchantCreateResult> AcceptDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        )
        {
            return await PostDisputeActionAsync(
                disputeId,
                "accept",
                body: null,
                cancellationToken
            );
        }

        public async Task<RevolutMerchantCreateResult> ChallengeDisputeAsync(
            string disputeId,
            string reason,
            CancellationToken cancellationToken = default
        )
        {
            return await PostDisputeActionAsync(
                disputeId,
                "challenge",
                body: new { reason },
                cancellationToken
            );
        }

        private async Task<RevolutMerchantCreateResult> PostDisputeActionAsync(
            string disputeId,
            string action,
            object? body,
            CancellationToken cancellationToken
        )
        {
            if (
                string.IsNullOrWhiteSpace(disputeId)
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
                $"api/disputes/{Uri.EscapeDataString(disputeId.Trim())}/{action}"
            );
            if (body != null)
            {
                message.Content = JsonContent.Create(body, options: JsonOptions);
            }

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
                Id: disputeId.Trim(),
                RawBody: raw
            );
        }

        private static string? ReadStringProp(JsonElement root, string name)
        {
            if (
                !root.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.String
            )
            {
                return null;
            }

            return element.GetString();
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
                    root.TryGetProperty("setup_order_id", out var setupElement)
                    && setupElement.ValueKind == JsonValueKind.String
                )
                {
                    setupOrderId = setupElement.GetString();
                }

                if (
                    root.TryGetProperty("checkout_url", out var checkoutElement)
                    && checkoutElement.ValueKind == JsonValueKind.String
                )
                {
                    checkoutUrl = checkoutElement.GetString();
                }

                // Subscription create returns setup_order_checkout_url, not checkout_url.
                if (
                    string.IsNullOrWhiteSpace(checkoutUrl)
                    && root.TryGetProperty(
                        "setup_order_checkout_url",
                        out var setupCheckoutElement
                    )
                    && setupCheckoutElement.ValueKind == JsonValueKind.String
                )
                {
                    checkoutUrl = setupCheckoutElement.GetString();
                }
            }

            return new RevolutMerchantCreateResult(
                Succeeded: true,
                Id: id,
                RawBody: raw,
                SetupOrderId: setupOrderId,
                CheckoutUrl: checkoutUrl
            );
        }

        private static int? TryReadAmountMinor(JsonElement root)
        {
            if (!root.TryGetProperty("amount", out var amountElement))
            {
                return null;
            }

            if (amountElement.ValueKind == JsonValueKind.Number
                && amountElement.TryGetInt32(out var amount))
            {
                return amount;
            }

            if (
                amountElement.ValueKind == JsonValueKind.Object
                && amountElement.TryGetProperty("value", out var valueElement)
                && valueElement.ValueKind == JsonValueKind.Number
                && valueElement.TryGetInt32(out var nested)
            )
            {
                return nested;
            }

            return null;
        }

        /// <summary>
        /// Builds "Paid via Visa ending in 4242" when Merchant order payments
        /// expose card brand + last four. Returns null when unknown.
        /// </summary>
        private static string? TryReadPaymentMethodSummary(JsonElement root)
        {
            if (
                !root.TryGetProperty("payments", out var payments)
                || payments.ValueKind != JsonValueKind.Array
            )
            {
                return null;
            }

            foreach (var payment in payments.EnumerateArray())
            {
                if (
                    !payment.TryGetProperty(
                        "payment_method",
                        out var method
                    )
                    || method.ValueKind != JsonValueKind.Object
                )
                {
                    continue;
                }

                var brand =
                    ReadStringProp(method, "card_brand")
                    ?? ReadStringProp(method, "brand");
                var lastFour =
                    ReadStringProp(method, "card_last_four")
                    ?? ReadStringProp(method, "last_four")
                    ?? ReadStringProp(method, "card_last4");

                if (
                    string.IsNullOrWhiteSpace(brand)
                    || string.IsNullOrWhiteSpace(lastFour)
                )
                {
                    continue;
                }

                var brandLabel = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                    brand.Trim().ToLowerInvariant()
                );
                return $"Paid via {brandLabel} ending in {lastFour.Trim()}";
            }

            return null;
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
