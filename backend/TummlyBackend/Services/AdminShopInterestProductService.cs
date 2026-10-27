using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Shop;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Services
{
    public sealed class AdminShopInterestProductService
        : IAdminShopInterestProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IShopCatalogImageStorage _imageStorage;
        private readonly IAdminShopCatalogService _catalog;

        public AdminShopInterestProductService(
            ApplicationDbContext context,
            IShopCatalogImageStorage imageStorage,
            IAdminShopCatalogService catalog
        )
        {
            _context = context;
            _imageStorage = imageStorage;
            _catalog = catalog;
        }

        public async Task<IReadOnlyList<AdminShopInterestProductDto>> ListProductsAsync(
            CancellationToken cancellationToken = default
        )
        {
            var products = await _context.ShopInterestProducts
                .AsNoTracking()
                .Include(row => row.Images)
                .OrderBy(row => row.Title)
                .ToListAsync(cancellationToken);

            var counts = await _context.ShopInterestRegistrations
                .AsNoTracking()
                .GroupBy(row => row.ShopInterestProductId)
                .Select(group => new { ProductId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(
                    row => row.ProductId,
                    row => row.Count,
                    cancellationToken
                );

            return products
                .Select(row => MapProduct(row, counts.GetValueOrDefault(row.Id)))
                .ToList();
        }

        public async Task<(AdminShopInterestProductDto? Product, string? Error)> UpdateProductAsync(
            string productKey,
            AdminShopInterestProductWriteRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var product = await LoadProductAsync(
                productKey,
                tracking: true,
                cancellationToken
            );
            if (product == null)
            {
                return (null, "Interest product was not found.");
            }

            if (request.Title != null)
            {
                var title = request.Title.Trim();
                if (title.Length == 0)
                {
                    return (null, "Title is required.");
                }

                product.Title = title;
            }

            if (request.Description != null)
            {
                var description = request.Description.Trim();
                if (description.Length == 0)
                {
                    return (null, "Description is required.");
                }

                product.Description = description;
            }

            if (request.Material != null)
            {
                product.Material = request.Material.Trim();
            }

            if (request.Dimensions != null)
            {
                product.Dimensions = request.Dimensions.Trim();
            }

            product.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var registrationCount = await _context.ShopInterestRegistrations
                .CountAsync(
                    row => row.ShopInterestProductId == product.Id,
                    cancellationToken
                );
            return (MapProduct(product, registrationCount), null);
        }

        public async Task<(AdminShopInterestProductImageDto? Image, string? Error)> UploadImageAsync(
            string productKey,
            IFormFile file,
            CancellationToken cancellationToken = default
        )
        {
            var product = await LoadProductAsync(
                productKey,
                tracking: true,
                cancellationToken
            );
            if (product == null)
            {
                return (null, "Interest product was not found.");
            }

            if (product.Images.Count >= ShopCatalogImageRules.MaxImagesPerSku)
            {
                return (
                    null,
                    $"A product may have at most {ShopCatalogImageRules.MaxImagesPerSku} images."
                );
            }

            var validation = ShopCatalogImageRules.ValidateFile(file);
            if (validation != null)
            {
                return (null, validation);
            }

            if (!_imageStorage.IsConfigured)
            {
                return (null, "Shop catalog image storage is not configured.");
            }

            var contentType = ShopCatalogImageRules.ResolveContentType(
                file.ContentType,
                file.FileName
            )!;
            var storageKey = ShopCatalogImageRules.BuildStorageKey(file.FileName);
            await using var stream = file.OpenReadStream();
            await _imageStorage.UploadAsync(
                storageKey,
                stream,
                contentType,
                file.Length,
                cancellationToken
            );

            var sortOrder =
                product.Images.Count == 0
                    ? 0
                    : product.Images.Max(row => row.SortOrder) + 1;
            var image = new ShopInterestProductImage
            {
                ShopInterestProductId = product.Id,
                StorageKey = storageKey,
                ContentType = contentType,
                SortOrder = sortOrder,
                CreatedAtUtc = DateTime.UtcNow,
            };
            product.Images.Add(image);
            product.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return (
                new AdminShopInterestProductImageDto
                {
                    Id = image.Id,
                    Url = ShopCatalogImageRules.BuildPublicUrl(image.StorageKey),
                    SortOrder = image.SortOrder,
                },
                null
            );
        }

        public async Task<(bool Ok, string? Error)> DeleteImageAsync(
            string productKey,
            int imageId,
            CancellationToken cancellationToken = default
        )
        {
            var product = await LoadProductAsync(
                productKey,
                tracking: true,
                cancellationToken
            );
            if (product == null)
            {
                return (false, "Interest product was not found.");
            }

            var image = product.Images.FirstOrDefault(row => row.Id == imageId);
            if (image == null)
            {
                return (false, "Image was not found.");
            }

            if (
                _imageStorage.IsConfigured
                && !ShopCatalogImageRules.TryGetLegacyPublicPath(
                    image.StorageKey,
                    out _
                )
            )
            {
                try
                {
                    await _imageStorage.DeleteAsync(
                        image.StorageKey,
                        cancellationToken
                    );
                }
                catch (FileNotFoundException)
                {
                    // Blob already gone — still remove the DB row.
                }
            }

            _context.ShopInterestProductImages.Remove(image);
            product.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        public async Task<(AdminShopInterestProductDto? Product, string? Error)> PromoteAsync(
            string productKey,
            AdminShopInterestPromoteRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var product = await LoadProductAsync(
                productKey,
                tracking: true,
                cancellationToken
            );
            if (product == null)
            {
                return (null, "Interest product was not found.");
            }

            if (!string.IsNullOrWhiteSpace(product.LinkedSkuId) && !product.IsActive)
            {
                return (null, "Interest product is already promoted to Shop.");
            }

            if (!product.IsActive)
            {
                return (null, "Interest product is not active.");
            }

            if (product.Images.Count == 0)
            {
                return (null, "Promote requires at least one product image.");
            }

            if (string.IsNullOrWhiteSpace(product.Dimensions))
            {
                return (null, "Dimensions are required before promote.");
            }

            if (string.IsNullOrWhiteSpace(product.Title))
            {
                return (null, "Title is required before promote.");
            }

            if (string.IsNullOrWhiteSpace(product.Description))
            {
                return (null, "Description is required before promote.");
            }

            var material = string.IsNullOrWhiteSpace(product.Material)
                ? "TBC"
                : product.Material.Trim();
            var category = string.IsNullOrWhiteSpace(request.Category)
                ? "tabletop"
                : request.Category.Trim();

            var existingSku = await _context.ShopMaterialSkus
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.SkuId == product.ProductKey
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                );

            AdminShopCatalogSkuDto? skuDto;
            string? error;
            if (existingSku != null)
            {
                var updateWrite = new AdminShopCatalogSkuWriteRequest
                {
                    Title = product.Title,
                    Category = category,
                    Description = product.Description,
                    Material = material,
                    Dimensions = product.Dimensions.Trim(),
                    Packs = request.Packs,
                    DeliveryNetPence = request.DeliveryNetPence ?? 0,
                    SupplierName = request.SupplierName,
                    InternalCostNetPence = request.InternalCostNetPence,
                    MintOnShopFulfilment = request.MintOnShopFulfilment,
                    AboutBody = request.AboutBody ?? string.Empty,
                    Specifications = request.Specifications,
                    TagIds = request.TagIds,
                };
                (skuDto, error) = await _catalog.UpdateSkuAsync(
                    product.ProductKey,
                    updateWrite,
                    cancellationToken
                );
            }
            else
            {
                var createWrite = new AdminShopCatalogSkuWriteRequest
                {
                    SkuId = product.ProductKey,
                    Title = product.Title,
                    Category = category,
                    Description = product.Description,
                    Material = material,
                    Dimensions = product.Dimensions.Trim(),
                    PlacementRecipeId = request.PlacementRecipeId,
                    Packs = request.Packs,
                    DeliveryNetPence = request.DeliveryNetPence ?? 0,
                    SupplierName = request.SupplierName,
                    InternalCostNetPence = request.InternalCostNetPence,
                    MintOnShopFulfilment = request.MintOnShopFulfilment,
                    AboutBody = request.AboutBody ?? string.Empty,
                    Specifications = request.Specifications,
                    TagIds = request.TagIds,
                };
                (skuDto, error) = await _catalog.CreateSkuAsync(
                    createWrite,
                    cancellationToken
                );
            }

            if (error != null || skuDto == null)
            {
                return (null, error ?? "Could not create shop product.");
            }

            var syncError = await SyncSkuImagesFromInterestAsync(
                product,
                cancellationToken
            );
            if (syncError != null)
            {
                return (null, syncError);
            }

            (skuDto, error) = await _catalog.PublishAsync(
                product.ProductKey,
                cancellationToken
            );
            if (error != null || skuDto == null)
            {
                return (null, error ?? "Could not publish shop product.");
            }

            var sku = await _context.ShopMaterialSkus
                .FirstOrDefaultAsync(
                    row =>
                        row.SkuId == product.ProductKey
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                );
            if (sku == null)
            {
                return (null, "Shop product was not found after publish.");
            }

            if (!await ShopQrPathRules.HasQrPathAsync(_context, sku, cancellationToken))
            {
                return (null, ShopQrPathRules.SkuHasNoQrPathCode);
            }

            sku.AvailableInShop = true;
            sku.UpdatedAtUtc = DateTime.UtcNow;

            product.IsActive = false;
            product.LinkedSkuId = product.ProductKey;
            product.Material = material;
            product.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var registrationCount = await _context.ShopInterestRegistrations
                .CountAsync(
                    row => row.ShopInterestProductId == product.Id,
                    cancellationToken
                );
            return (MapProduct(product, registrationCount), null);
        }

        public async Task<(AdminShopInterestProductDto? Product, string? Error)> DemoteByProductKeyAsync(
            string productKey,
            CancellationToken cancellationToken = default
        )
        {
            var product = await LoadProductAsync(
                productKey,
                tracking: true,
                cancellationToken
            );
            if (product == null)
            {
                return (null, "Interest product was not found.");
            }

            return await DemoteProductAsync(product, cancellationToken);
        }

        public async Task<(AdminShopInterestProductDto? Product, string? Error)> DemoteBySkuIdAsync(
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var key = (skuId ?? string.Empty).Trim();
            if (key.Length == 0)
            {
                return (null, "skuId is required.");
            }

            var product = await _context.ShopInterestProducts
                .Include(row => row.Images)
                .FirstOrDefaultAsync(
                    row => row.LinkedSkuId == key,
                    cancellationToken
                );
            if (product == null)
            {
                return (
                    null,
                    "No Register Interest product is linked to this shop product."
                );
            }

            return await DemoteProductAsync(product, cancellationToken);
        }

        private async Task<(AdminShopInterestProductDto? Product, string? Error)> DemoteProductAsync(
            ShopInterestProduct product,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(product.LinkedSkuId))
            {
                return (null, "Interest product is not linked to a shop product.");
            }

            var skuId = product.LinkedSkuId;
            var (skuDto, unpublishError) = await _catalog.UnpublishAsync(
                skuId,
                cancellationToken
            );
            if (unpublishError != null)
            {
                return (null, unpublishError);
            }

            var sku = await _context.ShopMaterialSkus
                .Include(row => row.Images)
                .FirstOrDefaultAsync(
                    row =>
                        row.SkuId == skuId
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                );
            if (sku == null)
            {
                return (null, "Shop product was not found.");
            }

            sku.AvailableInShop = false;
            sku.UpdatedAtUtc = DateTime.UtcNow;

            product.Title = sku.Title;
            product.Description = sku.Description;
            product.Material = sku.Material;
            product.Dimensions = sku.Dimensions;
            product.IsActive = true;
            product.LinkedSkuId = null;
            product.UpdatedAtUtc = DateTime.UtcNow;

            if (product.Images.Count > 0)
            {
                _context.ShopInterestProductImages.RemoveRange(product.Images);
                product.Images.Clear();
            }

            foreach (
                var image in sku.Images
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.Id)
            )
            {
                product.Images.Add(
                    new ShopInterestProductImage
                    {
                        StorageKey = image.StorageKey,
                        ContentType = image.ContentType,
                        SortOrder = image.SortOrder,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            var registrationCount = await _context.ShopInterestRegistrations
                .CountAsync(
                    row => row.ShopInterestProductId == product.Id,
                    cancellationToken
                );
            _ = skuDto;
            return (MapProduct(product, registrationCount), null);
        }

        private async Task<string?> SyncSkuImagesFromInterestAsync(
            ShopInterestProduct product,
            CancellationToken cancellationToken
        )
        {
            var sku = await _context.ShopMaterialSkus
                .Include(row => row.Images)
                .FirstOrDefaultAsync(
                    row =>
                        row.SkuId == product.ProductKey
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                );
            if (sku == null)
            {
                return "Shop product was not found while copying images.";
            }

            if (sku.Images.Count > 0)
            {
                _context.ShopMaterialSkuImages.RemoveRange(sku.Images);
                sku.Images.Clear();
            }

            foreach (
                var image in product.Images
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.Id)
            )
            {
                sku.Images.Add(
                    new ShopMaterialSkuImage
                    {
                        StorageKey = image.StorageKey,
                        ContentType = image.ContentType,
                        SortOrder = image.SortOrder,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
            }

            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }

        private async Task<ShopInterestProduct?> LoadProductAsync(
            string productKey,
            bool tracking,
            CancellationToken cancellationToken
        )
        {
            var key = (productKey ?? string.Empty).Trim();
            if (key.Length == 0)
            {
                return null;
            }

            IQueryable<ShopInterestProduct> query = _context.ShopInterestProducts
                .Include(row => row.Images);

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(
                row => row.ProductKey == key,
                cancellationToken
            );
        }

        private static AdminShopInterestProductDto MapProduct(
            ShopInterestProduct product,
            int registrationCount
        ) =>
            new()
            {
                ProductKey = product.ProductKey,
                Title = product.Title,
                Description = product.Description,
                Material = product.Material ?? string.Empty,
                Dimensions = product.Dimensions ?? string.Empty,
                IsActive = product.IsActive,
                LinkedSkuId = product.LinkedSkuId,
                RegistrationCount = registrationCount,
                Images = product.Images
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.Id)
                    .Select(row => new AdminShopInterestProductImageDto
                    {
                        Id = row.Id,
                        Url = ShopCatalogImageRules.BuildPublicUrl(row.StorageKey),
                        SortOrder = row.SortOrder,
                    })
                    .ToList(),
                UpdatedAtUtc = product.UpdatedAtUtc,
            };
    }
}
