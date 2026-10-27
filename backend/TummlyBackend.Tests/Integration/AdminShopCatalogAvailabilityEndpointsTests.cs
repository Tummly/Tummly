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
            await SetTableTentAsync(mint: false, available: false);

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

        [Fact]
        public async Task UpdateSku_RemovingLastQrPath_Returns409WithCode()
        {
            var adminJwt = await SeedAdminJwtAsync();
            await SetTableTentAsync(mint: true, available: true);

            using var request = AuthorizedPatchJson(
                "/api/admin/shop-catalog/skus/table-tents",
                adminJwt,
                new { mintOnShopFulfilment = false }
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

        [Fact]
        public async Task PromoteInterestProduct_NoQrPath_Returns409WithCode()
        {
            var adminJwt = await SeedAdminJwtAsync();
            var productKey = $"promote-409-{Guid.NewGuid():N}";
            var tentRecipeId = await SeedInterestProductAsync(productKey);
            // The seeded table-tents SKU holds the table-tent slot; free it so the
            // only reason to refuse is the QR path. Restored below (shared store).
            await SetTableTentStatusAsync(ShopMaterialSkuStatuses.Deleted);

            HttpResponseMessage response;
            try
            {
                using var request = AuthorizedPostJson(
                    $"/api/admin/shop-interest/products/{productKey}/promote",
                    adminJwt,
                    new
                    {
                        placementRecipeId = tentRecipeId,
                        category = "tabletop",
                        packs = new[] { new { qty = 1, netPence = 999 } },
                        deliveryNetPence = 100,
                        mintOnShopFulfilment = false,
                    }
                );
                response = await _client.SendAsync(request);
            }
            finally
            {
                await SetTableTentStatusAsync(ShopMaterialSkuStatuses.Published);
            }

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

        /// <summary>
        /// Inserts an active interest product with one image and returns the
        /// table-tent recipe id. The guard runs after the image, dimension and
        /// title checks, so these rows are enough to reach it.
        /// </summary>
        private async Task<int> SeedInterestProductAsync(string productKey)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var product = new ShopInterestProduct
            {
                ProductKey = productKey,
                Title = "Tray liner",
                Description = "Print a tray liner",
                Material = "Paper",
                Dimensions = "A4",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            product.Images.Add(
                new ShopInterestProductImage
                {
                    StorageKey = $"interest/{productKey}.png",
                    ContentType = "image/png",
                    SortOrder = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            context.ShopInterestProducts.Add(product);
            await context.SaveChangesAsync();

            return await context.PlacementRecipes
                .Where(row => row.Slug == "table-tent")
                .Select(row => row.Id)
                .SingleAsync();
        }

        private async Task SetTableTentStatusAsync(string status)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var sku = await context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == "table-tents"
            );
            sku.Status = status;
            await context.SaveChangesAsync();
        }

        private async Task SetTableTentAsync(bool mint, bool available)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var sku = await context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == "table-tents"
            );
            sku.MintOnShopFulfilment = mint;
            sku.AvailableInShop = available;
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

        private static HttpRequestMessage AuthorizedPatchJson(
            string url,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
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
