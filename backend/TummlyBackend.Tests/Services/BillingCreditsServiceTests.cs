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
        [InlineData(0, 5, 5, false)]
        [InlineData(1, 5, 6, false)]
        [InlineData(2, 6, 6, true)]
        [InlineData(2, 6, 7, false)]
        public void CanRemoveExtraGroupLocation_MatchesContract(
            int paidExtra,
            int entitledAfterRemove,
            int activeLocations,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                BillingCreditsService.CanRemoveExtraGroupLocation(
                    paidExtra,
                    entitledAfterRemove,
                    activeLocations
                )
            );
        }
    }
}
