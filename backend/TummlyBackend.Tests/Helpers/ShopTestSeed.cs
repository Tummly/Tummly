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
        /// Shape of the card offer to create. Defaults to an Active 30-day
        /// free-item offer at the attaching location.
        /// </summary>
        public sealed record CardOfferSetup(
            string Status = CatalogOfferStatus.Active,
            CatalogOfferValidity Validity = CatalogOfferValidity.Days30AfterIssue,
            DateOnly? CustomExpiryDate = null,
            int? OfferLocationId = null
        );

        /// <summary>
        /// Creates a free-item card offer and attaches it as the location's
        /// Offer Card print offer. <see cref="CardOfferSetup.OfferLocationId"/>
        /// sets which location the offer belongs to (default: <paramref name="locationId"/>).
        /// </summary>
        public static async Task<int> AttachCardOfferAsync(
            TummlyWebApplicationFactory factory,
            int locationId,
            CardOfferSetup setup
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var offer = new CatalogOffer
            {
                RestaurantLocationId = setup.OfferLocationId ?? locationId,
                Title = "Free Dessert Friday",
                Description = "Guest card offer",
                Status = setup.Status,
                Validity = setup.Validity,
                CustomExpiryDate = setup.CustomExpiryDate,
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
            return offer.Id;
        }

        /// <summary>
        /// Creates an Active 30-day free-item card offer at the location and
        /// attaches it as the location's Offer Card print offer.
        /// </summary>
        public static Task AttachActiveCardOfferAsync(
            TummlyWebApplicationFactory factory,
            int locationId
        ) => AttachCardOfferAsync(factory, locationId, new CardOfferSetup());

        /// <summary>Adds a second location to an existing restaurant.</summary>
        public static async Task<int> AddLocationAsync(
            TummlyWebApplicationFactory factory,
            int restaurantId,
            string locationName
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = locationName,
                Address = "9 Other Street",
                City = "London",
                Postcode = "SE1 1TQ",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            return location.Id;
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
