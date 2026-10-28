using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using System.Text.Json;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
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
        private static readonly AdminShopActor Actor = new(1, "admin@example.com");

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
                _catalog,
                new AdminAuditService(_context, TimeProvider.System)
            );
        }

        [Fact]
        public async Task Promote_HidesInterest_PublishesShopSku_LeavesCheckoutOff_KeepsRegistrations()
        {
            await _catalog.DeleteAsync("table-tents", Actor);
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
                },
                Actor
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
            Assert.False(sku.AvailableInShop);
            Assert.NotEmpty(sku.Images);

            Assert.Equal(
                1,
                await _context.ShopInterestRegistrations.CountAsync(row =>
                    row.ProductKey == product.ProductKey
                )
            );
        }

        [Fact]
        public async Task InterestPromote_NoQrPath_Returns409()
        {
            await _catalog.DeleteAsync("table-tents", Actor);
            var product = await EnsureInterestProductAsync("tray-liner");
            await UploadInterestImageAsync(product.ProductKey);
            product.Dimensions = "A4";
            product.Material = "Paper";
            await _context.SaveChangesAsync();

            var tentRecipe = await RecipeIdAsync("table-tent");
            var (promoted, error) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = tentRecipe,
                    Category = "tabletop",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 999 }],
                    DeliveryNetPence = 100,
                    MintOnShopFulfilment = false,
                },
                Actor
            );

            Assert.Null(promoted);
            Assert.Equal("sku_has_no_qr_path", error);
            var stillActive = await _context.ShopInterestProducts
                .AsNoTracking()
                .SingleAsync(row => row.ProductKey == product.ProductKey);
            Assert.True(stillActive.IsActive);
            Assert.Null(stillActive.LinkedSkuId);
            Assert.False(
                await _context.ShopMaterialSkus.AnyAsync(row =>
                    row.SkuId == product.ProductKey
                )
            );
        }

        [Fact]
        public async Task InterestPromote_NoQrPath_ExistingDraft_LeavesSkuUnchanged()
        {
            await _catalog.DeleteAsync("table-tents", Actor);
            var product = await EnsureInterestProductAsync("tray-liner");
            await UploadInterestImageAsync(product.ProductKey);
            product.Dimensions = "A4";
            product.Material = "Paper";
            await _context.SaveChangesAsync();

            var tentRecipe = await RecipeIdAsync("table-tent");
            var (draft, draftError) = await _catalog.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    SkuId = product.ProductKey,
                    Title = "Tray liner draft",
                    Category = "tabletop",
                    Description = "Draft listing",
                    Material = "Paper",
                    Dimensions = "A4",
                    PlacementRecipeId = tentRecipe,
                    UnitNetPence = 999,
                    MintOnShopFulfilment = true,
                    AboutBody = "About draft",
                },
                Actor
            );
            Assert.Null(draftError);
            Assert.NotNull(draft);
            var before = await _context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(row => row.SkuId == product.ProductKey);

            var (promoted, error) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = tentRecipe,
                    Category = "tabletop",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 999 }],
                    DeliveryNetPence = 100,
                    MintOnShopFulfilment = false,
                },
                Actor
            );

            Assert.Null(promoted);
            Assert.Equal("sku_has_no_qr_path", error);
            var after = await _context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(row => row.SkuId == product.ProductKey);
            Assert.Equal(ShopMaterialSkuStatuses.Draft, after.Status);
            Assert.False(after.AvailableInShop);
            Assert.True(after.MintOnShopFulfilment);
            Assert.Equal(before.Title, after.Title);
            Assert.Equal(before.UpdatedAtUtc, after.UpdatedAtUtc);
            Assert.Null(before.PublishedAtUtc);
            Assert.Null(after.PublishedAtUtc);
            Assert.True(
                (await _context.ShopInterestProducts
                    .AsNoTracking()
                    .SingleAsync(row => row.ProductKey == product.ProductKey))
                .IsActive
            );
        }

        [Fact]
        public async Task Demote_RestoresInterest_AndUnpublishesSku()
        {
            await _catalog.DeleteAsync("table-tents", Actor);
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
                },
                Actor
            );
            Assert.Null(promoteError);
            Assert.NotNull(promoted);

            var (demoted, demoteError) = await _service.DemoteBySkuIdAsync(
                product.ProductKey,
                Actor
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
                },
                Actor
            );
            Assert.Null(rePromoteError);
            Assert.NotNull(rePromoted);
            Assert.False(rePromoted!.IsActive);
            Assert.Equal(product.ProductKey, rePromoted.LinkedSkuId);
        }

        [Fact]
        public async Task Promote_RejectsWhenMissingImageOrDimensions()
        {
            await _catalog.DeleteAsync("window-sticker", Actor);
            var product = await EnsureInterestProductAsync("receipt-label");
            var recipe = await RecipeIdAsync("window-sticker");

            var (noImage, imageError) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = recipe,
                    Category = "window",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 500 }],
                },
                Actor
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
                },
                Actor
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

        [Fact]
        public async Task Promote_RefusesRecord_WithoutPricedPack()
        {
            await _catalog.DeleteAsync("table-tents", Actor);
            var product = await EnsureInterestProductAsync("tray-liner");
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
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 0 }],
                    DeliveryNetPence = 299,
                },
                Actor
            );

            Assert.Null(promoted);
            Assert.Equal("Promote requires a pack with a price before promote.", error);
            Assert.False(
                await _context.ShopMaterialSkus.AnyAsync(row => row.SkuId == product.ProductKey)
            );
        }

        [Fact]
        public async Task Promote_ThenDemote_WriteAuditWithActorProductKeyAndSkuId()
        {
            await _catalog.DeleteAsync("table-tents", Actor);
            var product = await EnsureInterestProductAsync("tray-liner");
            await UploadInterestImageAsync(product.ProductKey);
            product.Dimensions = "210mm x 297mm";
            product.Material = "Paper";
            await _context.SaveChangesAsync();

            var tentRecipe = await RecipeIdAsync("table-tent");
            var (_, promoteError) = await _service.PromoteAsync(
                product.ProductKey,
                new AdminShopInterestPromoteRequest
                {
                    PlacementRecipeId = tentRecipe,
                    Category = "tabletop",
                    Packs = [new ShopMaterialPackRow { Qty = 1, NetPence = 1500 }],
                    DeliveryNetPence = 299,
                },
                Actor
            );
            Assert.Null(promoteError);

            var promote = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.ShopInterestPromote
            );
            Assert.Equal(Actor.Identity, promote.ActorIdentity);
            Assert.Equal(Actor.AdminUserId, promote.ActorAdminUserId);
            Assert.Equal(AdminAuditTargetTypes.ShopInterestProduct, promote.TargetType);
            Assert.Equal(product.ProductKey, promote.TargetId);
            using (var detail = JsonDocument.Parse(promote.DetailJson!))
            {
                Assert.Equal(product.ProductKey, detail.RootElement.GetProperty("productKey").GetString());
                Assert.Equal(product.ProductKey, detail.RootElement.GetProperty("skuId").GetString());
            }

            var (_, demoteError) = await _service.DemoteBySkuIdAsync(product.ProductKey, Actor);
            Assert.Null(demoteError);

            var demote = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.ShopInterestDemote
            );
            Assert.Equal(Actor.Identity, demote.ActorIdentity);
            Assert.Equal(product.ProductKey, demote.TargetId);
            using var demoteDetail = JsonDocument.Parse(demote.DetailJson!);
            Assert.Equal(product.ProductKey, demoteDetail.RootElement.GetProperty("skuId").GetString());
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
