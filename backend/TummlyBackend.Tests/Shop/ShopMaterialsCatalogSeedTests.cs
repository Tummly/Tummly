using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Shop
{
    public sealed class ShopMaterialsCatalogSeedTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public ShopMaterialsCatalogSeedTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            _context = new ApplicationDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task EnsureSeeded_PublishesExactlyFiveCoreSkus_WithLaunchRoutesAndAvailability()
        {
            await ShopMaterialsCatalogSeed.EnsureSeededAsync(_context);

            var published = await _context.ShopMaterialSkus
                .Include(row => row.PlacementRecipe)
                .Where(row => row.Status == ShopMaterialSkuStatuses.Published)
                .OrderBy(row => row.SkuId)
                .ToListAsync();

            Assert.Equal(5, published.Count);
            Assert.DoesNotContain(published, row => row.SkuId == "offer-card");

            AssertCoreSku(
                published,
                "qr-counter-display",
                "QR Counter Display",
                "OfferCard",
                PlacementPrintRoutes.EmbeddedPdf,
                availableInShop: true,
                complimentary: true
            );
            AssertCoreSku(
                published,
                "table-tents",
                "Table Tent",
                "TableTent",
                PlacementPrintRoutes.PatchCsv60,
                availableInShop: false,
                complimentary: false
            );
            AssertCoreSku(
                published,
                "table-talker",
                "Table Talker",
                "TableTalker",
                PlacementPrintRoutes.PatchCsv60,
                availableInShop: false,
                complimentary: false
            );
            AssertCoreSku(
                published,
                "window-stickers",
                "Window Sticker",
                "WindowSticker",
                PlacementPrintRoutes.PatchCsv60,
                availableInShop: false,
                complimentary: false
            );
            AssertCoreSku(
                published,
                "qr-guest-cards",
                "QR Guest Cards",
                "GuestCard",
                PlacementPrintRoutes.GuestCardCsv,
                availableInShop: false,
                complimentary: false
            );

            foreach (var sku in published)
            {
                Assert.True(
                    ShopLaunchCommercialDefaults.TryForSkuId(sku.SkuId, out var commercial)
                );
                Assert.Equal(commercial.DeliveryNetPence, sku.DeliveryNetPence);
                Assert.Equal(
                    ShopMaterialPacks.Serialize(commercial.Packs),
                    sku.PacksJson
                );
            }
        }

        [Fact]
        public async Task EnsureSeeded_RetiresOfferCard_WhenLegacyThreeSkuCatalogExists()
        {
            await SeedLegacyThreeProductsAsync();

            await ShopMaterialsCatalogSeed.EnsureSeededAsync(_context);

            var published = await _context.ShopMaterialSkus
                .Where(row => row.Status == ShopMaterialSkuStatuses.Published)
                .Select(row => row.SkuId)
                .ToListAsync();

            Assert.Equal(5, published.Count);
            Assert.Contains("qr-counter-display", published);
            Assert.DoesNotContain("offer-card", published);
            Assert.False(
                await _context.ShopMaterialSkus.AnyAsync(row =>
                    row.SkuId == "offer-card"
                    && row.Status != ShopMaterialSkuStatuses.Deleted
                )
            );

            var counter = await _context.ShopMaterialSkus
                .Include(row => row.PlacementRecipe)
                .SingleAsync(row => row.SkuId == "qr-counter-display");
            Assert.Equal("OfferCard", counter.QrType);
            Assert.Equal(
                PlacementPrintRoutes.EmbeddedPdf,
                counter.PlacementRecipe.PrintRoute
            );
            Assert.True(counter.AvailableInShop);
        }

        
        [Fact]
        public async Task ResolveOrderedQrTypes_MapsCounterDisplayToOfferCard()
        {
            await ShopMaterialsCatalogSeed.EnsureSeededAsync(_context);
            var catalog = new DbMaterialsCatalog(_context);
            var lines = new[]
            {
                new ShopOrderLine
                {
                    CatalogSkuId = ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                    Quantity = 5,
                },
            };
            var ordered = TummlyBackend.Services.PrintReadyQrMaterialsService.ResolveOrderedQrTypes(
                lines,
                catalog
            );
            Assert.Equal(
                new Dictionary<QrType, int> { [QrType.OfferCard] = 5 },
                ordered
            );
        }

        private async Task SeedLegacyThreeProductsAsync()
        {
            var now = DateTime.UtcNow;
            var recipes = new[]
            {
                new PlacementRecipe
                {
                    Slug = "table-tent",
                    Label = "Table Tent",
                    PlacementKey = "TableTent",
                    PrintRoute = PlacementPrintRoutes.EmbeddedPdf,
                    IncludeInComplimentaryStarter = true,
                    IsActive = true,
                    CreatedAtUtc = now,
                },
                new PlacementRecipe
                {
                    Slug = "window-sticker",
                    Label = "Window Sticker",
                    PlacementKey = "WindowSticker",
                    PrintRoute = PlacementPrintRoutes.EmbeddedPdf,
                    IncludeInComplimentaryStarter = true,
                    IsActive = true,
                    CreatedAtUtc = now,
                },
                new PlacementRecipe
                {
                    Slug = "offer-card",
                    Label = "Offer Card",
                    PlacementKey = "OfferCard",
                    PrintRoute = PlacementPrintRoutes.EmbeddedPdf,
                    IncludeInComplimentaryStarter = true,
                    IsActive = true,
                    CreatedAtUtc = now,
                },
            };
            _context.PlacementRecipes.AddRange(recipes);
            await _context.SaveChangesAsync();

            _context.ShopMaterialSkus.AddRange(
                new ShopMaterialSku
                {
                    SkuId = "table-tents",
                    Title = "Table Tent QR",
                    Category = "tabletop",
                    Description = "legacy",
                    Material = "Acrylic",
                    Dimensions = "A6",
                    PlacementRecipeId = recipes[0].Id,
                    QrType = "TableTent",
                    UnitNetPence = 2400,
                    MinOrderQty = 1,
                    PacksJson = "[]",
                    Status = ShopMaterialSkuStatuses.Published,
                    AvailableInShop = true,
                    PublishedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                },
                new ShopMaterialSku
                {
                    SkuId = "window-stickers",
                    Title = "Window Sticker QR",
                    Category = "window",
                    Description = "legacy",
                    Material = "Vinyl",
                    Dimensions = "150mm",
                    PlacementRecipeId = recipes[1].Id,
                    QrType = "WindowSticker",
                    UnitNetPence = 1400,
                    MinOrderQty = 1,
                    PacksJson = "[]",
                    Status = ShopMaterialSkuStatuses.Published,
                    AvailableInShop = true,
                    PublishedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                },
                new ShopMaterialSku
                {
                    SkuId = "offer-card",
                    Title = "Offer Card",
                    Category = "payment",
                    Description = "legacy",
                    Material = "Card",
                    Dimensions = "85mm",
                    PlacementRecipeId = recipes[2].Id,
                    QrType = "OfferCard",
                    UnitNetPence = 1800,
                    MinOrderQty = 1,
                    PacksJson = "[]",
                    Status = ShopMaterialSkuStatuses.Published,
                    AvailableInShop = true,
                    PublishedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                }
            );
            await _context.SaveChangesAsync();
        }

        private static void AssertCoreSku(
            IReadOnlyList<ShopMaterialSku> published,
            string skuId,
            string title,
            string placementKey,
            string printRoute,
            bool availableInShop,
            bool complimentary
        )
        {
            var sku = Assert.Single(published, row => row.SkuId == skuId);
            Assert.Equal(title, sku.Title);
            Assert.Equal(placementKey, sku.QrType);
            Assert.Equal(placementKey, sku.PlacementRecipe.PlacementKey);
            Assert.Equal(printRoute, sku.PlacementRecipe.PrintRoute);
            Assert.Equal(availableInShop, sku.AvailableInShop);
            Assert.Equal(
                complimentary,
                sku.PlacementRecipe.IncludeInComplimentaryStarter
            );
        }
    }
}
