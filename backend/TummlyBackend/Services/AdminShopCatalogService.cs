using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Services
{
    public sealed class AdminShopCatalogService : IAdminShopCatalogService
    {
        private static readonly HashSet<string> AllowedQrTypes =
            new(StringComparer.Ordinal)
            {
                "TableTent",
                "WindowSticker",
                "OfferCard",
            };

        private static readonly HashSet<string> AllowedCategories =
            new(StringComparer.Ordinal)
            {
                "tabletop",
                "window",
                "payment",
                "takeaway",
                "delivery",
            };

        private readonly ApplicationDbContext _context;
        private readonly IShopCatalogImageStorage _imageStorage;

        public AdminShopCatalogService(
            ApplicationDbContext context,
            IShopCatalogImageStorage imageStorage
        )
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        public async Task<IReadOnlyList<AdminShopCatalogSkuDto>> ListSkusAsync(
            string? statusFilter = null,
            CancellationToken cancellationToken = default
        )
        {
            var query = _context.ShopMaterialSkus
                .AsNoTracking()
                .Include(row => row.Images)
                .Include(row => row.SkuTags)
                .ThenInclude(row => row.Tag)
                .Where(row => row.Status != ShopMaterialSkuStatuses.Deleted);

            if (
                !string.IsNullOrWhiteSpace(statusFilter)
                && (
                    statusFilter == ShopMaterialSkuStatuses.Draft
                    || statusFilter == ShopMaterialSkuStatuses.Published
                )
            )
            {
                query = query.Where(row => row.Status == statusFilter);
            }

            var skus = await query
                .OrderBy(row => row.Title)
                .ToListAsync(cancellationToken);
            return skus.Select(MapSku).ToList();
        }

        public async Task<AdminShopCatalogSkuDto?> GetSkuAsync(
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: false, cancellationToken);
            return sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted
                ? null
                : MapSku(sku);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> CreateSkuAsync(
            AdminShopCatalogSkuWriteRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var title = request.Title?.Trim() ?? string.Empty;
            if (title.Length == 0)
            {
                return (null, "Title is required.");
            }

            var skuId = string.IsNullOrWhiteSpace(request.SkuId)
                ? Slugify(title)
                : request.SkuId.Trim().ToLowerInvariant();
            if (!IsValidSkuId(skuId))
            {
                return (null, "SkuId must be lowercase letters, numbers, and hyphens.");
            }

            if (await _context.ShopMaterialSkus.AnyAsync(
                    row => row.SkuId == skuId,
                    cancellationToken
                ))
            {
                return (null, "A product with this skuId already exists.");
            }

            var validation = ValidateWriteFields(request, isCreate: true);
            if (validation != null)
            {
                return (null, validation);
            }

            var qrType = request.QrType!.Trim();
            if (await _context.ShopMaterialSkus.AnyAsync(
                    row =>
                        row.QrType == qrType
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                ))
            {
                return (
                    null,
                    $"A product with QR type '{qrType}' already exists. Delete or keep the existing product — only one active product is allowed per QR type."
                );
            }

            var tagError = await ValidateTagIdsAsync(
                request.TagIds,
                cancellationToken
            );
            if (tagError != null)
            {
                return (null, tagError);
            }

            var now = DateTime.UtcNow;
            var sku = new ShopMaterialSku
            {
                SkuId = skuId,
                Title = title,
                Category = request.Category!.Trim(),
                Description = request.Description!.Trim(),
                Material = request.Material!.Trim(),
                Dimensions = request.Dimensions!.Trim(),
                QrType = qrType,
                UnitNetPence = request.UnitNetPence!.Value,
                MinOrderQty = request.MinOrderQty ?? 1,
                Currency = string.IsNullOrWhiteSpace(request.Currency)
                    ? "GBP"
                    : request.Currency.Trim().ToUpperInvariant(),
                MintOnShopFulfilment = request.MintOnShopFulfilment ?? false,
                Status = ShopMaterialSkuStatuses.Draft,
                AboutBody = request.AboutBody?.Trim() ?? string.Empty,
                SpecificationsJson = ShopMaterialSpecifications.Serialize(
                    request.Specifications ?? []
                ),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            await AttachTagsAsync(sku, request.TagIds, cancellationToken);
            _context.ShopMaterialSkus.Add(sku);
            await _context.SaveChangesAsync(cancellationToken);

            var loaded = await LoadSkuAsync(skuId, tracking: false, cancellationToken);
            return (MapSku(loaded!), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UpdateSkuAsync(
            string skuId,
            AdminShopCatalogSkuWriteRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            if (request.QrType != null
                && !string.Equals(
                    request.QrType.Trim(),
                    sku.QrType,
                    StringComparison.Ordinal
                ))
            {
                return (null, "QrType cannot be changed after create.");
            }

            if (request.SkuId != null
                && !string.Equals(
                    request.SkuId.Trim(),
                    sku.SkuId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return (null, "SkuId cannot be changed after create.");
            }

            var validation = ValidateWriteFields(request, isCreate: false);
            if (validation != null)
            {
                return (null, validation);
            }

            if (request.TagIds != null)
            {
                var tagError = await ValidateTagIdsAsync(
                    request.TagIds,
                    cancellationToken
                );
                if (tagError != null)
                {
                    return (null, tagError);
                }
            }

            if (request.Title != null)
            {
                var title = request.Title.Trim();
                if (title.Length == 0)
                {
                    return (null, "Title is required.");
                }

                sku.Title = title;
            }

            if (request.Category != null)
            {
                sku.Category = request.Category.Trim();
            }

            if (request.Description != null)
            {
                sku.Description = request.Description.Trim();
            }

            if (request.Material != null)
            {
                sku.Material = request.Material.Trim();
            }

            if (request.Dimensions != null)
            {
                sku.Dimensions = request.Dimensions.Trim();
            }

            if (request.UnitNetPence != null)
            {
                sku.UnitNetPence = request.UnitNetPence.Value;
            }

            if (request.MinOrderQty != null)
            {
                sku.MinOrderQty = Math.Max(1, request.MinOrderQty.Value);
            }

            if (request.Currency != null)
            {
                sku.Currency = request.Currency.Trim().ToUpperInvariant();
            }

            if (request.MintOnShopFulfilment != null)
            {
                sku.MintOnShopFulfilment = request.MintOnShopFulfilment.Value;
            }

            if (request.AboutBody != null)
            {
                sku.AboutBody = request.AboutBody.Trim();
            }

            if (request.Specifications != null)
            {
                sku.SpecificationsJson = ShopMaterialSpecifications.Serialize(
                    request.Specifications
                );
            }

            if (request.TagIds != null)
            {
                _context.ShopMaterialSkuTags.RemoveRange(sku.SkuTags);
                sku.SkuTags.Clear();
                await AttachTagsAsync(sku, request.TagIds, cancellationToken);
            }

            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var loaded = await LoadSkuAsync(skuId, tracking: false, cancellationToken);
            return (MapSku(loaded!), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> PublishAsync(
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            if (sku.Images.Count == 0)
            {
                return (null, "Publish requires at least one product image.");
            }

            sku.Status = ShopMaterialSkuStatuses.Published;
            sku.PublishedAtUtc ??= DateTime.UtcNow;
            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (MapSku(sku), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UnpublishAsync(
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            sku.Status = ShopMaterialSkuStatuses.Draft;
            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (MapSku(sku), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> DeleteAsync(
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null)
            {
                return (null, "Product was not found.");
            }

            if (sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (MapSku(sku), null);
            }

            sku.Status = ShopMaterialSkuStatuses.Deleted;
            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (MapSku(sku), null);
        }

        public async Task<IReadOnlyList<AdminShopCatalogTagDto>> ListTagsAsync(
            CancellationToken cancellationToken = default
        )
        {
            var tags = await _context.ShopMaterialTags
                .AsNoTracking()
                .OrderBy(row => row.Label)
                .ToListAsync(cancellationToken);
            return tags.Select(MapTag).ToList();
        }

        public async Task<(AdminShopCatalogTagDto? Tag, string? Error)> CreateTagAsync(
            string label,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = label?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return (null, "Tag label is required.");
            }

            if (trimmed.Length > 80)
            {
                return (null, "Tag label must be 80 characters or fewer.");
            }

            var slug = LinkedSlugify(trimmed);
            if (await _context.ShopMaterialTags.AnyAsync(
                    row => row.Slug == slug,
                    cancellationToken
                ))
            {
                return (null, "A tag with this label already exists.");
            }

            var tag = new ShopMaterialTag
            {
                Slug = slug,
                Label = trimmed,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _context.ShopMaterialTags.Add(tag);
            await _context.SaveChangesAsync(cancellationToken);
            return (MapTag(tag), null);
        }

        public async Task<(bool Ok, string? Error)> DeleteTagAsync(
            int tagId,
            CancellationToken cancellationToken = default
        )
        {
            var tag = await _context.ShopMaterialTags
                .Include(row => row.SkuTags)
                .FirstOrDefaultAsync(row => row.Id == tagId, cancellationToken);
            if (tag == null)
            {
                return (false, "Tag was not found.");
            }

            if (tag.SkuTags.Count > 0)
            {
                return (false, "Tag is still attached to one or more products.");
            }

            _context.ShopMaterialTags.Remove(tag);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        public async Task<(AdminShopCatalogImageDto? Image, string? Error)> UploadImageAsync(
            string skuId,
            IFormFile file,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            if (sku.Images.Count >= ShopCatalogImageRules.MaxImagesPerSku)
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

            var sortOrder = sku.Images.Count == 0
                ? 0
                : sku.Images.Max(row => row.SortOrder) + 1;
            var image = new ShopMaterialSkuImage
            {
                SkuId = sku.Id,
                StorageKey = storageKey,
                ContentType = contentType,
                SortOrder = sortOrder,
                CreatedAtUtc = DateTime.UtcNow,
            };
            sku.Images.Add(image);
            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return (
                new AdminShopCatalogImageDto
                {
                    Id = image.Id,
                    Url = ShopCatalogImageRules.BuildPublicUrl(image.StorageKey),
                    SortOrder = image.SortOrder,
                },
                null
            );
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> ReorderImagesAsync(
            string skuId,
            IReadOnlyList<int> imageIds,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            var currentIds = sku.Images.Select(row => row.Id).OrderBy(id => id).ToList();
            var requested = imageIds.Distinct().ToList();
            if (
                requested.Count != currentIds.Count
                || !requested.OrderBy(id => id).SequenceEqual(currentIds)
            )
            {
                return (null, "Image reorder list must include every image exactly once.");
            }

            for (var index = 0; index < requested.Count; index++)
            {
                var image = sku.Images.First(row => row.Id == requested[index]);
                image.SortOrder = index;
            }

            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (MapSku(sku), null);
        }

        public async Task<(bool Ok, string? Error)> DeleteImageAsync(
            string skuId,
            int imageId,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (false, "Product was not found.");
            }

            var image = sku.Images.FirstOrDefault(row => row.Id == imageId);
            if (image == null)
            {
                return (false, "Image was not found.");
            }

            if (
                sku.Status == ShopMaterialSkuStatuses.Published
                && sku.Images.Count <= 1
            )
            {
                return (
                    false,
                    "A published product must keep at least one image."
                );
            }

            if (
                !ShopCatalogImageRules.TryGetLegacyPublicPath(
                    image.StorageKey,
                    out _
                )
                && _imageStorage.IsConfigured
            )
            {
                await _imageStorage.DeleteAsync(
                    image.StorageKey,
                    cancellationToken
                );
            }

            _context.ShopMaterialSkuImages.Remove(image);
            sku.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        private async Task<ShopMaterialSku?> LoadSkuAsync(
            string skuId,
            bool tracking,
            CancellationToken cancellationToken
        )
        {
            IQueryable<ShopMaterialSku> query = _context.ShopMaterialSkus
                .Include(row => row.Images)
                .Include(row => row.SkuTags)
                .ThenInclude(row => row.Tag);

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(
                row => row.SkuId == skuId,
                cancellationToken
            );
        }

        private async Task AttachTagsAsync(
            ShopMaterialSku sku,
            List<int>? tagIds,
            CancellationToken cancellationToken
        )
        {
            if (tagIds == null || tagIds.Count == 0)
            {
                return;
            }

            var ids = tagIds.Distinct().ToList();
            var tags = await _context.ShopMaterialTags
                .Where(row => ids.Contains(row.Id))
                .ToListAsync(cancellationToken);
            foreach (var tag in tags)
            {
                sku.SkuTags.Add(
                    new ShopMaterialSkuTag { Tag = tag, TagId = tag.Id }
                );
            }
        }

        private async Task<string?> ValidateTagIdsAsync(
            List<int>? tagIds,
            CancellationToken cancellationToken
        )
        {
            if (tagIds == null || tagIds.Count == 0)
            {
                return null;
            }

            var ids = tagIds.Distinct().ToList();
            var count = await _context.ShopMaterialTags.CountAsync(
                row => ids.Contains(row.Id),
                cancellationToken
            );
            return count == ids.Count ? null : "One or more tags were not found.";
        }

        private static string? ValidateWriteFields(
            AdminShopCatalogSkuWriteRequest request,
            bool isCreate
        )
        {
            if (isCreate)
            {
                if (string.IsNullOrWhiteSpace(request.Category))
                {
                    return "Category is required.";
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return "Description is required.";
                }

                if (string.IsNullOrWhiteSpace(request.Material))
                {
                    return "Material is required.";
                }

                if (string.IsNullOrWhiteSpace(request.Dimensions))
                {
                    return "Dimensions are required.";
                }

                if (string.IsNullOrWhiteSpace(request.QrType))
                {
                    return "QrType is required.";
                }

                if (request.UnitNetPence is null or < 0)
                {
                    return "UnitNetPence must be zero or greater.";
                }
            }

            if (
                request.Category != null
                && !AllowedCategories.Contains(request.Category.Trim())
            )
            {
                return "Category is not supported.";
            }

            if (
                request.QrType != null
                && !AllowedQrTypes.Contains(request.QrType.Trim())
            )
            {
                return "QrType must be TableTent, WindowSticker, or OfferCard.";
            }

            if (request.UnitNetPence is < 0)
            {
                return "UnitNetPence must be zero or greater.";
            }

            if (request.MinOrderQty is < 1)
            {
                return "MinOrderQty must be at least 1.";
            }

            return null;
        }

        private static AdminShopCatalogSkuDto MapSku(ShopMaterialSku sku) =>
            new()
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                Material = sku.Material,
                Dimensions = sku.Dimensions,
                QrType = sku.QrType,
                UnitNetPence = sku.UnitNetPence,
                MinOrderQty = sku.MinOrderQty,
                Currency = sku.Currency,
                MintOnShopFulfilment = sku.MintOnShopFulfilment,
                Status = sku.Status,
                AboutBody = sku.AboutBody ?? string.Empty,
                Specifications = ShopMaterialSpecifications.Parse(
                    sku.SpecificationsJson
                ),
                Tags = sku.SkuTags
                    .Where(row => row.Tag != null)
                    .OrderBy(row => row.Tag!.Label)
                    .Select(row => MapTag(row.Tag!))
                    .ToList(),
                Images = sku.Images
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.Id)
                    .Select(row => new AdminShopCatalogImageDto
                    {
                        Id = row.Id,
                        Url = ShopCatalogImageRules.BuildPublicUrl(row.StorageKey),
                        SortOrder = row.SortOrder,
                    })
                    .ToList(),
                PublishedAtUtc = sku.PublishedAtUtc,
                UpdatedAtUtc = sku.UpdatedAtUtc,
            };

        private static AdminShopCatalogTagDto MapTag(ShopMaterialTag tag) =>
            new()
            {
                Id = tag.Id,
                Slug = tag.Slug,
                Label = tag.Label,
            };

        private static bool IsValidSkuId(string skuId) =>
            Regex.IsMatch(skuId, "^[a-z0-9]+(?:-[a-z0-9]+)*$");

        private static string Slugify(string title)
        {
            var normalized = title.Trim().ToLowerInvariant();
            var builder = new StringBuilder();
            var pendingHyphen = false;
            foreach (var ch in normalized)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (pendingHyphen && builder.Length > 0)
                    {
                        builder.Append('-');
                    }

                    pendingHyphen = false;
                    builder.Append(ch);
                }
                else
                {
                    pendingHyphen = true;
                }
            }

            var slug = builder.ToString();
            return string.IsNullOrWhiteSpace(slug) ? "product" : slug;
        }

        private static string LinkedSlugify(string label)
        {
            var slug = Slugify(label);
            if (string.Equals(slug, "essential", StringComparison.Ordinal))
            {
                return ShopMaterialCatalogMapper.EssentialSlug;
            }

            if (
                slug.Contains("included", StringComparison.Ordinal)
                && slug.Contains("plan", StringComparison.Ordinal)
            )
            {
                return ShopMaterialCatalogMapper.IncludedWithPlanSlug;
            }

            return slug;
        }
    }
}
