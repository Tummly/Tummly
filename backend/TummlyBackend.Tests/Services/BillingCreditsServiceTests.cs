using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class BillingCreditsServiceTests
    {
        [Theory]
        [InlineData("Pilot", "Starter", true, null, "monthly", true)]
        [InlineData("Starter", "Growth", false, "monthly", "monthly", true)]
        [InlineData("Starter", "Growth", false, "monthly", "annual", false)]
        [InlineData("Growth", "Starter", false, "monthly", "monthly", false)]
        [InlineData("Growth", "Growth", false, "monthly", "annual", false)]
        public void ResolvePlanChangeRequiresPay_MatchesContract(
            string currentPlan,
            string targetPlan,
            bool isPilot,
            string? liveCadence,
            string targetCadence,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                BillingCreditsService.ResolvePlanChangeRequiresPay(
                    currentPlan,
                    targetPlan,
                    isPilot,
                    liveCadence,
                    targetCadence
                )
            );
        }

        [Theory]
        [InlineData(5, 5, false)]
        [InlineData(6, 6, false)]
        [InlineData(7, 6, true)]
        [InlineData(7, 7, false)]
        public void CanRemoveExtraGroupLocation_MatchesContract(
            int includedLocations,
            int activeLocations,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                BillingCreditsService.CanRemoveExtraGroupLocation(
                    includedLocations,
                    activeLocations
                )
            );
        }
    }
}
