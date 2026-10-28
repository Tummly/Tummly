using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    public sealed class AdminShopCatalogService : IAdminShopCatalogService
    {
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
        private readonly IAdminAuditService _audit;

        public AdminShopCatalogService(
            ApplicationDbContext context,
            IShopCatalogImageStorage imageStorage,
            IAdminAuditService audit
        )
        {
            _context = context;
            _imageStorage = imageStorage;
            _audit = audit;
        }

        public async Task<IReadOnlyList<AdminPlacementRecipeDto>> ListPlacementRecipesAsync(
            CancellationToken cancellationToken = default
        )
        {
            var recipes = await _context.PlacementRecipes
                .AsNoTracking()
                .Where(row => row.IsActive)
                .OrderBy(row => row.Label)
                .ToListAsync(cancellationToken);

            var takenRecipeIds = await _context.ShopMaterialSkus
                .AsNoTracking()
                .Where(row => row.Status != ShopMaterialSkuStatuses.Deleted)
                .Select(row => row.PlacementRecipeId)
                .ToListAsync(cancellationToken);
            var taken = takenRecipeIds.ToHashSet();

            return recipes
                .Select(row => new AdminPlacementRecipeDto
                {
                    Id = row.Id,
                    Slug = row.Slug,
                    Label = row.Label,
                    PlacementKey = row.PlacementKey,
                    PrintRoute = row.PrintRoute,
                    MintOnFulfilmentDefault = row.MintOnFulfilmentDefault,
                    IsAvailable = !taken.Contains(row.Id),
                })
                .ToList();
        }

        public async Task<IReadOnlyList<AdminShopCatalogSkuDto>> ListSkusAsync(
            string? statusFilter = null,
            CancellationToken cancellationToken = default
        )
        {
            var query = _context.ShopMaterialSkus
                .AsNoTracking()
                .Include(row => row.PlacementRecipe)
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
            var linkedBySku = await LoadLinkedInterestKeysAsync(cancellationToken);
            return skus.Select(row => MapSku(row, linkedBySku)).ToList();
        }

        public async Task<AdminShopCatalogSkuDto?> GetSkuAsync(
            string skuId,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: false, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return null;
            }

            var linkedBySku = await LoadLinkedInterestKeysAsync(cancellationToken);
            return MapSku(sku, linkedBySku);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> CreateSkuAsync(
            AdminShopCatalogSkuWriteRequest request,
            AdminShopActor actor,
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
                    row =>
                        row.SkuId == skuId
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
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

            if (request.PlacementRecipeId is null or <= 0)
            {
                return (null, "Placement recipe is required.");
            }

            var recipe = await _context.PlacementRecipes
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == request.PlacementRecipeId.Value
                        && row.IsActive,
                    cancellationToken
                );
            if (recipe == null)
            {
                return (null, "Placement recipe was not found.");
            }

            if (await _context.ShopMaterialSkus.AnyAsync(
                    row =>
                        row.PlacementRecipeId == recipe.Id
                        && row.Status != ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                ))
            {
                return (
                    null,
                    $"A product for placement '{recipe.Label}' already exists. Delete the existing product before adding another — only one active product is allowed per placement recipe."
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
            var category = request.Category!.Trim();
            var description = request.Description!.Trim();
            var material = request.Material!.Trim();
            var dimensions = request.Dimensions!.Trim();
            if (!TryResolvePacksForWrite(request, isCreate: true, out var packs, out var packError))
            {
                return (null, packError);
            }

            var packsJson = ShopMaterialPacks.Serialize(packs);
            var unitNetPence = ShopMaterialPacks.LowestNetPence(packs);
            var minOrderQty = ShopMaterialPacks.LowestQty(packs);
            var deliveryNetPence = Math.Max(0, request.DeliveryNetPence ?? 0);
            var currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "GBP"
                : request.Currency.Trim().ToUpperInvariant();
            var mintOnFulfilment =
                request.MintOnShopFulfilment
                ?? recipe.MintOnFulfilmentDefault;
            var aboutBody = request.AboutBody?.Trim() ?? string.Empty;
            var specificationsJson = ShopMaterialSpecifications.Serialize(
                request.Specifications ?? []
            );
            var supplierName = NormalizeSupplierName(request.SupplierName);
            var internalCost = NormalizeInternalCost(request.InternalCostNetPence);

            // Soft-deleted rows free the filtered unique index — revive that row
            // instead of inserting a second row with the same skuId.
            var softDeleted = await _context.ShopMaterialSkus
                .Include(row => row.SkuTags)
                .Include(row => row.Images)
                .FirstOrDefaultAsync(
                    row =>
                        row.SkuId == skuId
                        && row.Status == ShopMaterialSkuStatuses.Deleted,
                    cancellationToken
                );

            ShopMaterialSku sku;
            if (softDeleted != null)
            {
                sku = softDeleted;
                sku.Title = title;
                sku.Category = category;
                sku.Description = description;
                sku.Material = material;
                sku.Dimensions = dimensions;
                sku.PlacementRecipeId = recipe.Id;
                sku.QrType = recipe.PlacementKey;
                sku.UnitNetPence = unitNetPence;
                sku.MinOrderQty = minOrderQty;
                sku.PacksJson = packsJson;
                sku.DeliveryNetPence = deliveryNetPence;
                sku.SupplierName = supplierName;
                sku.InternalCostNetPence = internalCost;
                sku.Currency = currency;
                sku.MintOnShopFulfilment = mintOnFulfilment;
                sku.Status = ShopMaterialSkuStatuses.Draft;
                sku.PublishedAtUtc = null;
                sku.AboutBody = aboutBody;
                sku.SpecificationsJson = specificationsJson;
                sku.UpdatedAtUtc = now;

                if (sku.SkuTags.Count > 0)
                {
                    _context.ShopMaterialSkuTags.RemoveRange(sku.SkuTags);
                    sku.SkuTags.Clear();
                }

                if (sku.Images.Count > 0)
                {
                    _context.ShopMaterialSkuImages.RemoveRange(sku.Images);
                    sku.Images.Clear();
                }
            }
            else
            {
                sku = new ShopMaterialSku
                {
                    SkuId = skuId,
                    Title = title,
                    Category = category,
                    Description = description,
                    Material = material,
                    Dimensions = dimensions,
                    PlacementRecipeId = recipe.Id,
                    QrType = recipe.PlacementKey,
                    UnitNetPence = unitNetPence,
                    MinOrderQty = minOrderQty,
                    PacksJson = packsJson,
                    DeliveryNetPence = deliveryNetPence,
                    SupplierName = supplierName,
                    InternalCostNetPence = internalCost,
                    Currency = currency,
                    MintOnShopFulfilment = mintOnFulfilment,
                    Status = ShopMaterialSkuStatuses.Draft,
                    AboutBody = aboutBody,
                    SpecificationsJson = specificationsJson,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                _context.ShopMaterialSkus.Add(sku);
            }

            await AttachTagsAsync(sku, request.TagIds, cancellationToken);
            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopCatalogCreate,
                    actor.Identity,
                    AdminAuditTargetTypes.ShopMaterialSku,
                    skuId,
                    ActorAdminUserId: actor.AdminUserId,
                    DetailJson: ShopCatalogChangeLog.Detail(
                        new Dictionary<string, object?>(),
                        CatalogSnapshot(sku)
                    )
                )
            );
            await _context.SaveChangesAsync(cancellationToken);

            var loaded = await LoadSkuAsync(skuId, tracking: false, cancellationToken);
            return (await MapSkuAsync(loaded!, cancellationToken), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UpdateSkuAsync(
            string skuId,
            AdminShopCatalogSkuWriteRequest request,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            var before = CatalogSnapshot(sku);

            if (
                request.PlacementRecipeId != null
                && request.PlacementRecipeId.Value != sku.PlacementRecipeId
            )
            {
                return (null, "Placement recipe cannot be changed after create.");
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

            // Refuse an update that removes the last QR path from an available SKU.
            // Runs before any mutation so a rejected update changes nothing.
            var starter = sku.PlacementRecipe.IncludeInComplimentaryStarter;
            var mintAfter = request.MintOnShopFulfilment ?? sku.MintOnShopFulfilment;
            if (
                sku.AvailableInShop
                && ShopQrPathRules.HasQrPath(sku.MintOnShopFulfilment, starter)
                && !ShopQrPathRules.HasQrPath(mintAfter, starter)
            )
            {
                return (null, ShopQrPathRules.SkuHasNoQrPathCode);
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

            if (
                request.Packs != null
                || request.UnitNetPence != null
                || request.MinOrderQty != null
            )
            {
                if (
                    !TryResolvePacksForWrite(
                        request,
                        isCreate: false,
                        out var packs,
                        out var packError,
                        existingPacksJson: sku.PacksJson
                    )
                )
                {
                    return (null, packError);
                }

                sku.PacksJson = ShopMaterialPacks.Serialize(packs);
                sku.UnitNetPence = ShopMaterialPacks.LowestNetPence(packs);
                sku.MinOrderQty = ShopMaterialPacks.LowestQty(packs);
            }

            if (request.DeliveryNetPence != null)
            {
                if (request.DeliveryNetPence < 0)
                {
                    return (null, "DeliveryNetPence must be zero or greater.");
                }

                sku.DeliveryNetPence = request.DeliveryNetPence.Value;
            }

            if (request.SupplierName != null)
            {
                sku.SupplierName = NormalizeSupplierName(request.SupplierName);
            }

            if (request.InternalCostNetPence != null)
            {
                sku.InternalCostNetPence = NormalizeInternalCost(
                    request.InternalCostNetPence
                );
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

            var after = CatalogSnapshot(sku);
            if (ShopCatalogChangeLog.HasChanges(before, after))
            {
                _audit.Append(
                    new AdminAuditAppendRequest(
                        AdminAuditActions.ShopCatalogUpdate,
                        actor.Identity,
                        AdminAuditTargetTypes.ShopMaterialSku,
                        skuId,
                        ActorAdminUserId: actor.AdminUserId,
                        DetailJson: ShopCatalogChangeLog.Detail(before, after)
                    )
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            var loaded = await LoadSkuAsync(skuId, tracking: false, cancellationToken);
            return (await MapSkuAsync(loaded!, cancellationToken), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> PublishAsync(
            string skuId,
            AdminShopActor actor,
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

            var before = CatalogSnapshot(sku);
            sku.Status = ShopMaterialSkuStatuses.Published;
            sku.AvailableInShop = false;
            sku.PublishedAtUtc ??= DateTime.UtcNow;
            sku.UpdatedAtUtc = DateTime.UtcNow;
            AppendCatalogAudit(
                AdminAuditActions.ShopCatalogPublish,
                actor,
                sku.SkuId,
                before,
                CatalogSnapshot(sku)
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (await MapSkuAsync(sku, cancellationToken), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> SetAvailableInShopAsync(
            string skuId,
            bool availableInShop,
            AdminShopActor actor,
            string? approvalReference = null,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            var reference = approvalReference?.Trim();
            if (reference is { Length: > 200 })
            {
                return (null, "Approval reference must be 200 characters or fewer.");
            }

            reference = string.IsNullOrEmpty(reference) ? null : reference;

            if (sku.Status != ShopMaterialSkuStatuses.Published)
            {
                return (
                    null,
                    "Available in Shop can only change on Published products."
                );
            }

            if (sku.AvailableInShop == availableInShop)
            {
                return (await MapSkuAsync(sku, cancellationToken), null);
            }

            if (
                availableInShop
                && !await ShopQrPathRules.HasQrPathAsync(
                    _context,
                    sku,
                    cancellationToken
                )
            )
            {
                return (null, ShopQrPathRules.SkuHasNoQrPathCode);
            }

            if (availableInShop && ShopSkuReadiness.Blockers(sku).Count > 0)
            {
                return (
                    await MapSkuAsync(sku, cancellationToken),
                    ShopSkuReadiness.SkuNotReadyCode
                );
            }

            var previous = sku.AvailableInShop;
            sku.AvailableInShop = availableInShop;
            sku.UpdatedAtUtc = DateTime.UtcNow;

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopCatalogAvailableInShop,
                    actor.Identity,
                    AdminAuditTargetTypes.ShopMaterialSku,
                    sku.SkuId,
                    ActorAdminUserId: actor.AdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            availableInShop,
                            previousAvailableInShop = previous,
                            approvalReference = reference,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            return (await MapSkuAsync(sku, cancellationToken), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> UnpublishAsync(
            string skuId,
            AdminShopActor actor,
            CancellationToken cancellationToken = default
        )
        {
            var sku = await LoadSkuAsync(skuId, tracking: true, cancellationToken);
            if (sku == null || sku.Status == ShopMaterialSkuStatuses.Deleted)
            {
                return (null, "Product was not found.");
            }

            var before = CatalogSnapshot(sku);
            sku.Status = ShopMaterialSkuStatuses.Draft;
            sku.UpdatedAtUtc = DateTime.UtcNow;
            AppendCatalogAudit(
                AdminAuditActions.ShopCatalogUnpublish,
                actor,
                sku.SkuId,
                before,
                CatalogSnapshot(sku)
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (await MapSkuAsync(sku, cancellationToken), null);
        }

        public async Task<(AdminShopCatalogSkuDto? Sku, string? Error)> DeleteAsync(
            string skuId,
            AdminShopActor actor,
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
                return (await MapSkuAsync(sku, cancellationToken), null);
            }

            var before = CatalogSnapshot(sku);
            sku.Status = ShopMaterialSkuStatuses.Deleted;
            sku.UpdatedAtUtc = DateTime.UtcNow;
            AppendCatalogAudit(
                AdminAuditActions.ShopCatalogDelete,
                actor,
                sku.SkuId,
                before,
                CatalogSnapshot(sku)
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (await MapSkuAsync(sku, cancellationToken), null);
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
            AdminShopActor actor,
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
            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopCatalogTagChange,
                    actor.Identity,
                    AdminAuditTargetTypes.ShopMaterialTag,
                    slug,
                    ActorAdminUserId: actor.AdminUserId,
                    DetailJson: ShopCatalogChangeLog.Detail(
                        new Dictionary<string, object?>(),
                        new Dictionary<string, object?> { ["tag"] = trimmed }
                    )
                )
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (MapTag(tag), null);
        }

        public async Task<(bool Ok, string? Error)> DeleteTagAsync(
            int tagId,
            AdminShopActor actor,
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

            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.ShopCatalogTagChange,
                    actor.Identity,
                    AdminAuditTargetTypes.ShopMaterialTag,
                    tag.Slug,
                    ActorAdminUserId: actor.AdminUserId,
                    DetailJson: ShopCatalogChangeLog.Detail(
                        new Dictionary<string, object?> { ["tag"] = tag.Label },
                        new Dictionary<string, object?>()
                    )
                )
            );
            _context.ShopMaterialTags.Remove(tag);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        public async Task<(AdminShopCatalogImageDto? Image, string? Error)> UploadImageAsync(
            string skuId,
            IFormFile file,
            AdminShopActor actor,
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
            var before = ImageChangeSnapshot(sku.Images);
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
            AppendCatalogAudit(
                AdminAuditActions.ShopCatalogImageChange,
                actor,
                sku.SkuId,
                before,
                ImageChangeSnapshot(sku.Images)
            );
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
            AdminShopActor actor,
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

            var before = ImageChangeSnapshot(sku.Images);
            for (var index = 0; index < requested.Count; index++)
            {
                var image = sku.Images.First(row => row.Id == requested[index]);
                image.SortOrder = index;
            }

            sku.UpdatedAtUtc = DateTime.UtcNow;
            AppendCatalogAudit(
                AdminAuditActions.ShopCatalogImageChange,
                actor,
                sku.SkuId,
                before,
                ImageChangeSnapshot(sku.Images)
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (await MapSkuAsync(sku, cancellationToken), null);
        }

        public async Task<(bool Ok, string? Error)> DeleteImageAsync(
            string skuId,
            int imageId,
            AdminShopActor actor,
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

            var before = ImageChangeSnapshot(sku.Images);
            _context.ShopMaterialSkuImages.Remove(image);
            sku.UpdatedAtUtc = DateTime.UtcNow;
            AppendCatalogAudit(
                AdminAuditActions.ShopCatalogImageChange,
                actor,
                sku.SkuId,
                before,
                ImageChangeSnapshot(sku.Images.Where(row => row.Id != imageId))
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        private void AppendCatalogAudit(
            string action,
            AdminShopActor actor,
            string skuId,
            IReadOnlyDictionary<string, object?> before,
            IReadOnlyDictionary<string, object?> after
        )
        {
            _audit.Append(
                new AdminAuditAppendRequest(
                    action,
                    actor.Identity,
                    AdminAuditTargetTypes.ShopMaterialSku,
                    skuId,
                    ActorAdminUserId: actor.AdminUserId,
                    DetailJson: ShopCatalogChangeLog.Detail(before, after)
                )
            );
        }

        /// <summary>
        /// Values the catalogue history records. Each key is compared between
        /// the state before and after a change.
        /// </summary>
        private static Dictionary<string, object?> CatalogSnapshot(ShopMaterialSku sku) =>
            new(StringComparer.Ordinal)
            {
                ["status"] = sku.Status,
                ["availableInShop"] = sku.AvailableInShop,
                ["title"] = sku.Title,
                ["category"] = sku.Category,
                ["description"] = sku.Description,
                ["material"] = sku.Material,
                ["dimensions"] = sku.Dimensions,
                ["packs"] = ShopMaterialPacks.Parse(sku.PacksJson),
                ["deliveryNetPence"] = sku.DeliveryNetPence,
                ["supplierName"] = sku.SupplierName,
                ["internalCostNetPence"] = sku.InternalCostNetPence,
                ["currency"] = sku.Currency,
                ["mintOnShopFulfilment"] = sku.MintOnShopFulfilment,
                ["aboutBody"] = sku.AboutBody,
                ["specifications"] = ShopMaterialSpecifications.Parse(
                    sku.SpecificationsJson
                ),
                ["tags"] = sku.SkuTags
                    .Where(row => row.Tag != null)
                    .Select(row => row.Tag!.Label)
                    .OrderBy(label => label, StringComparer.Ordinal)
                    .ToList(),
                ["images"] = ImageChangeSnapshot(sku.Images),
            };

        private static Dictionary<string, object?> ImageChangeSnapshot(
            IEnumerable<ShopMaterialSkuImage> images
        ) =>
            new(StringComparer.Ordinal)
            {
                ["images"] = images
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.Id)
                    .Select(row => row.StorageKey)
                    .ToList(),
            };

        private async Task<ShopMaterialSku?> LoadSkuAsync(
            string skuId,
            bool tracking,
            CancellationToken cancellationToken
        )
        {
            IQueryable<ShopMaterialSku> query = _context.ShopMaterialSkus
                .Include(row => row.PlacementRecipe)
                .Include(row => row.Images)
                .Include(row => row.SkuTags)
                .ThenInclude(row => row.Tag);

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            // Prefer a live row if a soft-deleted duplicate somehow exists.
            return await query
                .Where(row => row.SkuId == skuId)
                .OrderBy(row =>
                    row.Status == ShopMaterialSkuStatuses.Deleted ? 1 : 0
                )
                .ThenByDescending(row => row.Id)
                .FirstOrDefaultAsync(cancellationToken);
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

                if (
                    (request.Packs == null || request.Packs.Count == 0)
                    && request.UnitNetPence is null or < 0
                )
                {
                    return "Packs or UnitNetPence are required.";
                }
            }

            if (
                request.Category != null
                && !AllowedCategories.Contains(request.Category.Trim())
            )
            {
                return "Category is not supported.";
            }

            if (request.UnitNetPence is < 0)
            {
                return "UnitNetPence must be zero or greater.";
            }

            if (request.MinOrderQty is < 1)
            {
                return "MinOrderQty must be at least 1.";
            }

            if (request.DeliveryNetPence is < 0)
            {
                return "DeliveryNetPence must be zero or greater.";
            }

            if (request.InternalCostNetPence is < 0)
            {
                return "InternalCostNetPence must be zero or greater.";
            }

            if (request.Packs != null
                && !ShopMaterialPacks.TryValidate(request.Packs, out var packError))
            {
                return packError;
            }

            return null;
        }

        private static bool TryResolvePacksForWrite(
            AdminShopCatalogSkuWriteRequest request,
            bool isCreate,
            out IReadOnlyList<ShopMaterialPackRow> packs,
            out string? error,
            string? existingPacksJson = null
        )
        {
            if (request.Packs != null)
            {
                if (!ShopMaterialPacks.TryValidate(request.Packs, out error))
                {
                    packs = [];
                    return false;
                }

                packs = ShopMaterialPacks.Parse(
                    ShopMaterialPacks.Serialize(request.Packs)
                );
                error = null;
                return true;
            }

            if (request.UnitNetPence is >= 0)
            {
                var qty = request.MinOrderQty is >= 1 ? request.MinOrderQty.Value : 1;
                packs = ShopMaterialPacks.SinglePack(qty, request.UnitNetPence.Value);
                error = null;
                return true;
            }

            if (!isCreate)
            {
                packs = ShopMaterialPacks.Parse(existingPacksJson);
                if (ShopMaterialPacks.TryValidate(packs, out error))
                {
                    return true;
                }
            }

            packs = [];
            error = "Packs or UnitNetPence are required.";
            return false;
        }

        private static string? NormalizeSupplierName(string? value)
        {
            if (value == null)
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static int? NormalizeInternalCost(int? value)
        {
            if (value == null)
            {
                return null;
            }

            return Math.Max(0, value.Value);
        }

        private async Task<Dictionary<string, string>> LoadLinkedInterestKeysAsync(
            CancellationToken cancellationToken
        )
        {
            var rows = await _context.ShopInterestProducts
                .AsNoTracking()
                .Where(row => row.LinkedSkuId != null && row.LinkedSkuId != "")
                .Select(row => new { row.LinkedSkuId, row.ProductKey })
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(
                row => row.LinkedSkuId!,
                row => row.ProductKey,
                StringComparer.Ordinal
            );
        }

        private async Task<AdminShopCatalogSkuDto> MapSkuAsync(
            ShopMaterialSku sku,
            CancellationToken cancellationToken
        )
        {
            var linkedBySku = await LoadLinkedInterestKeysAsync(cancellationToken);
            return MapSku(sku, linkedBySku);
        }

        private static AdminShopCatalogSkuDto MapSku(
            ShopMaterialSku sku,
            IReadOnlyDictionary<string, string>? linkedBySku = null
        ) =>
            new()
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                Material = sku.Material,
                Dimensions = sku.Dimensions,
                PlacementRecipeId = sku.PlacementRecipeId,
                PlacementRecipeLabel = sku.PlacementRecipe?.Label ?? sku.QrType,
                PlacementRecipeSlug = sku.PlacementRecipe?.Slug ?? string.Empty,
                QrType = sku.QrType,
                PrintRoute =
                    sku.PlacementRecipe?.PrintRoute
                    ?? PlacementPrintRoutes.EmbeddedPdf,
                UnitNetPence = sku.UnitNetPence,
                MinOrderQty = sku.MinOrderQty,
                Packs = ShopMaterialPacks.Parse(sku.PacksJson),
                DeliveryNetPence = sku.DeliveryNetPence,
                SupplierName = sku.SupplierName,
                InternalCostNetPence = sku.InternalCostNetPence,
                Currency = sku.Currency,
                MintOnShopFulfilment = sku.MintOnShopFulfilment,
                Status = sku.Status,
                AvailableInShop = sku.AvailableInShop,
                AvailabilityBlockers = ShopSkuReadiness.Blockers(sku),
                LinkedInterestProductKey =
                    linkedBySku != null
                    && linkedBySku.TryGetValue(sku.SkuId, out var interestKey)
                        ? interestKey
                        : null,
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
