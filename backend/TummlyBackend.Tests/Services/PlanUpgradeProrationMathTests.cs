using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class PlanUpgradeProrationMathTests
    {
        [Fact]
        public void ProratedNet_HalfRemaining_HalfUpAwayFromZero()
        {
            // Starter 3900 → Growth 9900; ratio 0.5 → 3000
            Assert.Equal(
                3000,
                PlanUpgradeProrationMath.ProratedNetPence(3900, 9900, 0.5m)
            );
            Assert.Equal(600, PlanUpgradeProrationMath.VatOnNetPence(3000));
        }

        [Fact]
        public void ResolvePeriod_Monthly_SubtractsOneMonth()
        {
            var end = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
            var (start, periodEnd) = PlanUpgradeProrationMath.ResolvePeriod(
                end,
                BillingCycles.Monthly
            );
            Assert.Equal(end, periodEnd);
            Assert.Equal(
                new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
                start
            );
        }

        [Fact]
        public void ResolvePeriod_Annual_SubtractsTwelveMonths()
        {
            var end = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
            var (start, _) = PlanUpgradeProrationMath.ResolvePeriod(
                end,
                BillingCycles.Annual
            );
            Assert.Equal(
                new DateTime(2025, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                start
            );
        }
    }
}
