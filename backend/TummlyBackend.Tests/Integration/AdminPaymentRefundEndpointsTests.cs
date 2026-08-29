using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class AdminPaymentRefundEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminPaymentRefundEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostPaymentRefund_Returns403_ForOperatorJwt()
        {
            var seeded = await SeedWorkspaceWithTopupAsync();
            using var request = AuthorizedRefundPost(
                seeded.OwnerJwt,
                new
                {
                    restaurantId = seeded.RestaurantId,
                    orderId = seeded.PaymentOrderId,
                },
                idempotencyKey: Guid.NewGuid().ToString("D")
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPaymentRefund_AdminFullRefund_CallsMerchant()
        {
            var seeded = await SeedWorkspaceWithTopupAsync();
            _factory.Merchant.Orders[seeded.PaymentOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: seeded.PaymentOrderId,
                    State: "completed",
                    AmountMinor: 1200,
                    OrderType: RevolutOrderTypes.Payment
                );
            var before = _factory.Merchant.RefundOrderCallCount;
            using var request = AuthorizedRefundPost(
                seeded.TummlyAdminJwt,
                new
                {
                    restaurantId = seeded.RestaurantId,
                    orderId = seeded.PaymentOrderId,
                },
                idempotencyKey: "idem-full-refund-001"
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    body.GetProperty("refundOrderId").GetString()
                )
            );
            Assert.Equal(before + 1, _factory.Merchant.RefundOrderCallCount);
            Assert.Equal(
                seeded.PaymentOrderId,
                _factory.Merchant.LastRefundOrderId
            );
            Assert.Null(_factory.Merchant.LastRefundAmountMinor);
            Assert.Equal(
                "idem-full-refund-001",
                _factory.Merchant.LastRefundIdempotencyKey
            );
        }

        [Fact]
        public async Task PostPaymentRefund_PartialWhileBindable_Refuses()
        {
            var seeded = await SeedWorkspaceWithTopupAsync(bindableQuantity: 50);
            _factory.Merchant.Orders[seeded.PaymentOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: seeded.PaymentOrderId,
                    State: "completed",
                    AmountMinor: 1200,
                    OrderType: RevolutOrderTypes.Payment
                );
            var beforeCalls = _factory.Merchant.RefundOrderCallCount;
            using var request = AuthorizedRefundPost(
                seeded.TummlyAdminJwt,
                new
                {
                    restaurantId = seeded.RestaurantId,
                    orderId = seeded.PaymentOrderId,
                    amountMinor = 600,
                },
                idempotencyKey: Guid.NewGuid().ToString("D")
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "partial_refund_while_bindable",
                body.GetProperty("code").GetString()
            );
            Assert.Equal(beforeCalls, _factory.Merchant.RefundOrderCallCount);
        }

        private static HttpRequestMessage AuthorizedRefundPost(
            string jwt,
            object payload,
            string idempotencyKey
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/admin/payment-refunds"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            request.Headers.TryAddWithoutValidation(
                "Idempotency-Key",
                idempotencyKey
            );
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private async Task<Seeded> SeedWorkspaceWithTopupAsync(
            int bindableQuantity = 100
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Owner",
                Role = "Owner",
                PhoneNumber = "07700900111",
                AccountType = "Single",
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
                Name = "Refund Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            owner.SelectedRestaurantId = restaurant.Id;

            var paymentOrderId = $"ord_pay_{Guid.NewGuid():N}";
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.TopupAllocation,
                    Quantity = bindableQuantity,
                    SourcePaymentRef = paymentOrderId,
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                    ExpiresAtUtc = DateTime.UtcNow.AddMonths(12),
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );

            var tummlyAdmin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(tummlyAdmin);
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                paymentOrderId,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateAdminToken(tummlyAdmin)
            );
        }

        private sealed record Seeded(
            int RestaurantId,
            string PaymentOrderId,
            string OwnerJwt,
            string TummlyAdminJwt
        );
    }
}
