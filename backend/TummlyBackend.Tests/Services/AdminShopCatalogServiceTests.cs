using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
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
            Assert.Contains(
                skus,
                sku => sku.SkuId == "table-tents" && sku.Images.Count == 1
            );
        }

        [Fact]
        public async Task Create_RejectsDuplicateQrType_WhileActiveProductExists()
        {
            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Second Offer Card",
                    Category = "payment",
                    Description = "Duplicate type",
                    Material = "Card",
                    Dimensions = "85mm x 55mm",
                    QrType = "OfferCard",
                    UnitNetPence = 1800,
                }
            );

            Assert.Null(created);
            Assert.Contains("OfferCard", createError, StringComparison.Ordinal);
            Assert.Contains("already exists", createError, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Create_AllowsQrType_AfterExistingProductDeleted()
        {
            var (deleted, deleteError) = await _service.DeleteAsync("offer-card");
            Assert.Null(deleteError);
            Assert.Equal(ShopMaterialSkuStatuses.Deleted, deleted!.Status);

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Replacement Offer Card",
                    Category = "payment",
                    Description = "Replacement listing",
                    Material = "Card",
                    Dimensions = "85mm x 55mm",
                    QrType = "OfferCard",
                    UnitNetPence = 1900,
                    AboutBody = "Replacement about",
                }
            );

            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("OfferCard", created!.QrType);
            Assert.Equal("Draft", created.Status);
        }

        [Fact]
        public async Task Create_ThenPublish_RequiresImage()
        {
            await _service.DeleteAsync("table-tents");

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Extra Table Tent",
                    Category = "tabletop",
                    Description = "Extra stock listing",
                    Material = "Acrylic",
                    Dimensions = "A6",
                    QrType = "TableTent",
                    UnitNetPence = 2000,
                    AboutBody = "About extra tents",
                }
            );
            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("Draft", created!.Status);

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
        public async Task DbCatalog_HidesDraft_FromOperatorList()
        {
            await _service.DeleteAsync("window-stickers");

            await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Hidden Draft",
                    Category = "window",
                    Description = "Not live",
                    Material = "Vinyl",
                    Dimensions = "150mm",
                    QrType = "WindowSticker",
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

        public void Dispose() => _context.Dispose();
    }
}
