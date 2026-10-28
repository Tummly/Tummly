using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Migrations;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class ShopProductArtworkSqlTests
    {
        private const string Placeholder = "legacy:/assets/images/shop/tummly-sticker.png";
        private const string AdminUpload = "shop-materials/0123456789abcdef.png";

        private readonly SqlServerFixture _fixture;

        public ShopProductArtworkSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task Migration_CoreSkus_SwapPlaceholderOnly_KeepUploadsAndDeletedRows()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();
            // Other tests start the app, which seeds these SkuIds; free them in this transaction.
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ShopMaterialSkus SET Status = 'Deleted' WHERE SkuId IN ('table-tents', 'window-stickers', 'qr-guest-cards', 'custom-sticker')"
            );

            var tent = await AddSkuAsync(context, "table-tents", ShopMaterialSkuStatuses.Published, Placeholder);
            var window = await AddSkuAsync(context, "window-stickers", ShopMaterialSkuStatuses.Published, AdminUpload, Placeholder);
            var deletedCards = await AddSkuAsync(context, "qr-guest-cards", ShopMaterialSkuStatuses.Deleted, Placeholder);
            var custom = await AddSkuAsync(context, "custom-sticker", ShopMaterialSkuStatuses.Published, Placeholder);

            await context.Database.ExecuteSqlRawAsync(ShopProductArtwork.ArtworkSql);
            context.ChangeTracker.Clear();

            var tentImage = Assert.Single(await SkuImagesAsync(context, tent));
            Assert.Equal("legacy:/assets/images/shop/products/table-tents.webp", tentImage.StorageKey);
            Assert.Equal("image/webp", tentImage.ContentType);

            Assert.Equal(
                [AdminUpload, "legacy:/assets/images/shop/products/window-stickers.webp"],
                (await SkuImagesAsync(context, window)).Select(row => row.StorageKey)
            );
            Assert.Equal(
                [Placeholder],
                (await SkuImagesAsync(context, deletedCards)).Select(row => row.StorageKey)
            );
            Assert.Equal(
                [Placeholder],
                (await SkuImagesAsync(context, custom)).Select(row => row.StorageKey)
            );

            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Migration_InterestProducts_AddArtworkOnlyWhenNoImage_RunsOnce()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();
            // Other tests start the app, which seeds these keys; free them in this transaction.
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ShopInterestProducts SET ProductKey = CONCAT('seeded-', Id) WHERE ProductKey IN ('receipt-label', 'tray-liner', 'custom-interest')"
            );

            var receipt = await AddInterestAsync(context, "receipt-label");
            var tray = await AddInterestAsync(context, "tray-liner", AdminUpload);
            var custom = await AddInterestAsync(context, "custom-interest");

            await context.Database.ExecuteSqlRawAsync(ShopProductArtwork.ArtworkSql);
            await context.Database.ExecuteSqlRawAsync(ShopProductArtwork.ArtworkSql);
            context.ChangeTracker.Clear();

            var receiptImage = Assert.Single(await InterestImagesAsync(context, receipt));
            Assert.Equal("legacy:/assets/images/shop/products/receipt-label.webp", receiptImage.StorageKey);
            Assert.Equal("image/webp", receiptImage.ContentType);
            Assert.Equal(0, receiptImage.SortOrder);

            Assert.Equal(
                [AdminUpload],
                (await InterestImagesAsync(context, tray)).Select(row => row.StorageKey)
            );
            Assert.Empty(await InterestImagesAsync(context, custom));

            await tx.RollbackAsync();
        }

        private static async Task<int> AddSkuAsync(
            ApplicationDbContext context,
            string skuId,
            string status,
            params string[] storageKeys
        )
        {
            var unique = Guid.NewGuid().ToString("N")[..12];
            var recipe = new PlacementRecipe
            {
                Slug = $"artwork-test-{unique}",
                Label = "Artwork test",
                PlacementKey = $"ArtworkTest{unique}",
            };
            var sku = new ShopMaterialSku
            {
                SkuId = skuId,
                Title = skuId,
                Category = "tabletop",
                QrType = "TableTent",
                Status = status,
                PlacementRecipe = recipe,
            };
            for (var i = 0; i < storageKeys.Length; i++)
            {
                sku.Images.Add(
                    new ShopMaterialSkuImage { StorageKey = storageKeys[i], SortOrder = i }
                );
            }

            context.ShopMaterialSkus.Add(sku);
            await context.SaveChangesAsync();
            return sku.Id;
        }

        private static async Task<int> AddInterestAsync(
            ApplicationDbContext context,
            string productKey,
            params string[] storageKeys
        )
        {
            var product = new ShopInterestProduct
            {
                ProductKey = productKey,
                Title = productKey,
                Description = productKey,
                IsActive = true,
            };
            foreach (var key in storageKeys)
            {
                product.Images.Add(new ShopInterestProductImage { StorageKey = key });
            }

            context.ShopInterestProducts.Add(product);
            await context.SaveChangesAsync();
            return product.Id;
        }

        private static Task<List<ShopMaterialSkuImage>> SkuImagesAsync(
            ApplicationDbContext context,
            int skuId
        ) =>
            context.ShopMaterialSkuImages
                .AsNoTracking()
                .Where(row => row.SkuId == skuId)
                .OrderBy(row => row.SortOrder)
                .ToListAsync();

        private static Task<List<ShopInterestProductImage>> InterestImagesAsync(
            ApplicationDbContext context,
            int productId
        ) =>
            context.ShopInterestProductImages
                .AsNoTracking()
                .Where(row => row.ShopInterestProductId == productId)
                .ToListAsync();
    }
}
