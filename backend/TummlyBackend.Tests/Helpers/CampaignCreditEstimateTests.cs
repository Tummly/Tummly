using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class CampaignCreditEstimateTests
    {
        [Fact]
        public void CampaignEstimate_UsesFinalText()
        {
            // 150 characters: one segment as the operator wrote it. With the
            // restaurant prefix and the STOP footer the text is 193 characters, so
            // each recipient needs two segments.
            var body = new string('a', 150);
            Assert.Equal(1, CampaignSmsSegmentCalculator.CountSegments(body));

            Assert.Equal(
                6,
                CampaignCreditEstimate.EstimateUnits("sms", "The Golden Fork", body, 3)
            );
        }
    }
}
