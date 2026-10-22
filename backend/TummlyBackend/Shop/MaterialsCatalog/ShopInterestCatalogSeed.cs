using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    /// <summary>
    /// Seeds the seven Register Interest Shop products (no price / cart).
    /// Visuals optional until Design drops artwork.
    /// </summary>
    public static class ShopInterestCatalogSeed
    {
        public static readonly (
            string ProductKey,
            string Title,
            string Description
        )[] DefaultProducts =
        [
            (
                "receipt-label",
                "Receipt Label",
                "Add a small QR prompt to printed receipts, collection bags or order slips."
            ),
            (
                "delivery-bag-seal",
                "Delivery Bag Seal",
                "Invite takeaway guests to share private feedback with a seal on delivery bags."
            ),
            (
                "standard-package-label",
                "Standard Package Label",
                "Place a trackable QR label on standard packaging before it leaves the kitchen."
            ),
            (
                "drink-cup-seal",
                "Drink / Cup Seal",
                "Add a compact QR seal to cups and drink lids for post-visit feedback."
            ),
            (
                "pizza-box-label",
                "Pizza Box Label",
                "Put a feedback QR on pizza boxes and large takeaway cartons."
            ),
            (
                "poster-customer-club",
                "Poster / Customer Club",
                "Display a poster QR that grows your customer club and private feedback."
            ),
            (
                "tray-liner",
                "Tray Liner",
                "Print a tray liner with a Location QR for dine-in and counter service."
            ),
        ];

        public static async Task EnsureSeededAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default
        )
        {
            var existing = await context.ShopInterestProducts
                .Select(row => row.ProductKey)
                .ToListAsync(cancellationToken);
            var existingSet = existing.ToHashSet(StringComparer.Ordinal);
            var now = DateTime.UtcNow;
            var changed = false;

            foreach (var product in DefaultProducts)
            {
                if (existingSet.Contains(product.ProductKey))
                {
                    continue;
                }

                context.ShopInterestProducts.Add(
                    new ShopInterestProduct
                    {
                        ProductKey = product.ProductKey,
                        Title = product.Title,
                        Description = product.Description,
                        Material = string.Empty,
                        Dimensions = string.Empty,
                        LinkedSkuId = null,
                        IsActive = true,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    }
                );
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
