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
    public sealed class AdminShopCatalogServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly InMemoryShopCatalogImageStorage _images = new();
        private readonly AdminShopCatalogService _service;

        public AdminShopCatalogServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            ShopMaterialsCatalogSeed
                .EnsureSeededAsync(_context)
                .GetAwaiter()
                .GetResult();
            _service = new AdminShopCatalogService(_context, _images);
        }

        [Fact]
        public async Task Seed_PublishesThreeSkus_WithPlanTags()
        {
            var skus = await _service.ListSkusAsync();
            Assert.Equal(3, skus.Count);
            Assert.All(skus, sku => Assert.Equal("Published", sku.Status));
            Assert.All(skus, sku => Assert.True(sku.PlacementRecipeId > 0));
            Assert.Contains(
                skus,
                sku => sku.SkuId == "table-tents" && sku.Images.Count == 1
            );
        }

        [Fact]
        public async Task ListPlacementRecipes_MarksTakenSlotsUnavailable()
        {
            var recipes = await _service.ListPlacementRecipesAsync();
            Assert.Equal(3, recipes.Count);
            Assert.All(recipes, recipe => Assert.False(recipe.IsAvailable));
        }

        [Fact]
        public async Task Create_RejectsDuplicatePlacementRecipe_WhileActiveProductExists()
        {
            var offerRecipe = await RecipeIdAsync("offer-card");
            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Second Offer Card",
                    Category = "payment",
                    Description = "Duplicate type",
                    Material = "Card",
                    Dimensions = "85mm x 55mm",
                    PlacementRecipeId = offerRecipe,
                    UnitNetPence = 1800,
                }
            );

            Assert.Null(created);
            Assert.Contains("already exists", createError, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Create_AllowsPlacementRecipe_AfterExistingProductDeleted()
        {
            var (deleted, deleteError) = await _service.DeleteAsync("offer-card");
            Assert.Null(deleteError);
            Assert.Equal(ShopMaterialSkuStatuses.Deleted, deleted!.Status);

            var offerRecipe = await RecipeIdAsync("offer-card");
            var recipes = await _service.ListPlacementRecipesAsync();
            Assert.Contains(
                recipes,
                row => row.Id == offerRecipe && row.IsAvailable
            );

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Replacement Offer Card",
                    Category = "payment",
                    Description = "Replacement listing",
                    Material = "Card",
                    Dimensions = "85mm x 55mm",
                    PlacementRecipeId = offerRecipe,
                    UnitNetPence = 1900,
                    AboutBody = "Replacement about",
                }
            );

            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("OfferCard", created!.QrType);
            Assert.Equal(offerRecipe, created.PlacementRecipeId);
            Assert.Equal("Draft", created.Status);
        }

        [Fact]
        public async Task Create_ReusesSoftDeletedSkuId()
        {
            await _service.DeleteAsync("offer-card");
            var offerRecipe = await RecipeIdAsync("offer-card");

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    SkuId = "offer-card",
                    Title = "Offer Card Reloaded",
                    Category = "payment",
                    Description = "Same sku id after soft delete",
                    Material = "Card",
                    Dimensions = "85mm x 55mm",
                    PlacementRecipeId = offerRecipe,
                    UnitNetPence = 1800,
                }
            );

            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("offer-card", created!.SkuId);
            Assert.Equal("Draft", created.Status);
        }

        [Fact]
        public async Task Create_ThenPublish_RequiresImage()
        {
            await _service.DeleteAsync("table-tents");
            var tentRecipe = await RecipeIdAsync("table-tent");

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Extra Table Tent",
                    Category = "tabletop",
                    Description = "Extra stock listing",
                    Material = "Acrylic",
                    Dimensions = "A6",
                    PlacementRecipeId = tentRecipe,
                    UnitNetPence = 2000,
                    AboutBody = "About extra tents",
                }
            );
            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("Draft", created!.Status);
            Assert.Equal("TableTent", created.QrType);

            var (publishedFail, publishError) = await _service.PublishAsync(
                created.SkuId
            );
            Assert.Null(publishedFail);
            Assert.Contains("image", publishError, StringComparison.OrdinalIgnoreCase);

            await using var stream = new MemoryStream([1, 2, 3, 4]);
            var formFile = new FormFile(stream, 0, stream.Length, "file", "tent.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };
            var (image, uploadError) = await _service.UploadImageAsync(
                created.SkuId,
                formFile
            );
            Assert.Null(uploadError);
            Assert.NotNull(image);

            var (published, publishOkError) = await _service.PublishAsync(
                created.SkuId
            );
            Assert.Null(publishOkError);
            Assert.Equal("Published", published!.Status);
        }

        [Fact]
        public async Task Update_RoundTripsPacksDeliveryAndAdminOnlyCost()
        {
            var (updated, error) = await _service.UpdateSkuAsync(
                "table-tents",
                new AdminShopCatalogSkuWriteRequest
                {
                    Packs =
                    [
                        new ShopMaterialPackRow { Qty = 5, NetPence = 999 },
                        new ShopMaterialPackRow { Qty = 10, NetPence = 1699 },
                        new ShopMaterialPackRow { Qty = 20, NetPence = 2999 },
                    ],
                    DeliveryNetPence = 399,
                    SupplierName = "PrintCo UK",
                    InternalCostNetPence = 450,
                }
            );

            Assert.Null(error);
            Assert.NotNull(updated);
            Assert.Equal(3, updated!.Packs.Count);
            Assert.Equal(5, updated.Packs[0].Qty);
            Assert.Equal(999, updated.Packs[0].NetPence);
            Assert.Equal(999, updated.UnitNetPence);
            Assert.Equal(5, updated.MinOrderQty);
            Assert.Equal(399, updated.DeliveryNetPence);
            Assert.Equal("PrintCo UK", updated.SupplierName);
            Assert.Equal(450, updated.InternalCostNetPence);

            var catalog = new DbMaterialsCatalog(_context);
            var detail = catalog.TryBuildDetail("table-tents");
            Assert.NotNull(detail);
            Assert.Equal(999, detail!.UnitNetPence);
            Assert.Equal(399, detail.DeliveryNetPence);
            Assert.Equal(3, detail.Packs.Count);
            // Operator catalog must not expose admin-only cost fields.
            var json = System.Text.Json.JsonSerializer.Serialize(detail);
            Assert.DoesNotContain("supplier", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("internalCost", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DbCatalog_HidesDraft_FromOperatorList()
        {
            await _service.DeleteAsync("window-stickers");
            var windowRecipe = await RecipeIdAsync("window-sticker");

            await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Hidden Draft",
                    Category = "window",
                    Description = "Not live",
                    Material = "Vinyl",
                    Dimensions = "150mm",
                    PlacementRecipeId = windowRecipe,
                    UnitNetPence = 1000,
                }
            );

            var catalog = new DbMaterialsCatalog(_context);
            var list = catalog.BuildList();
            Assert.DoesNotContain(list, item => item.SkuId.Contains("hidden"));
            Assert.Contains(list, item => item.SkuId == "table-tents");
            Assert.Equal(
                ShopMaterialCatalogMapper.CatalogVersionId,
                catalog.CurrentCatalogId
            );
        }

        private async Task<int> RecipeIdAsync(string slug)
        {
            var id = await _context.PlacementRecipes
                .Where(row => row.Slug == slug)
                .Select(row => row.Id)
                .FirstAsync();
            return id;
        }

        public void Dispose() => _context.Dispose();
    }
}
