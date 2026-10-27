using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Shop
{
    /// <summary>
    /// Card-offer rule for Tummly Shop orders. Only QR Guest Cards carry the
    /// location offer headline, so only they need an attached card offer.
    /// QR Counter Display never does.
    /// </summary>
    public static class ShopCardOfferRules
    {
        public static bool RequiresCardOffer(ShopCatalogDetailDto sku)
        {
            return string.Equals(
                sku.QrType,
                nameof(QrType.GuestCard),
                StringComparison.Ordinal
            );
        }

        /// <summary>
        /// True when the location's attached card offer exists, belongs to the
        /// location and is Draft or Active (same predicate as
        /// <c>ShopOfferCardOfferService.SetAsync</c>).
        /// </summary>
        public static async Task<bool> HasAttachableOfferAsync(
            ApplicationDbContext db,
            int restaurantId,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var offerId = await db.RestaurantLocations
                .AsNoTracking()
                .Where(row =>
                    row.Id == locationId
                    && row.RestaurantId == restaurantId
                )
                .Select(row => row.OfferCardCatalogOfferId)
                .FirstOrDefaultAsync(cancellationToken);
            if (offerId is null)
            {
                return false;
            }

            var offer = await db.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == offerId
                        && row.RestaurantLocationId == locationId,
                    cancellationToken
                );
            if (offer == null)
            {
                return false;
            }

            return CatalogOfferStatus.IsAttachable(
                offer.Status,
                offer.Validity,
                offer.CustomExpiryDate,
                CatalogOfferStatus.VenueLocalToday(DateTime.UtcNow, 0)
            );
        }

        /// <summary>
        /// Checks an order at pay time. Returns the first blocking error code,
        /// or null when the order may proceed to checkout.
        /// </summary>
        public static async Task<string?> FirstPayBlockAsync(
            ApplicationDbContext db,
            IMaterialsCatalog catalog,
            ShopOrder order,
            CancellationToken cancellationToken
        )
        {
            var skus = new List<ShopCatalogDetailDto>();
            foreach (var line in order.Lines)
            {
                var sku = catalog.TryBuildDetail(line.CatalogSkuId);
                if (sku == null || !sku.AvailableInShop)
                {
                    return ShopSkuNotAvailableException.ErrorCode;
                }

                skus.Add(sku);
            }

            if (
                skus.Any(RequiresCardOffer)
                && !await HasAttachableOfferAsync(
                    db,
                    order.RestaurantId,
                    order.LocationId,
                    cancellationToken
                )
            )
            {
                return OfferCardOfferRequiredException.ErrorCode;
            }

            return null;
        }
    }
}
