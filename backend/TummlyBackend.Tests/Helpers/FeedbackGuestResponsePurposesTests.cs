using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class FeedbackGuestResponsePurposesTests
    {
        [Theory]
        [InlineData("acknowledge_feedback")]
        [InlineData("apologise_and_confirm_follow_up")]
        [InlineData("ask_for_more_information")]
        [InlineData("confirm_operational_action")]
        [InlineData("create_custom_response")]
        [InlineData("  acknowledge_feedback  ")]
        public void IsAllowed_ServicePurposes_ReturnsTrue(string purpose)
        {
            Assert.True(FeedbackGuestResponsePurposes.IsAllowed(purpose));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsAllowed_Blank_ReturnsTrue_NoPurposeRecorded(string? purpose)
        {
            Assert.True(FeedbackGuestResponsePurposes.IsAllowed(purpose));
        }

        [Theory]
        [InlineData("include_a_recovery_offer")]
        [InlineData("free_dessert_promotion")]
        [InlineData("unknown_purpose")]
        [InlineData("Acknowledge_Feedback")]
        [InlineData("acknowledge feedback")]
        public void IsAllowed_PromotionalOrUnknown_ReturnsFalse(string purpose)
        {
            Assert.False(FeedbackGuestResponsePurposes.IsAllowed(purpose));
        }

        [Fact]
        public void Message_ListsEveryAllowedPurpose()
        {
            Assert.Contains("acknowledge_feedback", FeedbackGuestResponsePurposes.Message);
            Assert.Contains("create_custom_response", FeedbackGuestResponsePurposes.Message);
            Assert.DoesNotContain("include_a_recovery_offer", FeedbackGuestResponsePurposes.Message);
        }
    }
}
