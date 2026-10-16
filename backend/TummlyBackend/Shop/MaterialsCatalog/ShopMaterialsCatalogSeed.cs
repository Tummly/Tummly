using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    public static class ShopMaterialsCatalogSeed
    {
        public const string CounterDisplaySkuId = "qr-counter-display";
        public const string LegacyOfferCardSkuId = "offer-card";
        public const string CounterDisplayRecipeSlug = "counter-display";
        public const string LegacyOfferCardRecipeSlug = "offer-card";

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
                CounterDisplayRecipeSlug,
                "QR Counter Display",
                "OfferCard",
                PlacementPrintRoutes.EmbeddedPdf,
                false,
                true
            ),
            (
                "table-tent",
                "Table Tent",
                "TableTent",
                PlacementPrintRoutes.PatchCsv60,
                false,
                false
            ),
            (
                "table-talker",
                "Table Talker",
                "TableTalker",
                PlacementPrintRoutes.PatchCsv60,
                false,
                false
            ),
            (
                "window-sticker",
                "Window Sticker",
                "WindowSticker",
                PlacementPrintRoutes.PatchCsv60,
                false,
                false
            ),
            (
                "qr-guest-cards",
                "QR Guest Cards",
                "GuestCard",
                PlacementPrintRoutes.GuestCardCsv,
                false,
                false
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
            await EnsureCoreSkusAsync(context, cancellationToken);
            await BackfillPacksAndDeliveryAsync(context, cancellationToken);
            await ShopInterestCatalogSeed.EnsureSeededAsync(
                context,
                cancellationToken
            );
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
            var byPlacement = existing.ToDictionary(
                row => row.PlacementKey,
                StringComparer.Ordinal
            );
            var now = DateTime.UtcNow;
            var changed = false;

            // Remap legacy Offer Card recipe onto Counter Display (same OfferCard placement).
            if (
                bySlug.TryGetValue(LegacyOfferCardRecipeSlug, out var legacyOffer)
                && !bySlug.ContainsKey(CounterDisplayRecipeSlug)
            )
            {
                legacyOffer.Slug = CounterDisplayRecipeSlug;
                legacyOffer.Label = "QR Counter Display";
                legacyOffer.PrintRoute = PlacementPrintRoutes.EmbeddedPdf;
                legacyOffer.IncludeInComplimentaryStarter = true;
                bySlug.Remove(LegacyOfferCardRecipeSlug);
                bySlug[CounterDisplayRecipeSlug] = legacyOffer;
                changed = true;
            }

            foreach (var recipe in DefaultRecipes)
            {
                PlacementRecipe? row = null;
                if (bySlug.TryGetValue(recipe.Slug, out var bySlugRow))
                {
                    row = bySlugRow;
                }
                else if (
                    byPlacement.TryGetValue(recipe.PlacementKey, out var byKeyRow)
                )
                {
                    row = byKeyRow;
                }

                if (row != null)
                {
                    if (
                        row.Slug != recipe.Slug
                        || row.Label != recipe.Label
                        || row.PrintRoute != recipe.PrintRoute
                        || row.IncludeInComplimentaryStarter
                            != recipe.IncludeInComplimentaryStarter
                        || row.MintOnFulfilmentDefault != recipe.MintDefault
                        || !row.IsActive
                    )
                    {
                        row.Slug = recipe.Slug;
                        row.Label = recipe.Label;
                        row.PrintRoute = recipe.PrintRoute;
                        row.IncludeInComplimentaryStarter =
                            recipe.IncludeInComplimentaryStarter;
                        row.MintOnFulfilmentDefault = recipe.MintDefault;
                        row.IsActive = true;
                        changed = true;
                    }

                    if (
                        !string.Equals(
                            row.PrintRoute,
                            recipe.PrintRoute,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        row.PrintRoute = recipe.PrintRoute;
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
        /// Inserts missing launch SKUs and remaps Offer Card → QR Counter Display.
        /// Full commercial / availability seed runs on insert and on the one-time
        /// upgrade to five products. Later startups only re-bind recipes so Admin
        /// packs and AvailableInShop stay authoritative.
        /// </summary>
        private static async Task EnsureCoreSkusAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken
        )
        {
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
            var skus = await context.ShopMaterialSkus
                .Include(row => row.SkuTags)
                .Include(row => row.Images)
                .ToListAsync(cancellationToken);
            var bySkuId = skus.ToDictionary(
                row => row.SkuId,
                StringComparer.Ordinal
            );
            var now = DateTime.UtcNow;
            var changed = false;
            var remappedOfferCard = false;
            var hadTalker = bySkuId.ContainsKey("table-talker");
            var hadGuestCards = bySkuId.ContainsKey("qr-guest-cards");

            if (
                bySkuId.TryGetValue(LegacyOfferCardSkuId, out var legacyOfferSku)
                && !bySkuId.ContainsKey(CounterDisplaySkuId)
            )
            {
                legacyOfferSku.SkuId = CounterDisplaySkuId;
                bySkuId.Remove(LegacyOfferCardSkuId);
                bySkuId[CounterDisplaySkuId] = legacyOfferSku;
                remappedOfferCard = true;
                changed = true;
            }
            else if (
                bySkuId.TryGetValue(LegacyOfferCardSkuId, out var duplicateOffer)
                && bySkuId.ContainsKey(CounterDisplaySkuId)
            )
            {
                duplicateOffer.Status = ShopMaterialSkuStatuses.Deleted;
                duplicateOffer.UpdatedAtUtc = now;
                changed = true;
            }

            foreach (var seed in SeedSkus())
            {
                if (!recipeBySlug.TryGetValue(seed.RecipeSlug, out var recipe))
                {
                    continue;
                }

                var commercial = ResolveCommercial(seed.SkuId, seed.UnitNetPence);
                if (bySkuId.TryGetValue(seed.SkuId, out var existing))
                {
                    if (SyncRecipeBinding(existing, recipe, now))
                    {
                        changed = true;
                    }

                    continue;
                }

                var sku = new ShopMaterialSku
                {
                    SkuId = seed.SkuId,
                    CreatedAtUtc = now,
                };
                ApplySeedToSku(sku, seed, recipe, commercial, now, tagBySlug);
                sku.Images.Add(
                    new ShopMaterialSkuImage
                    {
                        StorageKey = ShopCatalogImageRules.BuildLegacyStorageKey(
                            seed.LegacyImagePath
                        ),
                        ContentType = "image/png",
                        SortOrder = 0,
                        CreatedAtUtc = now,
                    }
                );
                context.ShopMaterialSkus.Add(sku);
                bySkuId[seed.SkuId] = sku;
                changed = true;
            }

            // One-time align when leaving the legacy three-SKU kit.
            // Inserts already received ApplySeedToSku above; do not re-apply on
            // steady-state startups or when only a single missing SKU is restored.
            var upgradeToFive =
                remappedOfferCard || (!hadTalker && !hadGuestCards);
            if (upgradeToFive)
            {
                foreach (var seed in SeedSkus())
                {
                    if (
                        !recipeBySlug.TryGetValue(seed.RecipeSlug, out var recipe)
                        || !bySkuId.TryGetValue(seed.SkuId, out var sku)
                    )
                    {
                        continue;
                    }

                    var commercial = ResolveCommercial(
                        seed.SkuId,
                        seed.UnitNetPence
                    );
                    if (
                        ApplySeedToSku(
                            sku,
                            seed,
                            recipe,
                            commercial,
                            now,
                            tagBySlug
                        )
                    )
                    {
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Keeps denormalized recipe binding in sync without touching commercial fields.
        /// </summary>
        private static bool SyncRecipeBinding(
            ShopMaterialSku sku,
            PlacementRecipe recipe,
            DateTime now
        )
        {
            var changed = false;
            if (sku.PlacementRecipeId != recipe.Id)
            {
                sku.PlacementRecipeId = recipe.Id;
                changed = true;
            }

            if (sku.QrType != recipe.PlacementKey)
            {
                sku.QrType = recipe.PlacementKey;
                changed = true;
            }

            if (changed)
            {
                sku.UpdatedAtUtc = now;
            }

            return changed;
        }

        private static bool ApplySeedToSku(
            ShopMaterialSku sku,
            SeedSku seed,
            PlacementRecipe recipe,
            ShopLaunchCommercialDefaults.LaunchCommercial commercial,
            DateTime now,
            Dictionary<string, ShopMaterialTag> tagBySlug
        )
        {
            var changed = false;
            if (sku.Title != seed.Title)
            {
                sku.Title = seed.Title;
                changed = true;
            }

            if (sku.Category != seed.Category)
            {
                sku.Category = seed.Category;
                changed = true;
            }

            if (sku.Description != seed.Description)
            {
                sku.Description = seed.Description;
                changed = true;
            }

            if (sku.Material != seed.Material)
            {
                sku.Material = seed.Material;
                changed = true;
            }

            if (sku.Dimensions != seed.Dimensions)
            {
                sku.Dimensions = seed.Dimensions;
                changed = true;
            }

            if (sku.PlacementRecipeId != recipe.Id)
            {
                sku.PlacementRecipeId = recipe.Id;
                changed = true;
            }

            if (sku.QrType != recipe.PlacementKey)
            {
                sku.QrType = recipe.PlacementKey;
                changed = true;
            }

            var packsJson = ShopMaterialPacks.Serialize(commercial.Packs);
            if (sku.PacksJson != packsJson)
            {
                sku.PacksJson = packsJson;
                changed = true;
            }

            if (sku.DeliveryNetPence != commercial.DeliveryNetPence)
            {
                sku.DeliveryNetPence = commercial.DeliveryNetPence;
                changed = true;
            }

            var unitNet = ShopMaterialPacks.LowestNetPence(commercial.Packs);
            if (sku.UnitNetPence != unitNet)
            {
                sku.UnitNetPence = unitNet;
                changed = true;
            }

            var minQty = ShopMaterialPacks.LowestQty(commercial.Packs);
            if (sku.MinOrderQty != minQty)
            {
                sku.MinOrderQty = minQty;
                changed = true;
            }

            if (sku.Currency != seed.Currency)
            {
                sku.Currency = seed.Currency;
                changed = true;
            }

            var mint =
                seed.MintOnShopFulfilment ?? recipe.MintOnFulfilmentDefault;
            if (sku.MintOnShopFulfilment != mint)
            {
                sku.MintOnShopFulfilment = mint;
                changed = true;
            }

            if (sku.Status != ShopMaterialSkuStatuses.Published)
            {
                sku.Status = ShopMaterialSkuStatuses.Published;
                sku.PublishedAtUtc ??= now;
                changed = true;
            }

            if (sku.AvailableInShop != seed.AvailableInShop)
            {
                sku.AvailableInShop = seed.AvailableInShop;
                changed = true;
            }

            if (sku.AboutBody != seed.AboutBody)
            {
                sku.AboutBody = seed.AboutBody;
                changed = true;
            }

            var specsJson = ShopMaterialSpecifications.Serialize(seed.Specifications);
            if (sku.SpecificationsJson != specsJson)
            {
                sku.SpecificationsJson = specsJson;
                changed = true;
            }

            var desiredTags = seed.TagSlugs
                .Where(tagBySlug.ContainsKey)
                .Select(slug => tagBySlug[slug].Id)
                .OrderBy(id => id)
                .ToArray();
            var currentTags = sku.SkuTags
                .Select(row => row.TagId)
                .OrderBy(id => id)
                .ToArray();
            if (!desiredTags.SequenceEqual(currentTags))
            {
                sku.SkuTags.Clear();
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

                changed = true;
            }

            if (changed)
            {
                sku.UpdatedAtUtc = now;
            }

            return changed;
        }

        /// <summary>
        /// Fills empty PacksJson from launch PRD defaults (or a single UnitNetPence pack).
        /// </summary>
        private static async Task BackfillPacksAndDeliveryAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken
        )
        {
            var skus = await context.ShopMaterialSkus.ToListAsync(cancellationToken);
            var changed = false;
            foreach (var sku in skus)
            {
                if (sku.Status == ShopMaterialSkuStatuses.Deleted)
                {
                    continue;
                }

                var packs = ShopMaterialPacks.Parse(sku.PacksJson);
                if (packs.Count > 0)
                {
                    continue;
                }

                var commercial = ResolveCommercial(sku.SkuId, sku.UnitNetPence);
                sku.PacksJson = ShopMaterialPacks.Serialize(commercial.Packs);
                sku.DeliveryNetPence = commercial.DeliveryNetPence;
                sku.UnitNetPence = ShopMaterialPacks.LowestNetPence(commercial.Packs);
                sku.MinOrderQty = ShopMaterialPacks.LowestQty(commercial.Packs);
                sku.UpdatedAtUtc = DateTime.UtcNow;
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private static ShopLaunchCommercialDefaults.LaunchCommercial ResolveCommercial(
            string skuId,
            int fallbackUnitNetPence
        )
        {
            if (ShopLaunchCommercialDefaults.TryForSkuId(skuId, out var commercial))
            {
                return commercial;
            }

            return new ShopLaunchCommercialDefaults.LaunchCommercial(
                skuId,
                ShopMaterialPacks.SinglePack(1, Math.Max(0, fallbackUnitNetPence)),
                DeliveryNetPence: 0
            );
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
            var qrConnection = new ShopMaterialSpecificationRow
            {
                Label = "QR connection",
                Value = "Location and placement-specific",
            };
            var madeToOrder = new ShopMaterialSpecificationRow
            {
                Label = "Production",
                Value = "Made to order",
            };

            yield return new SeedSku(
                CounterDisplaySkuId,
                "QR Counter Display",
                "payment",
                "Place a branded QR counter display where guests order, pay or collect — capture feedback and offers at the counter.",
                "350gsm Soft-touch Recycled Card",
                "A5",
                CounterDisplayRecipeSlug,
                999,
                1,
                "GBP",
                false,
                true,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly QR Counter Display puts a clear feedback and offer prompt on the counter. Each unit uses a location-specific QR placement so Tummly can measure scans and completed feedback.",
                [
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finished size",
                        Value = "A5",
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
                        Label = "Print route",
                        Value = "Embedded A5 PDF",
                    },
                    qrConnection,
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Counters and payment points",
                    },
                    madeToOrder,
                ],
                [
                    ShopMaterialCatalogMapper.IncludedWithPlanSlug,
                    ShopMaterialCatalogMapper.EssentialSlug,
                ]
            );

            yield return new SeedSku(
                "table-tents",
                "Table Tent",
                "tabletop",
                "Place a branded QR prompt on guest tables to collect private feedback after a dine-in visit.",
                "3mm Matte Frosted Acrylic",
                "105mm x 148mm (A6)",
                "table-tent",
                999,
                5,
                "GBP",
                false,
                false,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly Table Tent gives dine-in guests a clear opportunity to share private feedback while their experience is still recent. Each order uses a location-specific QR placement.",
                [
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finished size",
                        Value = "105mm x 148mm (A6)",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Construction",
                        Value = "Folded A-frame",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Material",
                        Value = "3mm Matte Frosted Acrylic",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Print patch",
                        Value = "60×60 mm",
                    },
                    qrConnection,
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Dine-in tables",
                    },
                    madeToOrder,
                ],
                [ShopMaterialCatalogMapper.EssentialSlug]
            );

            yield return new SeedSku(
                "table-talker",
                "Table Talker",
                "tabletop",
                "A larger table prompt for promotions and private feedback on dine-in tables.",
                "350gsm Soft-touch Recycled Card",
                "Table talker",
                "table-talker",
                999,
                10,
                "GBP",
                false,
                false,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly Table Talker is a larger tabletop prompt for promotions and private feedback. Each order uses a location-specific QR placement.",
                [
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Print patch",
                        Value = "60×60 mm",
                    },
                    qrConnection,
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Dine-in tables",
                    },
                    madeToOrder,
                ],
                [ShopMaterialCatalogMapper.EssentialSlug]
            );

            yield return new SeedSku(
                "window-stickers",
                "Window Sticker",
                "window",
                "Invite guests to share feedback using a branded QR prompt at an entrance or front window.",
                "UV-resistant Clear Vinyl Cling",
                "150mm x 150mm",
                "window-sticker",
                499,
                1,
                "GBP",
                false,
                false,
                "/assets/images/shop/tummly-sticker.png",
                "The Tummly Window Sticker invites guests to share private feedback at an entrance or front window. Each order uses a location-specific QR placement.",
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
                        Label = "Print patch",
                        Value = "60×60 mm",
                    },
                    qrConnection,
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Entrance and front windows",
                    },
                    madeToOrder,
                ],
                [ShopMaterialCatalogMapper.EssentialSlug]
            );

            yield return new SeedSku(
                "qr-guest-cards",
                "QR Guest Cards",
                "payment",
                "Hand guests a branded card with a location-specific QR and offer headline for feedback and redemption.",
                "350gsm Soft-touch Recycled Card",
                "80mm x 50.8mm",
                "qr-guest-cards",
                1499,
                250,
                "GBP",
                false,
                false,
                "/assets/images/shop/tummly-sticker.png",
                "Tummly QR Guest Cards carry a location-specific QR and offer headline for feedback and redemption. Print uses the Guest Card CSV route.",
                [
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Finished size",
                        Value = "80mm x 50.8mm",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Material",
                        Value = "350gsm Soft-touch Recycled Card",
                    },
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Print route",
                        Value = "Guest Card CSV",
                    },
                    qrConnection,
                    new ShopMaterialSpecificationRow
                    {
                        Label = "Recommended use",
                        Value = "Hand-out guest cards",
                    },
                    madeToOrder,
                ],
                [ShopMaterialCatalogMapper.EssentialSlug]
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
            bool AvailableInShop,
            string LegacyImagePath,
            string AboutBody,
            ShopMaterialSpecificationRow[] Specifications,
            string[] TagSlugs
        );
    }
}
