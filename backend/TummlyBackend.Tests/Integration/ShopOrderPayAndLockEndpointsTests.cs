using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class ShopOrderPayAndLockEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string SigningSecret = "whsec_placeholder";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrderPayAndLockEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PayThenWebhook_MarksOrderPaidAndProcessing()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            var orderId = await PlaceAwaitingPaymentOrderAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId
            );

            using var payRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/shop/orders/{orderId}/pay?locationId={seeded.InScopeLocationId}"
            );
            payRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            payRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));

            var payResponse = await _client.SendAsync(payRequest);
            Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
            var payBody = await ReadJsonAsync(payResponse);
            Assert.Equal("pay", payBody.GetProperty("outcome").GetString());
            Assert.Equal(
                FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                payBody.GetProperty("redirectUrl").GetString()
            );

            string revolutOrderId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                revolutOrderId = await context.RevolutOrderIntents
                    .Where(row => row.ShopOrderId == orderId)
                    .Select(row => row.OrderId)
                    .SingleAsync();
            }

            _factory.Merchant.Orders[revolutOrderId] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: revolutOrderId,
                State: "completed",
                AmountMinor: 12480,
                CheckoutUrl: FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                PaymentMethodSummary: "Paid via Visa ending in 4242"
            );

            var webhookBody =
                $$"""{"event":"ORDER_COMPLETED","order_id":"{{revolutOrderId}}"}""";
            var webhook = await SendSignedWebhookAsync(webhookBody);
            Assert.True(
                webhook.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent
            );

            using var getRequest = AuthorizedGet(
                $"/api/shop/orders/{orderId}?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var orderBody = await ReadJsonAsync(getResponse);
            Assert.Equal("paid", orderBody.GetProperty("paymentStatus").GetString());
            Assert.Equal(
                "processing",
                orderBody.GetProperty("fulfilmentStatus").GetString()
            );
            var invoiceDocumentNumber = orderBody
                .GetProperty("paymentSummary")
                .GetProperty("invoiceDocumentNumber")
                .GetString();
            Assert.False(string.IsNullOrWhiteSpace(invoiceDocumentNumber));
            Assert.StartsWith("TM-", invoiceDocumentNumber);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var order = await context.ShopOrders
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == orderId);
                Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
                Assert.Equal(revolutOrderId, order.RevolutOrderId);
                Assert.NotNull(order.PaidAtUtc);

                var intent = await context.RevolutOrderIntents
                    .AsNoTracking()
                    .SingleAsync(row => row.ShopOrderId == orderId);
                Assert.False(intent.IsOpen);
                Assert.Equal(
                    RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    intent.Purpose
                );

                var invoiceExists = await context.TummlyVatInvoices
                    .AsNoTracking()
                    .AnyAsync(row =>
                        row.RevolutOrderId == revolutOrderId
                        && row.DocumentNumber == invoiceDocumentNumber
                    );
                Assert.True(invoiceExists);

                var invoice = await context.TummlyVatInvoices
                    .AsNoTracking()
                    .SingleAsync(row => row.RevolutOrderId == revolutOrderId);
                Assert.False(string.IsNullOrWhiteSpace(invoice.DeliverToSnapshot));
                Assert.Contains(
                    order.ShipToContactName,
                    invoice.DeliverToSnapshot,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    order.ShipToPostcode,
                    invoice.DeliverToSnapshot,
                    StringComparison.Ordinal
                );
                Assert.Equal(
                    "Paid via Visa ending in 4242",
                    invoice.PaymentMethodSummary
                );
                Assert.False(string.IsNullOrWhiteSpace(invoice.LineItemsJson));
                Assert.Contains(
                    "Standard delivery",
                    invoice.LineItemsJson,
                    StringComparison.Ordinal
                );

                var pdf = System.Text.Encoding.ASCII.GetString(
                    TummlyVatInvoicePdfWriter.Render(invoice)
                );
                Assert.Contains("Deliver to", pdf, StringComparison.Ordinal);
                Assert.Contains("Invoice", pdf, StringComparison.Ordinal);
                Assert.Contains(
                    "Paid via Visa ending in 4242",
                    pdf,
                    StringComparison.Ordinal
                );
                Assert.Contains("Tax = VAT", pdf, StringComparison.Ordinal);
            }
        }

        [Fact]
        public async Task OrderFailedWebhook_MarksAwaitingPaymentAsPaymentFailed()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            var orderId = await PlaceAwaitingPaymentOrderAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId
            );

            using var payRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/shop/orders/{orderId}/pay?locationId={seeded.InScopeLocationId}"
            );
            payRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            payRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            var payResponse = await _client.SendAsync(payRequest);
            Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);

            string revolutOrderId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                revolutOrderId = await context.RevolutOrderIntents
                    .Where(row => row.ShopOrderId == orderId)
                    .Select(row => row.OrderId)
                    .SingleAsync();
            }

            var webhookBody =
                $$"""{"event":"ORDER_FAILED","order_id":"{{revolutOrderId}}"}""";
            var webhook = await SendSignedWebhookAsync(webhookBody);
            Assert.True(
                webhook.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var order = await context.ShopOrders
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == orderId);
                Assert.Equal(
                    ShopPaymentStatuses.PaymentFailed,
                    order.PaymentStatus
                );

                var intent = await context.RevolutOrderIntents
                    .AsNoTracking()
                    .SingleAsync(row => row.ShopOrderId == orderId);
                Assert.False(intent.IsOpen);
            }
        }

        [Fact]
        public async Task SoftLock_Catalog200_CartPut403()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.SoftLock
            );

            using var catalogRequest = AuthorizedGet(
                $"/api/shop/catalog?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var catalogResponse = await _client.SendAsync(catalogRequest);
            Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);

            var put = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "table-tents",
                2
            );
            Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
            var body = await ReadJsonAsync(put);
            Assert.Equal("soft_lock", body.GetProperty("code").GetString());
            Assert.Equal("soft_lock", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task SoftLock_PlaceAndPay_Return403()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.SoftLock
            );

            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[] { new { skuId = "table-tents", quantity = 2 } },
                    deliveryMethod = "standard",
                    expectedGrossPence = 5760,
                    shipTo = ValidShipTo(),
                }
            );
            Assert.Equal(HttpStatusCode.Forbidden, place.StatusCode);
            var placeBody = await ReadJsonAsync(place);
            Assert.Equal("soft_lock", placeBody.GetProperty("code").GetString());

            var orderId = await PlaceAwaitingPaymentOrderAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                bypassLock: true
            );
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.SoftLock
            );

            using var payRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/shop/orders/{orderId}/pay?locationId={seeded.InScopeLocationId}"
            );
            payRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            payRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            var payResponse = await _client.SendAsync(payRequest);
            Assert.Equal(HttpStatusCode.Forbidden, payResponse.StatusCode);
            var payBody = await ReadJsonAsync(payResponse);
            Assert.Equal("soft_lock", payBody.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Dormant_CartPut_Return403()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.Dormant
            );

            var put = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "table-tents",
                2
            );
            Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
            var body = await ReadJsonAsync(put);
            Assert.Equal("dormant", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task ChargebackRestricted_CartPutAndPay_Return403()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            await SetChargebackRestrictedAsync(seeded.RestaurantId, restricted: true);

            var put = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "table-tents",
                2
            );
            Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
            var putBody = await ReadJsonAsync(put);
            Assert.Equal(
                "chargeback_restricted",
                putBody.GetProperty("code").GetString()
            );

            var orderId = await PlaceAwaitingPaymentOrderAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                bypassLock: true
            );
            await SetChargebackRestrictedAsync(seeded.RestaurantId, restricted: true);

            using var payRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/shop/orders/{orderId}/pay?locationId={seeded.InScopeLocationId}"
            );
            payRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            payRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            var payResponse = await _client.SendAsync(payRequest);
            Assert.Equal(HttpStatusCode.Forbidden, payResponse.StatusCode);
            var payBody = await ReadJsonAsync(payResponse);
            Assert.Equal(
                "chargeback_restricted",
                payBody.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task ChargebackRestricted_PlaceOrder_Return403()
        {
            var seeded = await SeedPaidShopWorkspaceAsync();
            await SetChargebackRestrictedAsync(seeded.RestaurantId, restricted: true);

            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[] { new { skuId = "table-tents", quantity = 2 } },
                    deliveryMethod = "standard",
                    expectedGrossPence = 5760,
                    shipTo = ValidShipTo(),
                }
            );
            Assert.Equal(HttpStatusCode.Forbidden, place.StatusCode);
            var body = await ReadJsonAsync(place);
            Assert.Equal(
                "chargeback_restricted",
                body.GetProperty("code").GetString()
            );
        }

        private async Task<Guid> PlaceAwaitingPaymentOrderAsync(
            string jwt,
            int locationId,
            bool bypassLock = false
        )
        {
            if (bypassLock)
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurantId = await context.RestaurantLocations
                    .Where(row => row.Id == locationId)
                    .Select(row => row.RestaurantId)
                    .SingleAsync();
                var account = await context.BillingAccounts
                    .SingleAsync(row => row.RestaurantId == restaurantId);
                account.BillingStatus = BillingStatuses.Active;
                account.ChargebackRestricted = false;
                await context.SaveChangesAsync();
            }

            var place = await PlaceOrderAsync(
                jwt,
                new
                {
                    locationId,
                    lines = new[] { new { skuId = "table-tents", quantity = 2 } },
                    deliveryMethod = "standard",
                    expectedGrossPence = 5760,
                    shipTo = ValidShipTo(),
                }
            );
            Assert.Equal(HttpStatusCode.OK, place.StatusCode);
            var body = await ReadJsonAsync(place);
            return body.GetProperty("id").GetGuid();
        }

        private async Task<HttpResponseMessage> SendSignedWebhookAsync(string body)
        {
            const string timestamp = "1710000000";
            var signature = RevolutWebhookSignature.SignForTests(
                SigningSecret,
                timestamp,
                body
            );
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/webhooks/revolut"
            )
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(
                "Revolut-Request-Timestamp",
                timestamp
            );
            request.Headers.TryAddWithoutValidation("Revolut-Signature", signature);
            return await _client.SendAsync(request);
        }

        private static object ValidShipTo()
        {
            return new
            {
                contactName = "Ada Lovelace",
                contactPhone = "+442074071234",
                addressLine1 = "6 Southwark Street",
                addressLine2 = "London",
                postcode = "SE1 1TQ",
                country = "United Kingdom",
                deliveryInstructions = "Side entrance",
            };
        }

        private async Task<HttpResponseMessage> PlaceOrderAsync(
            string jwt,
            object body
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/shop/orders"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PutLineAsync(
            string jwt,
            int locationId,
            string skuId,
            int quantity
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/shop/cart/lines"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    locationId,
                    skuId,
                    quantity,
                }),
                Encoding.UTF8,
                "application/json"
            );
            return await _client.SendAsync(request);
        }

        private async Task SetBillingStatusAsync(
            int restaurantId,
            string billingStatus
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var account = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.BillingStatus = billingStatus;
            await context.SaveChangesAsync();
        }

        private async Task SetChargebackRestrictedAsync(
            int restaurantId,
            bool restricted
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var account = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.ChargebackRestricted = restricted;
            await context.SaveChangesAsync();
        }

        private async Task<ShopSeed> SeedPaidShopWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Shop Pay Owner",
                Email = $"owner-shop-16-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Shop Pay Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(new BillingAccount
            {
                RestaurantId = restaurant.Id,
                RevolutCustomerId = "cust_shop_16",
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = "default",
            });

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                LocalContact = "Site Contact",
                LocationPhone = "+442074071234",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "Shop Pay Member",
                Email = $"member-shop-16-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Admin,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new ShopSeed(
                memberJwt,
                location.Id,
                restaurant.Id
            );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record ShopSeed(
            string MemberJwt,
            int InScopeLocationId,
            int RestaurantId
        );
    }
}
