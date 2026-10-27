using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Shared arrange steps for Tummly Shop integration tests (cart, place, pay,
    /// fulfilment). Each step opens its own scope on the factory's shared
    /// in-memory store.
    /// </summary>
    public static class ShopTestSeed
    {
        public static async Task SetSkuAvailableInShopAsync(
            TummlyWebApplicationFactory factory,
            string skuId,
            bool available
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var sku = await context.ShopMaterialSkus
                .SingleAsync(row => row.SkuId == skuId);
            sku.AvailableInShop = available;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates an Active 30-day free-item card offer at the location and
        /// attaches it as the location's Offer Card print offer.
        /// </summary>
        public static async Task AttachActiveCardOfferAsync(
            TummlyWebApplicationFactory factory,
            int locationId
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Title = "Free Dessert Friday",
                Description = "Guest card offer",
                Status = CatalogOfferStatus.Active,
                Validity = CatalogOfferValidity.Days30AfterIssue,
                OfferType = CatalogOfferType.FreeItem,
                FreeItemText = "Dessert",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            var location = await context.RestaurantLocations
                .SingleAsync(row => row.Id == locationId);
            location.OfferCardCatalogOfferId = offer.Id;
            await context.SaveChangesAsync();
        }

        public static async Task DetachCardOfferAsync(
            TummlyWebApplicationFactory factory,
            int locationId
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .SingleAsync(row => row.Id == locationId);
            location.OfferCardCatalogOfferId = null;
            await context.SaveChangesAsync();
        }
    }
}
