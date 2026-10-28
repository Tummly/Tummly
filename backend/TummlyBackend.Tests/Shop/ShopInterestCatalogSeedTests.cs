using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Shop
{
    public sealed class ShopInterestCatalogSeedTests
    {
        [Fact]
        public void DefaultProducts_UseTheExactSevenConceptNames()
        {
            var titles = ShopInterestCatalogSeed.DefaultProducts
                .Select(product => product.Title)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    "Receipt Label",
                    "Delivery Bag Seal",
                    "Standard Packaging Label",
                    "Drink Cup Seal",
                    "Pizza Box Label",
                    "Customer Club Poster",
                    "Tray Liner",
                },
                titles
            );
        }

        [Fact]
        public void DefaultProducts_KeepTheirStableProductKeys()
        {
            var keys = ShopInterestCatalogSeed.DefaultProducts
                .Select(product => product.ProductKey)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    "receipt-label",
                    "delivery-bag-seal",
                    "standard-package-label",
                    "drink-cup-seal",
                    "pizza-box-label",
                    "poster-customer-club",
                    "tray-liner",
                },
                keys
            );
        }
    }
}
