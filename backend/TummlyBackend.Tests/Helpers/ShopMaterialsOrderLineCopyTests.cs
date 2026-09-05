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
                        CatalogSkuId = "counter-cards",
                        TitleSnapshot = "Counter cards",
                    },
                ],
            };

            Assert.Equal(
                "Tummly Shop · Counter cards, Table tents",
                ShopMaterialsOrderLineCopy.FormatOrderDescription(order)
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
