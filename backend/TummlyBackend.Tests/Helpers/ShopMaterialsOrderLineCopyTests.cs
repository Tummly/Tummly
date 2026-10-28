using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class ShopMaterialsOrderLineCopyTests
    {
        [Fact]
        public void FormatLineName_PrefixesTummlyShop()
        {
            Assert.Equal(
                "Tummly Shop · Table tents",
                ShopMaterialsOrderLineCopy.FormatLineName("Table tents")
            );
        }

        [Fact]
        public void FormatOrderDescription_JoinsProductTitles()
        {
            var order = new ShopOrder
            {
                OrderNumber = "SO-1001",
                Lines =
                [
                    new ShopOrderLine
                    {
                        CatalogSkuId = "table-tents",
                        TitleSnapshot = "Table tents",
                    },
                    new ShopOrderLine
                    {
                        CatalogSkuId = "offer-card",
                        TitleSnapshot = "Offer Card",
                    },
                ],
            };

            Assert.Equal(
                "Tummly Shop · Offer Card, Table tents",
                ShopMaterialsOrderLineCopy.FormatOrderDescription(order)
            );
        }

        [Theory]
        [InlineData("standard", "Tummly Shop · Standard delivery")]
        [InlineData("express", "Tummly Shop · Express delivery")]
        [InlineData(null, "Tummly Shop · Standard delivery")]
        public void DeliveryLineName_FollowsDeliveryMethod(
            string? method,
            string expected
        )
        {
            Assert.Equal(
                expected,
                ShopMaterialsOrderLineCopy.DeliveryLineName(method)
            );
        }

        [Fact]
        public void ExpressDeliveryLineName_IsFriendly()
        {
            Assert.Equal(
                "Tummly Shop · Express delivery",
                ShopMaterialsOrderLineCopy.ExpressDeliveryLineName
            );
        }
    }
}
