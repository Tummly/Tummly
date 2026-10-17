using TummlyBackend.Helpers;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class ShopMaterialPacksTests
    {
        [Fact]
        public void LaunchCommercialDefaults_MatchPrdTable_ForAllFiveProducts()
        {
            Assert.Equal(5, ShopLaunchCommercialDefaults.AllFive.Count);

            AssertPacks(
                ShopLaunchCommercialDefaults.QrCounterDisplay,
                delivery: 299,
                (1, 999),
                (2, 1899),
                (5, 4499)
            );
            AssertPacks(
                ShopLaunchCommercialDefaults.TableTent,
                delivery: 399,
                (5, 999),
                (10, 1699),
                (20, 2999)
            );
            AssertPacks(
                ShopLaunchCommercialDefaults.TableTalker,
                delivery: 399,
                (10, 999),
                (25, 1999),
                (50, 3499)
            );
            AssertPacks(
                ShopLaunchCommercialDefaults.WindowSticker,
                delivery: 399,
                (1, 499),
                (2, 799),
                (5, 1499)
            );
            AssertPacks(
                ShopLaunchCommercialDefaults.QrGuestCards,
                delivery: 399,
                (250, 1499),
                (500, 2499),
                (1000, 3999)
            );
        }

        [Fact]
        public void TryValidate_RejectsEmptyAndDuplicateQty()
        {
            Assert.False(ShopMaterialPacks.TryValidate([], out var emptyError));
            Assert.Contains("pack", emptyError, StringComparison.OrdinalIgnoreCase);

            Assert.False(
                ShopMaterialPacks.TryValidate(
                    [
                        new ShopMaterialPackRow { Qty = 1, NetPence = 100 },
                        new ShopMaterialPackRow { Qty = 1, NetPence = 200 },
                    ],
                    out var dupError
                )
            );
            Assert.Contains("unique", dupError, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryResolve_MatchesPackQty()
        {
            var json = ShopMaterialPacks.Serialize(
                [
                    new ShopMaterialPackRow { Qty = 1, NetPence = 999 },
                    new ShopMaterialPackRow { Qty = 2, NetPence = 1899 },
                ]
            );

            Assert.True(ShopMaterialPacks.TryResolve(json, 2, out var pack));
            Assert.Equal(1899, pack!.NetPence);
            Assert.False(ShopMaterialPacks.TryResolve(json, 3, out _));
        }

        private static void AssertPacks(
            ShopLaunchCommercialDefaults.LaunchCommercial commercial,
            int delivery,
            params (int Qty, int NetPence)[] expected
        )
        {
            Assert.Equal(delivery, commercial.DeliveryNetPence);
            Assert.Equal(expected.Length, commercial.Packs.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Qty, commercial.Packs[i].Qty);
                Assert.Equal(expected[i].NetPence, commercial.Packs[i].NetPence);
            }
        }
    }
}
