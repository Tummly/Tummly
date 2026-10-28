using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Services
{
    public sealed class AdminShopCatalogServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly InMemoryShopCatalogImageStorage _images = new();
        private readonly AdminShopCatalogService _service;
        private static readonly AdminShopActor Actor = new(1, "admin@example.com");

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
            _service = new AdminShopCatalogService(
                _context,
                _images,
                new AdminAuditService(_context, TimeProvider.System)
            );
        }

        [Fact]
        public async Task Seed_PublishesFiveCoreSkus_WithPlanTagsOnCounter()
        {
            var skus = await _service.ListSkusAsync();
            Assert.Equal(5, skus.Count);
            Assert.All(skus, sku => Assert.Equal("Published", sku.Status));
            Assert.All(skus, sku => Assert.True(sku.PlacementRecipeId > 0));
            Assert.Contains(
                skus,
                sku => sku.SkuId == "table-tents" && sku.Images.Count == 1
            );
            Assert.Contains(
                skus,
                sku =>
                    sku.SkuId == ShopMaterialsCatalogSeed.CounterDisplaySkuId
                    && sku.AvailableInShop
            );
            Assert.DoesNotContain(skus, sku => sku.SkuId == "offer-card");
        }

        [Fact]
        public async Task ListPlacementRecipes_MarksTakenSlotsUnavailable()
        {
            var recipes = await _service.ListPlacementRecipesAsync();
            Assert.Equal(5, recipes.Count);
            Assert.All(recipes, recipe => Assert.False(recipe.IsAvailable));
        }

        [Fact]
        public async Task Create_RejectsDuplicatePlacementRecipe_WhileActiveProductExists()
        {
            var counterRecipe = await RecipeIdAsync(
                ShopMaterialsCatalogSeed.CounterDisplayRecipeSlug
            );
            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Second Counter Display",
                    Category = "payment",
                    Description = "Duplicate type",
                    Material = "Card",
                    Dimensions = "A5",
                    PlacementRecipeId = counterRecipe,
                    UnitNetPence = 999,
                },
                Actor
            );

            Assert.Null(created);
            Assert.Contains("already exists", createError, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Create_AllowsPlacementRecipe_AfterExistingProductDeleted()
        {
            var (deleted, deleteError) = await _service.DeleteAsync(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                Actor
            );
            Assert.Null(deleteError);
            Assert.Equal(ShopMaterialSkuStatuses.Deleted, deleted!.Status);

            var counterRecipe = await RecipeIdAsync(
                ShopMaterialsCatalogSeed.CounterDisplayRecipeSlug
            );
            var recipes = await _service.ListPlacementRecipesAsync();
            Assert.Contains(
                recipes,
                row => row.Id == counterRecipe && row.IsAvailable
            );

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Replacement Counter Display",
                    Category = "payment",
                    Description = "Replacement listing",
                    Material = "Card",
                    Dimensions = "A5",
                    PlacementRecipeId = counterRecipe,
                    UnitNetPence = 999,
                    AboutBody = "Replacement about",
                },
                Actor
            );

            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("OfferCard", created!.QrType);
            Assert.Equal(counterRecipe, created.PlacementRecipeId);
            Assert.Equal("Draft", created.Status);
        }

        [Fact]
        public async Task Create_ReusesSoftDeletedSkuId()
        {
            await _service.DeleteAsync(ShopMaterialsCatalogSeed.CounterDisplaySkuId, Actor);
            var counterRecipe = await RecipeIdAsync(
                ShopMaterialsCatalogSeed.CounterDisplayRecipeSlug
            );

            var (created, createError) = await _service.CreateSkuAsync(
                new AdminShopCatalogSkuWriteRequest
                {
                    SkuId = ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                    Title = "QR Counter Display Reloaded",
                    Category = "payment",
                    Description = "Same sku id after soft delete",
                    Material = "Card",
                    Dimensions = "A5",
                    PlacementRecipeId = counterRecipe,
                    UnitNetPence = 999,
                },
                Actor
            );

            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                created!.SkuId
            );
            Assert.Equal("Draft", created.Status);
        }

        [Fact]
        public async Task Create_ThenPublish_RequiresImage()
        {
            await _service.DeleteAsync("table-tents", Actor);
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
                },
                Actor
            );
            Assert.Null(createError);
            Assert.NotNull(created);
            Assert.Equal("Draft", created!.Status);
            Assert.Equal("TableTent", created.QrType);

            var (publishedFail, publishError) = await _service.PublishAsync(
                created.SkuId,
                Actor
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
                formFile,
                Actor
            );
            Assert.Null(uploadError);
            Assert.NotNull(image);

            var (published, publishOkError) = await _service.PublishAsync(
                created.SkuId,
                Actor
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
                },
                Actor
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
            await _service.DeleteAsync("window-stickers", Actor);
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
                },
                Actor
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

        [Fact]
        public async Task SetAvailableInShop_Off_KeepsPublishedVisible_WithComingSoonBadge()
        {
            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: false,
                Actor
            );

            Assert.Null(error);
            Assert.NotNull(sku);
            Assert.Equal(ShopMaterialSkuStatuses.Published, sku!.Status);
            Assert.False(sku.AvailableInShop);

            var catalog = new DbMaterialsCatalog(_context);
            var list = catalog.BuildList();
            var item = Assert.Single(list, row => row.SkuId == "table-tents");
            Assert.False(item.AvailableInShop);
            Assert.Equal(
                ShopMaterialCatalogMapper.ComingSoonBadgeLabel,
                item.PopularBadge
            );

            var detail = catalog.TryBuildDetail("table-tents");
            Assert.NotNull(detail);
            Assert.False(detail!.AvailableInShop);
            Assert.Equal(
                ShopMaterialCatalogMapper.ComingSoonBadgeLabel,
                detail.PopularBadge
            );
        }

        [Fact]
        public async Task SetAvailableInShop_WritesAdminAudit()
        {
            var before = await _context.AdminAuditEvents.CountAsync();

            var (sku, error) = await _service.SetAvailableInShopAsync(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                availableInShop: false,
                new AdminShopActor(42, "ops@tummly.test")
            );

            Assert.Null(error);
            Assert.NotNull(sku);

            var events = await _context.AdminAuditEvents.ToListAsync();
            Assert.Equal(before + 1, events.Count);
            var audit = Assert.Single(
                events,
                row =>
                    row.Action == AdminAuditActions.ShopCatalogAvailableInShop
            );
            Assert.Equal("ops@tummly.test", audit.ActorIdentity);
            Assert.Equal(42, audit.ActorAdminUserId);
            Assert.Equal(
                AdminAuditTargetTypes.ShopMaterialSku,
                audit.TargetType
            );
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                audit.TargetId
            );
            Assert.Contains(
                "\"availableInShop\":false",
                audit.DetailJson,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SetAvailable_NoQrPath_Returns409SkuHasNoQrPath()
        {
            var tent = await _context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == "table-tents"
            );
            tent.MintOnShopFulfilment = false;
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor
            );

            Assert.Null(sku);
            Assert.Equal("sku_has_no_qr_path", error);
            var stored = await _context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(row => row.SkuId == "table-tents");
            Assert.False(stored.AvailableInShop);
        }

        [Fact]
        public async Task SetAvailable_MintFlagTrue_Succeeds()
        {
            await MakeSupplierAsync("table-tents");

            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor
            );

            Assert.Null(error);
            Assert.NotNull(sku);
            Assert.True(sku!.AvailableInShop);
        }

        [Fact]
        public async Task SetAvailable_StarterRecipeWithoutMintFlag_Succeeds()
        {
            await MakeSupplierAsync(ShopMaterialsCatalogSeed.CounterDisplaySkuId);
            var counter = await _context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == ShopMaterialsCatalogSeed.CounterDisplaySkuId
            );
            counter.AvailableInShop = false;
            counter.MintOnShopFulfilment = false;
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.SetAvailableInShopAsync(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                availableInShop: true,
                Actor
            );

            Assert.Null(error);
            Assert.NotNull(sku);
            Assert.True(sku!.AvailableInShop);
        }

        [Fact]
        public async Task UpdateSku_RemovingLastQrPath_FromAvailableSku_Returns409()
        {
            await MakeSupplierAsync("table-tents");
            var (_, activateError) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor
            );
            Assert.Null(activateError);

            var (sku, error) = await _service.UpdateSkuAsync(
                "table-tents",
                new AdminShopCatalogSkuWriteRequest
                {
                    MintOnShopFulfilment = false,
                    Title = "Changed title",
                },
                Actor
            );

            Assert.Null(sku);
            Assert.Equal("sku_has_no_qr_path", error);
            var stored = await _context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(row => row.SkuId == "table-tents");
            Assert.True(stored.AvailableInShop);
            Assert.True(stored.MintOnShopFulfilment);
            Assert.NotEqual("Changed title", stored.Title);
        }

        [Fact]
        public async Task UpdateSku_LegacyAvailableWithoutQrPath_UnrelatedEdit_Succeeds()
        {
            var tent = await _context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == "table-tents"
            );
            tent.AvailableInShop = true;
            tent.MintOnShopFulfilment = false;
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.UpdateSkuAsync(
                "table-tents",
                new AdminShopCatalogSkuWriteRequest { Title = "Legacy edit" },
                Actor
            );

            Assert.Null(error);
            Assert.NotNull(sku);
            Assert.Equal("Legacy edit", sku!.Title);
            Assert.False(sku.MintOnShopFulfilment);
            Assert.True(sku.AvailableInShop);
        }

        [Fact]
        public async Task UpdateSku_StarterRecipe_MintOff_Succeeds()
        {
            var counter = await _context.ShopMaterialSkus.SingleAsync(row =>
                row.SkuId == ShopMaterialsCatalogSeed.CounterDisplaySkuId
            );
            Assert.True(counter.AvailableInShop);
            counter.MintOnShopFulfilment = true;
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.UpdateSkuAsync(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                new AdminShopCatalogSkuWriteRequest { MintOnShopFulfilment = false },
                Actor
            );

            Assert.Null(error);
            Assert.NotNull(sku);
            Assert.False(sku!.MintOnShopFulfilment);
            Assert.True(sku.AvailableInShop);
        }

        [Fact]
        public async Task Publish_DefaultsAvailableInShopToFalse()
        {
            await _service.DeleteAsync("table-tents", Actor);
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
                },
                Actor
            );
            Assert.Null(createError);

            await using var stream = new MemoryStream([1, 2, 3, 4]);
            var formFile = new FormFile(stream, 0, stream.Length, "file", "tent.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };
            await _service.UploadImageAsync(created!.SkuId, formFile, Actor);

            var (published, publishError) = await _service.PublishAsync(
                created.SkuId,
                Actor
            );
            Assert.Null(publishError);
            Assert.False(published!.AvailableInShop);
        }

        [Fact]
        public async Task SetAvailable_On_RefusedWithoutSupplier_ReturnsNotReadyAndBlocker()
        {
            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor
            );

            Assert.Equal(ShopSkuReadiness.SkuNotReadyCode, error);
            Assert.NotNull(sku);
            Assert.False(sku!.AvailableInShop);
            Assert.Equal("Add a supplier name.", Assert.Single(sku.AvailabilityBlockers));
            var stored = await _context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(row => row.SkuId == "table-tents");
            Assert.False(stored.AvailableInShop);
            Assert.False(
                await _context.AdminAuditEvents.AnyAsync(row =>
                    row.Action == AdminAuditActions.ShopCatalogAvailableInShop
                )
            );
        }

        [Fact]
        public async Task SetAvailable_On_RefusedWithoutImage()
        {
            await MakeSupplierAsync("table-tents");
            var tent = await _context.ShopMaterialSkus
                .Include(row => row.Images)
                .SingleAsync(row => row.SkuId == "table-tents");
            _context.ShopMaterialSkuImages.RemoveRange(tent.Images);
            tent.Images.Clear();
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor
            );

            Assert.Equal(ShopSkuReadiness.SkuNotReadyCode, error);
            Assert.Contains("Add at least one product image.", sku!.AvailabilityBlockers);
        }

        [Fact]
        public async Task SetAvailable_On_RefusedWithoutPricedPackOrDelivery()
        {
            await MakeSupplierAsync("table-tents");
            var tent = await _context.ShopMaterialSkus
                .SingleAsync(row => row.SkuId == "table-tents");
            tent.PacksJson = ShopMaterialPacks.Serialize(
                [new ShopMaterialPackRow { Qty = 1, NetPence = 0 }]
            );
            tent.DeliveryNetPence = 0;
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor
            );

            Assert.Equal(ShopSkuReadiness.SkuNotReadyCode, error);
            Assert.Contains("Add a pack with a price above £0.00.", sku!.AvailabilityBlockers);
            Assert.Contains("Set a delivery price above £0.00.", sku.AvailabilityBlockers);
        }

        [Fact]
        public async Task SetAvailable_Off_IsAllowed_WhenSkuIsNotReady()
        {
            var counter = await _context.ShopMaterialSkus
                .SingleAsync(row => row.SkuId == ShopMaterialsCatalogSeed.CounterDisplaySkuId);
            counter.AvailableInShop = true;
            counter.SupplierName = null;
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.SetAvailableInShopAsync(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                availableInShop: false,
                Actor
            );

            Assert.Null(error);
            Assert.False(sku!.AvailableInShop);
        }

        [Fact]
        public async Task UpdateSku_OnSkuWithoutSupplier_StaysOn()
        {
            var counter = await _context.ShopMaterialSkus
                .SingleAsync(row => row.SkuId == ShopMaterialsCatalogSeed.CounterDisplaySkuId);
            counter.AvailableInShop = true;
            counter.SupplierName = "Print House";
            await _context.SaveChangesAsync();

            var (sku, error) = await _service.UpdateSkuAsync(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                new AdminShopCatalogSkuWriteRequest { SupplierName = " " },
                Actor
            );

            Assert.Null(error);
            Assert.True(sku!.AvailableInShop);
            Assert.Contains("Add a supplier name.", sku.AvailabilityBlockers);
        }

        [Fact]
        public async Task SetAvailable_On_WhenReady_AuditsPreviousValueAndApprovalReference()
        {
            await MakeSupplierAsync("table-tents");

            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor,
                "  CHG-1042  "
            );

            Assert.Null(error);
            Assert.True(sku!.AvailableInShop);
            Assert.Empty(sku.AvailabilityBlockers);
            var audit = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.ShopCatalogAvailableInShop
            );
            Assert.Equal(Actor.Identity, audit.ActorIdentity);
            Assert.Equal(Actor.AdminUserId, audit.ActorAdminUserId);
            using var detail = JsonDocument.Parse(audit.DetailJson!);
            Assert.True(detail.RootElement.GetProperty("availableInShop").GetBoolean());
            Assert.False(detail.RootElement.GetProperty("previousAvailableInShop").GetBoolean());
            Assert.Equal("CHG-1042", detail.RootElement.GetProperty("approvalReference").GetString());
        }

        [Fact]
        public async Task SetAvailable_ApprovalReferenceOver200Characters_IsRefused()
        {
            await MakeSupplierAsync("table-tents");

            var (sku, error) = await _service.SetAvailableInShopAsync(
                "table-tents",
                availableInShop: true,
                Actor,
                new string('a', 201)
            );

            Assert.Null(sku);
            Assert.Equal("Approval reference must be 200 characters or fewer.", error);
            Assert.False(
                (await _context.ShopMaterialSkus.AsNoTracking()
                    .SingleAsync(row => row.SkuId == "table-tents")).AvailableInShop
            );
        }

        [Fact]
        public async Task Update_AuditsOnlyChangedFields_WithOldAndNewValues()
        {
            var before = await _service.GetSkuAsync("table-tents");

            var (_, error) = await _service.UpdateSkuAsync(
                "table-tents",
                new AdminShopCatalogSkuWriteRequest
                {
                    Title = "Table Tent Plus",
                    DeliveryNetPence = 499,
                },
                Actor
            );

            Assert.Null(error);
            var audit = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.ShopCatalogUpdate
            );
            Assert.Equal("table-tents", audit.TargetId);
            Assert.Equal(Actor.AdminUserId, audit.ActorAdminUserId);
            using var detail = JsonDocument.Parse(audit.DetailJson!);
            var changes = detail.RootElement.GetProperty("changes");
            Assert.Equal(before!.Title, changes.GetProperty("title").GetProperty("old").GetString());
            Assert.Equal("Table Tent Plus", changes.GetProperty("title").GetProperty("new").GetString());
            Assert.Equal(before.DeliveryNetPence, changes.GetProperty("deliveryNetPence").GetProperty("old").GetInt32());
            Assert.Equal(499, changes.GetProperty("deliveryNetPence").GetProperty("new").GetInt32());
            Assert.False(changes.TryGetProperty("description", out _));
        }

        [Fact]
        public async Task Update_WithNoChange_WritesNoUpdateAudit()
        {
            var sku = await _service.GetSkuAsync("table-tents");
            var before = await _context.AdminAuditEvents.CountAsync();

            var (_, error) = await _service.UpdateSkuAsync(
                "table-tents",
                new AdminShopCatalogSkuWriteRequest { Title = sku!.Title },
                Actor
            );

            Assert.Null(error);
            Assert.Equal(before, await _context.AdminAuditEvents.CountAsync());
        }

        [Fact]
        public async Task Unpublish_Publish_Delete_EachWriteStatusAudit()
        {
            await _service.UnpublishAsync("table-tents", Actor);
            await _service.PublishAsync("table-tents", Actor);
            await _service.DeleteAsync("table-tents", Actor);

            var unpublish = await ReadAuditAsync(AdminAuditActions.ShopCatalogUnpublish);
            Assert.Equal("Published", unpublish.GetProperty("changes").GetProperty("status").GetProperty("old").GetString());
            Assert.Equal("Draft", unpublish.GetProperty("changes").GetProperty("status").GetProperty("new").GetString());

            var publish = await ReadAuditAsync(AdminAuditActions.ShopCatalogPublish);
            Assert.Equal("Draft", publish.GetProperty("changes").GetProperty("status").GetProperty("old").GetString());
            Assert.Equal("Published", publish.GetProperty("changes").GetProperty("status").GetProperty("new").GetString());

            var delete = await ReadAuditAsync(AdminAuditActions.ShopCatalogDelete);
            Assert.Equal("Deleted", delete.GetProperty("changes").GetProperty("status").GetProperty("new").GetString());
        }

        [Fact]
        public async Task Create_WritesCreateAuditWithInitialValues()
        {
            await _service.DeleteAsync("table-tents", Actor);
            var tentRecipe = await RecipeIdAsync("table-tent");

            var (created, error) = await _service.CreateSkuAsync(
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
                },
                Actor
            );

            Assert.Null(error);
            var audit = await _context.AdminAuditEvents.SingleAsync(row =>
                row.Action == AdminAuditActions.ShopCatalogCreate
            );
            Assert.Equal(created!.SkuId, audit.TargetId);
            using var detail = JsonDocument.Parse(audit.DetailJson!);
            Assert.Equal(
                "Extra Table Tent",
                detail.RootElement.GetProperty("changes").GetProperty("title").GetProperty("new").GetString()
            );
        }

        [Fact]
        public async Task UploadImage_WritesImageAuditWithOldAndNewImageLists()
        {
            await using var stream = new MemoryStream([1, 2, 3, 4]);
            var formFile = new FormFile(stream, 0, stream.Length, "file", "tent.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };

            var (image, error) = await _service.UploadImageAsync("table-tents", formFile, Actor);

            Assert.Null(error);
            Assert.NotNull(image);
            var audit = await ReadAuditAsync(AdminAuditActions.ShopCatalogImageChange);
            Assert.Equal(1, audit.GetProperty("changes").GetProperty("images").GetProperty("old").GetArrayLength());
            Assert.Equal(2, audit.GetProperty("changes").GetProperty("images").GetProperty("new").GetArrayLength());
        }

        [Fact]
        public async Task Tag_CreateAndDelete_WriteTagAudit()
        {
            var (tag, error) = await _service.CreateTagAsync("Seasonal", Actor);
            Assert.Null(error);

            var (deleted, deleteError) = await _service.DeleteTagAsync(tag!.Id, Actor);
            Assert.True(deleted);
            Assert.Null(deleteError);

            var audits = await _context.AdminAuditEvents
                .Where(row => row.Action == AdminAuditActions.ShopCatalogTagChange)
                .ToListAsync();
            Assert.Equal(2, audits.Count);
            Assert.All(audits, row => Assert.Equal(AdminAuditTargetTypes.ShopMaterialTag, row.TargetType));
            var tagChanges = audits
                .Select(row => JsonDocument.Parse(row.DetailJson!).RootElement.GetProperty("changes").GetProperty("tag"))
                .ToList();
            Assert.Contains(tagChanges, change =>
                change.GetProperty("old").ValueKind == JsonValueKind.Null
                && change.GetProperty("new").GetString() == "Seasonal"
            );
            Assert.Contains(tagChanges, change =>
                change.GetProperty("old").GetString() == "Seasonal"
                && change.GetProperty("new").ValueKind == JsonValueKind.Null
            );
        }

        private async Task<JsonElement> ReadAuditAsync(string action)
        {
            var audit = await _context.AdminAuditEvents.SingleAsync(row => row.Action == action);
            using var document = JsonDocument.Parse(audit.DetailJson!);
            return document.RootElement.Clone();
        }

        private async Task MakeSupplierAsync(string skuId)
        {
            var sku = await _context.ShopMaterialSkus.SingleAsync(row => row.SkuId == skuId);
            sku.SupplierName = "Test Print Supplier";
            await _context.SaveChangesAsync();
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
