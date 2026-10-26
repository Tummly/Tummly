using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class LocationBandPricingTests
    {
        private static Dictionary<int, LocationBandRate> RatesWithBand1()
        {
            return new Dictionary<int, LocationBandRate>(
                LocationBandPricing.ProvisionalRates
            )
            {
                [1] = new LocationBandRate(1, 3900, 39800),
            };
        }

        [Fact]
        public void Slice_6To500_FiveBands()
        {
            var slices = LocationBandPricing.Slice(6, 500);

            Assert.Equal(
                [25, 20, 50, 150, 250],
                slices.Select(s => s.SlotCount).ToArray()
            );
        }

        [Fact]
        public void Slice_29To32_SplitsBand1And2()
        {
            var slices = LocationBandPricing.Slice(29, 32);

            Assert.Equal(
                [
                    new LocationBandSlice(1, 2, 29, 30),
                    new LocationBandSlice(2, 2, 31, 32),
                ],
                slices
            );
        }

        [Fact]
        public void Slice_LastBeforeFirst_IsEmpty()
        {
            Assert.Empty(LocationBandPricing.Slice(10, 9));
        }

        [Fact]
        public void TryPrice_Monthly_6To500()
        {
            var ok = LocationBandPricing.TryPrice(
                6,
                500,
                "monthly",
                RatesWithBand1(),
                out var netPence,
                out var missingBand
            );

            Assert.True(ok);
            Assert.Null(missingBand);
            Assert.Equal(1616500, netPence);
        }

        [Fact]
        public void TryPrice_Annual_6To500()
        {
            var ok = LocationBandPricing.TryPrice(
                6,
                500,
                "annual",
                RatesWithBand1(),
                out var netPence,
                out var missingBand
            );

            Assert.True(ok);
            Assert.Null(missingBand);
            Assert.Equal(16488800, netPence);
        }

        [Fact]
        public void TryPrice_MissingBand3_ReturnsFalse()
        {
            var rates = RatesWithBand1();
            rates.Remove(3);

            var ok = LocationBandPricing.TryPrice(
                6,
                60,
                "monthly",
                rates,
                out _,
                out var missingBand
            );

            Assert.False(ok);
            Assert.Equal(3, missingBand);
        }

        [Fact]
        public void TryPrice_AnnualMissingBand2_ReturnsFalse_WithZeroNetPence()
        {
            var rates = RatesWithBand1();
            rates[2] = new LocationBandRate(2, 3700, null);

            var ok = LocationBandPricing.TryPrice(
                6,
                31,
                "annual",
                rates,
                out var netPence,
                out var missingBand
            );

            Assert.False(ok);
            Assert.Equal(2, missingBand);
            Assert.Equal(0, netPence);
        }

        [Fact]
        public void TryPrice_MissingBand3_NotNeededUpTo50()
        {
            var rates = RatesWithBand1();
            rates.Remove(3);

            var ok = LocationBandPricing.TryPrice(
                6,
                50,
                "monthly",
                rates,
                out _,
                out var missingBand
            );

            Assert.True(ok);
            Assert.Null(missingBand);
        }

        [Theory]
        [InlineData(6, 1)]
        [InlineData(30, 1)]
        [InlineData(31, 2)]
        [InlineData(50, 2)]
        [InlineData(51, 3)]
        [InlineData(100, 3)]
        [InlineData(101, 4)]
        [InlineData(250, 4)]
        [InlineData(251, 5)]
        [InlineData(500, 5)]
        public void BandIndexForSlot_MapsSlotToBand(int slot, int expectedBand)
        {
            Assert.Equal(expectedBand, LocationBandPricing.BandIndexForSlot(slot));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(501)]
        public void BandIndexForSlot_OutsideRange_Throws(int slot)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LocationBandPricing.BandIndexForSlot(slot)
            );
        }

        [Fact]
        public void Seeder_FromSnapshot_AddsBandRates()
        {
            var json = PricebookCatalog.LoadFromDirectory(
                TestPricebookPaths.PackDirectory()
            );
            var snapshot = json.GetRequired(json.CurrentPricebookId);
            var version = CommercialCatalogSeeder.FromSnapshot(
                snapshot,
                DateTime.UtcNow
            );

            AssertBandRow(version, "extra_location_band_2", 3700, 37740);
            AssertBandRow(version, "extra_location_band_3", 3500, 35700);
            AssertBandRow(version, "extra_location_band_4", 3300, 33660);
            AssertBandRow(version, "extra_location_band_5", 3100, 31620);

            var group = version.Plans.Single(p => p.PlanKey == "group");
            Assert.Equal(30, group.SelfServeMaxLocations);
        }

        private static void AssertBandRow(
            CommercialCatalogVersion version,
            string rateKey,
            int monthlyNetPence,
            int annualNetPence
        )
        {
            var row = Assert.Single(version.CustomUnitRates, r => r.RateKey == rateKey);
            Assert.Equal(1, row.UnitQuantity);
            Assert.Equal(monthlyNetPence, row.MonthlyNetPence);
            Assert.Equal(annualNetPence, row.AnnualNetPence);
        }
    }
}
