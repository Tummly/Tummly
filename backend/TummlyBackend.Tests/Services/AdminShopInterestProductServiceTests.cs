using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Services
{
    public sealed class AdminShopInterestProductServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly InMemoryShopCatalogImageStorage _images = new();
        private readonly AdminShopCatalogService _catalog;
        private readonly AdminShopInterestProductService _service;

        public AdminShopInterestProductServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            ShopMaterialsCatalogSeed
                .EnsureSeededAsync(_context)
                .GetAwaiter()
                .GetResult();
            _catalog = new AdminShopCatalogService(
                _context,
                _images,
                new AdminAuditService(_context, TimeProvider.System)
            );
            _service = new AdminShopInterestProductService(
                _context,
                _images,
                _catalog
            );
        }

        [Fact]
        public async Task Promote_HidesInterest_PublishesShopSku_KeepsRegistrations()
        {
            await _catalog.DeleteAsync("table-tents");
            var product = await EnsureInterestProductAsync("tray-liner");
            await RegisterInterestAsync(product, userId: 11, locationId: 22);
            await UploadInterestImageAsync(product.ProductKey);

            product.Dimensions = "210mm x 297mm";
            product.Material = "Paper";
            await _context.SaveChangesAsync();

            var tentRecipe = await RecipeIdAsync("table-tent");
            var (promoted, error) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = tentRecipe,
                    Category = "tabletop",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 1500 }],
                    DeliveryNetPence = 299,
                }
            );

            Assert.Null(error);
            Assert.NotNull(promoted);
            Assert.False(promoted!.IsActive);
            Assert.Equal(product.ProductKey, promoted.LinkedSkuId);
            Assert.Equal(1, promoted.RegistrationCount);

            var activeInterest = await _context.ShopInterestProducts
                .AsNoTracking()
                .Where(row => row.IsActive)
                .Select(row => row.ProductKey)
                .ToListAsync();
            Assert.DoesNotContain(product.ProductKey, activeInterest);

            var sku = await _context.ShopMaterialSkus
                .AsNoTracking()
                .Include(row => row.Images)
                .SingleAsync(row => row.SkuId == product.ProductKey);
            Assert.Equal(ShopMaterialSkuStatuses.Published, sku.Status);
            Assert.True(sku.AvailableInShop);
            Assert.NotEmpty(sku.Images);

            Assert.Equal(
                1,
                await _context.ShopInterestRegistrations.CountAsync(row =>
                    row.ProductKey == product.ProductKey
                )
            );
        }

        [Fact]
        public async Task Demote_RestoresInterest_AndUnpublishesSku()
        {
            await _catalog.DeleteAsync("table-tents");
            var product = await EnsureInterestProductAsync("tray-liner");
            await UploadInterestImageAsync(product.ProductKey);
            product.Dimensions = "A4";
            product.Material = "Paper";
            await _context.SaveChangesAsync();

            var tentRecipe = await RecipeIdAsync("table-tent");
            var (promoted, promoteError) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = tentRecipe,
                    Category = "tabletop",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 999 }],
                    DeliveryNetPence = 100,
                }
            );
            Assert.Null(promoteError);
            Assert.NotNull(promoted);

            var (demoted, demoteError) = await _service.DemoteBySkuIdAsync(
                product.ProductKey
            );
            Assert.Null(demoteError);
            Assert.NotNull(demoted);
            Assert.True(demoted!.IsActive);
            Assert.Null(demoted.LinkedSkuId);

            var sku = await _context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(row => row.SkuId == product.ProductKey);
            Assert.Equal(ShopMaterialSkuStatuses.Draft, sku.Status);
            Assert.False(sku.AvailableInShop);

            var interest = await _context.ShopInterestProducts
                .AsNoTracking()
                .Include(row => row.Images)
                .SingleAsync(row => row.ProductKey == product.ProductKey);
            Assert.True(interest.IsActive);
            Assert.NotEmpty(interest.Images);

            // Re-promote reuses the Draft SKU (placement still held by that draft).
            var (rePromoted, rePromoteError) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = tentRecipe,
                    Category = "tabletop",
                    Packs = [new ShopMaterialPackRow { Qty = 2, NetPence = 1800 }],
                    DeliveryNetPence = 150,
                }
            );
            Assert.Null(rePromoteError);
            Assert.NotNull(rePromoted);
            Assert.False(rePromoted!.IsActive);
            Assert.Equal(product.ProductKey, rePromoted.LinkedSkuId);
        }

        [Fact]
        public async Task Promote_RejectsWhenMissingImageOrDimensions()
        {
            await _catalog.DeleteAsync("window-sticker");
            var product = await EnsureInterestProductAsync("receipt-label");
            var recipe = await RecipeIdAsync("window-sticker");

            var (noImage, imageError) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = recipe,
                    Category = "window",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 500 }],
                }
            );
            Assert.Null(noImage);
            Assert.Contains("image", imageError, StringComparison.OrdinalIgnoreCase);

            await UploadInterestImageAsync(product.ProductKey);
            var (noDims, dimsError) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = recipe,
                    Category = "window",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 500 }],
                }
            );
            Assert.Null(noDims);
            Assert.Contains(
                "Dimensions",
                dimsError,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private async Task<ShopInterestProduct> EnsureInterestProductAsync(
            string productKey
        )
        {
            var existing = await _context.ShopInterestProducts
                .Include(row => row.Images)
                .FirstOrDefaultAsync(row => row.ProductKey == productKey);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.LinkedSkuId = null;
                existing.Dimensions = string.Empty;
                existing.Material = string.Empty;
                if (existing.Images.Count > 0)
                {
                    _context.ShopInterestProductImages.RemoveRange(existing.Images);
                    existing.Images.Clear();
                }

                await _context.SaveChangesAsync();
                return existing;
            }

            var product = new ShopInterestProduct
            {
                ProductKey = productKey,
                Title = productKey,
                Description = "Test interest product",
                Material = string.Empty,
                Dimensions = string.Empty,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            _context.ShopInterestProducts.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        private async Task RegisterInterestAsync(
            ShopInterestProduct product,
            int userId,
            int locationId
        )
        {
            _context.ShopInterestRegistrations.Add(
                new ShopInterestRegistration
                {
                    ShopInterestProductId = product.Id,
                    ProductKey = product.ProductKey,
                    ProductTitleSnapshot = product.Title,
                    RestaurantId = 1,
                    RestaurantNameSnapshot = "Test Venue",
                    LocationId = locationId,
                    LocationNameSnapshot = "Main",
                    UserId = userId,
                    UserFullNameSnapshot = "Owner",
                    UserEmailSnapshot = "owner@example.com",
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task UploadInterestImageAsync(string productKey)
        {
            await using var stream = new MemoryStream([1, 2, 3, 4]);
            var formFile = new FormFile(stream, 0, stream.Length, "file", "item.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };
            var (image, error) = await _service.UploadImageAsync(
                productKey,
                formFile
            );
            Assert.Null(error);
            Assert.NotNull(image);
        }

        private async Task<int> RecipeIdAsync(string slug)
        {
            return await _context.PlacementRecipes
                .Where(row => row.Slug == slug)
                .Select(row => row.Id)
                .FirstAsync();
        }

        public void Dispose() => _context.Dispose();
    }
}
