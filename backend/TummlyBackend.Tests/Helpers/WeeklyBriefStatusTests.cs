using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefStatusTests
    {
        [Theory]
        [InlineData(WeeklyBriefStatus.Succeeded, "succeeded")]
        [InlineData(WeeklyBriefStatus.Failed, "failed")]
        public void ToWireString_UsesStableWireValues(
            WeeklyBriefStatus status,
            string expected
        )
        {
            Assert.Equal(expected, status.ToWireString());
        }

        [Theory]
        [InlineData("succeeded", WeeklyBriefStatus.Succeeded)]
        [InlineData("failed", WeeklyBriefStatus.Failed)]
        public void FromWireString_ParsesStableWireValues(
            string wire,
            WeeklyBriefStatus expected
        )
        {
            Assert.Equal(expected, WeeklyBriefStatusExtensions.FromWireString(wire));
        }
    }
}
