using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    public static class ShopMaterialsCatalogSeed
    {
        public static readonly (string Slug, string Label)[] DefaultTags =
        [
            (
                ShopMaterialCatalogMapper.IncludedWithPlanSlug,
                "Included with your plan"
            ),
            (ShopMaterialCatalogMapper.EssentialSlug, "Essential"),
        ];

        public static readonly (
            string Slug,
            string Label,
            string PlacementKey,
            string PrintRoute,
            bool MintDefault,
            bool IncludeInComplimentaryStarter
        )[] DefaultRecipes =
        [
            (
                "table-tent",
                "Table Tent",
                "TableTent",
                PlacementPrintRoutes.EmbeddedPdf,
                false,
                true
            ),
            (
                "window-sticker",
                "Window Sticker",
                "WindowSticker",
                PlacementPrintRoutes.EmbeddedPdf,
                false,
                true
            ),
            (
                "offer-card",
                "Offer Card",
                "OfferCard",
                PlacementPrintRoutes.EmbeddedPdf,
                false,
                true
            ),
        ];

        public static async Task EnsureSeededAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default
        )
        {
            await EnsureRecipesAsync(context, cancellationToken);
            await EnsureTagsAsync(context, cancellationToken);
            await BackfillSkuRecipesAsync(context, cancellationToken);
            if (await context.ShopMaterialSkus.AnyAsync(cancellationToken))
            {
                return;
            }

            var tagBySlug = await context.ShopMaterialTags
                .ToDictionaryAsync(
                    row => row.Slug,
                    StringComparer.Ordinal,
                    cancellationToken
                );
            var recipeBySlug = await context.PlacementRecipes
                .ToDictionaryAsync(
                    row => row.Slug,
                    StringComparer.Ordinal,
                    cancellationToken
                );

            var now = DateTime.UtcNow;
            foreach (var seed in SeedSkus())
            {
                if (!recipeBySlug.TryGetValue(seed.RecipeSlug, out var recipe))
                {
                    continue;
                }

                var sku = new ShopMaterialSku
                {
                    SkuId = seed.SkuId,
                    Title = seed.Title,
                    Category = seed.Category,
                    Description = seed.Description,
                    Material = seed.Material,
                    Dimensions = seed.Dimensions,
                    PlacementRecipeId = recipe.Id,
                    QrType = recipe.PlacementKey,
                    UnitNetPence = seed.UnitNetPence,
                    MinOrderQty = seed.MinOrderQty,
                    Currency = seed.Currency,
                    MintOnShopFulfilment = seed.MintOnShopFulfilment
                        ?? recipe.MintOnFulfilmentDefault,
                    Status = ShopMaterialSkuStatuses.Published,
                    AboutBody = seed.AboutBody,
                    SpecificationsJson = ShopMaterialSpecifications.Serialize(
                        seed.Specifications
                    ),
                    PublishedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                foreach (var slug in seed.TagSlugs)
                {
                    if (!tagBySlug.TryGetValue(slug, out var tag))
                    {
                        continue;
                    }

                    sku.SkuTags.Add(
                        new ShopMaterialSkuTag { Tag = tag, TagId = tag.Id }
                    );
                }

                sku.Images.Add(
                    new ShopMaterialSkuImage
                    {
                        StorageKey =
                            ShopCatalogImageRules.BuildLegacyStorageKey(
                                seed.LegacyImagePath
                            ),
                        ContentType = "image/png",
                        SortOrder = 0,
                        CreatedAtUtc = now,
                    }
                );

                context.ShopMaterialSkus.Add(sku);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureRecipesAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken
        )
        {
            var existing = await context.PlacementRecipes.ToListAsync(
                cancellationToken
            );
            var bySlug = existing.ToDictionary(
                row => row.Slug,
                StringComparer.Ordinal
            );
            var now = DateTime.UtcNow;
            var changed = false;
            foreach (var recipe in DefaultRecipes)
            {
                if (bySlug.TryGetValue(recipe.Slug, out var row))
                {
                    if (
                        row.IncludeInComplimentaryStarter
                        != recipe.IncludeInComplimentaryStarter
                    )
                    {
                        row.IncludeInComplimentaryStarter =
                            recipe.IncludeInComplimentaryStarter;
                        changed = true;
                    }

                    continue;
                }

                context.PlacementRecipes.Add(
                    new PlacementRecipe
                    {
                        Slug = recipe.Slug,
                        Label = recipe.Label,
                        PlacementKey = recipe.PlacementKey,
                        PrintRoute = recipe.PrintRoute,
                        MintOnFulfilmentDefault = recipe.MintDefault,
                        IncludeInComplimentaryStarter =
                            recipe.IncludeInComplimentaryStarter,
                        IsActive = true,
                        CreatedAtUtc = now,
                    }
                );
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Links pre-recipe SKUs to recipes by denormalized QrType / PlacementKey.
        /// </summary>
        private static async Task BackfillSkuRecipesAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken
        )
        {
            var recipes = await context.PlacementRecipes
                .AsNoTracking()
                .ToDictionaryAsync(
                    row => row.PlacementKey,
                    StringComparer.Ordinal,
                    cancellationToken
                );
            if (recipes.Count == 0)
            {
                return;
            }

            var orphanSkus = await context.ShopMaterialSkus
                .Where(row => row.PlacementRecipeId == 0)
                .ToListAsync(cancellationToken);
            if (orphanSkus.Count == 0)
            {
                return;
            }

            foreach (var sku in orphanSkus)
            {
                if (recipes.TryGetValue(sku.QrType, out var recipe))
                {
                    sku.PlacementRecipeId = recipe.Id;
                    sku.QrType = recipe.PlacementKey;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureTagsAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken
        )
        {
            var existing = await context.ShopMaterialTags
                .Select(row => row.Slug)
                .ToListAsync(cancellationToken);
            var existingSet = existing.ToHashSet(StringComparer.Ordinal);
            var now = DateTime.UtcNow;
            foreach (var (slug, label) in DefaultTags)
            {
                if (existingSet.Contains(slug))
                {
                    continue;
                }

                context.ShopMaterialTags.Add(
                    new ShopMaterialTag
                    {
                        Slug = slug,
                        Label = label,
                        CreatedAtUtc = now,
                    }
                );
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static IEnumerable<SeedSku> SeedSkus()
        {
            var defaultSpecs = new[]
            {
                new ShopMaterialSpecificationRow
                {
                    Label = "Finished size",
                    Value = "A5",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "Construction",
                    Value = "Folded A-frame",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "Print",
                    Value = "Double-sided",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "Finish",
                    Value = "Matte",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "QR connection",
                    Value = "Location and placement-specific",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "Recommended use",
                    Value = "Dine-in tables",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "Minimum quantity",
                    Value = "Pack of 5",
                },
                new ShopMaterialSpecificationRow
                {
                    Label = "Production",
                    Value = "Made to order",
                },
            };

            yield return new SeedSku(
                "table-tents",
                "Table Tent QR",
                "tabletop",
                "Place a branded QR prompt on guest tables to collect private feedback after a dine-in visit.",
                "3mm Matte Frosted Acrylic",
                "105mm x 148mm (A6)",
                "table-tent",
                2400,
                1,
                "GBP",
                false,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly table tent qr gives dine-in guests a clear opportunity to share private feedback while their experience is still recent. Each order uses a location-specific QR placement, allowing Tummly to measure scans, form starts and completed feedback submissions from table-based prompts.",
                [
                    .. defaultSpecs,
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Material",
                        Value = "3mm Matte Frosted Acrylic",
                    },
                ],
                [
                    ShopMaterialCatalogMapper.IncludedWithPlanSlug,
                    ShopMaterialCatalogMapper.EssentialSlug,
                ]
            );

            yield return new SeedSku(
                "window-stickers",
                "Window Sticker QR",
                "window",
                "Invite guests to share feedback using a branded QR prompt at an entrance or front windows.",
                "UV-resistant Clear Vinyl Cling",
                "150mm x 150mm",
                "window-sticker",
                1400,
                1,
                "GBP",
                false,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly window sticker qr invites guests to share private feedback at an entrance or front window. Each order uses a location-specific QR placement so Tummly can measure scans, form starts and completed submissions.",
                [
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finished size",
                        Value = "150mm x 150mm",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Material",
                        Value = "UV-resistant Clear Vinyl Cling",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Print",
                        Value = "Single-sided",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finish",
                        Value = "Clear cling",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "QR connection",
                        Value = "Location and placement-specific",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Entrance and front windows",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Minimum quantity",
                        Value = "Pack of 5",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Production",
                        Value = "Made to order",
                    },
                ],
                [
                    ShopMaterialCatalogMapper.IncludedWithPlanSlug,
                    ShopMaterialCatalogMapper.EssentialSlug,
                ]
            );

            yield return new SeedSku(
                "offer-card",
                "Offer Card",
                "payment",
                "Place a compact QR prompt on ordering, payment or collection counter for offers and feedback.",
                "350gsm Soft-touch Recycled Card",
                "85mm x 55mm (Card size)",
                "offer-card",
                1800,
                1,
                "GBP",
                false,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly offer card places a compact QR prompt on ordering, payment or collection counters for offers and feedback. Each card uses a location-specific QR placement.",
                [
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finished size",
                        Value = "85mm x 55mm",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Material",
                        Value = "350gsm Soft-touch Recycled Card",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Print",
                        Value = "Double-sided",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finish",
                        Value = "Soft-touch",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "QR connection",
                        Value = "Location and placement-specific",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Counters and payment points",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Minimum quantity",
                        Value = "Pack of 5",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Production",
                        Value = "Made to order",
                    },
                ],
                [
                    ShopMaterialCatalogMapper.IncludedWithPlanSlug,
                    ShopMaterialCatalogMapper.EssentialSlug,
                ]
            );
        }

        private sealed record SeedSku(
            string SkuId,
            string Title,
            string Category,
            string Description,
            string Material,
            string Dimensions,
            string RecipeSlug,
            int UnitNetPence,
            int MinOrderQty,
            string Currency,
            bool? MintOnShopFulfilment,
            string LegacyImagePath,
            string AboutBody,
            ShopMaterialSpecificationRow[] Specifications,
            string[] TagSlugs
        );
    }
}
