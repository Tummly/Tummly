using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantExplainPerformanceTests
    {
        [Theory]
        [InlineData("Explain performance")]
        [InlineData("explain our performance for the last 30 days")]
        [InlineData("Can you explain the performance?")]
        public void LooksLike_MatchesChipAndVariants(string message)
        {
            Assert.True(AssistantExplainPerformance.LooksLike(message));
        }

        [Theory]
        [InlineData("What is the Performance overview?")]
        [InlineData("Offers performance")]
        [InlineData("How are we doing?")]
        public void LooksLike_RejectsNearbyAsks(string message)
        {
            Assert.False(AssistantExplainPerformance.LooksLike(message));
        }

        [Fact]
        public void GroundedBody_EmitsDataAndInterpretationWithKpis()
        {
            var body = AssistantExplainPerformance.GroundedBody(
                "Camden",
                "the last 30 days",
                new AssistantHomeKpiEvidence(2, 0, 5, 0, 7, 0)
            );

            Assert.Contains("## Data", body, StringComparison.Ordinal);
            Assert.Contains("## Interpretation", body, StringComparison.Ordinal);
            Assert.Contains("Camden", body, StringComparison.Ordinal);
            Assert.Contains("the last 30 days", body, StringComparison.Ordinal);
            Assert.Contains("**Feedback submitted:** 2", body, StringComparison.Ordinal);
            Assert.Contains("**Guests joined:** 5", body, StringComparison.Ordinal);
            Assert.Contains("**QR scans:** 7", body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantExplainPerformance.Interpretation,
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("## Recommendation", body, StringComparison.Ordinal);
        }
    }
}
