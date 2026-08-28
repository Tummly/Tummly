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
    }
}
