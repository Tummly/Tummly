using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Billing.PlanEntitlements;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class LocationCapTests
    {
        private static PricebookSnapshot LoadBook()
        {
            var catalog = PricebookCatalog.LoadFromDirectory(
                TestPricebookPaths.PackDirectory()
            );
            return catalog.GetRequired(catalog.CurrentPricebookId);
        }

        [Theory]
        [InlineData(25, 30)]
        [InlineData(40, 30)]
        public void LocationCap_Group_StaysAt30(int extras, int expected)
        {
            var book = LoadBook();

            Assert.True(
                LocationCap.TryResolve(book, "group", extras, out var entitled)
            );
            Assert.Equal(expected, entitled);
        }

        [Theory]
        [InlineData(500, 500)]
        [InlineData(600, 500)]
        public void LocationCap_Custom_500(int configured, int expected)
        {
            var book = LoadBook();

            Assert.True(
                LocationCap.TryResolve(
                    book,
                    "custom",
                    paidExtraLocationCount: 0,
                    customConfiguredLocations: configured,
                    out var entitled
                )
            );
            Assert.Equal(expected, entitled);
        }
    }
}
