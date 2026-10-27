using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AdminShopCatalogAvailabilityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminShopCatalogAvailabilityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SetAvailableInShop_NoQrPath_Returns409WithCode()
        {
            var adminJwt = await SeedAdminJwtAsync();
            await SetTableTentMintAsync(false);

            using var request = AuthorizedPostJson(
                "/api/admin/shop-catalog/skus/table-tents/available-in-shop",
                adminJwt,
                new { availableInShop = true }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "sku_has_no_qr_path",
                body.GetProperty("code").GetString()
            );
            Assert.False(
                string.IsNullOrWhiteSpace(body.GetProperty("message").GetString())
            );
        }

        private async Task SetTableTentMintAsync(bool mint)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var sku = await context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == "table-tents"
            );
            sku.MintOnShopFulfilment = mint;
            await context.SaveChangesAsync();
        }

        private async Task<string> SeedAdminJwtAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();
            var admin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-shop-qr-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();
            return jwtService.GenerateAdminToken(admin);
        }

        private static HttpRequestMessage AuthorizedPostJson(
            string url,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }
    }
}
