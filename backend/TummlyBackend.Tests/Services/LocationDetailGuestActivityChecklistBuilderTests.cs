using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class LocationDetailGuestActivityChecklistBuilderTests
    {
        [Fact]
        public void Build_MarksNeedsRecoveryWhenPendingRecoveryExists()
        {
            var checklist = LocationDetailGuestActivityChecklistBuilder.Build(
                guestsCaptured: 12,
                optIns: 8,
                feedback: 3,
                offersClaimed: 2,
                offersRedeemed: 0,
                pendingRecoveryCount: 1,
                pendingFeedbackActionCount: 1
            );

            Assert.Equal("complete", checklist["guestProfilesCreated"]);
            Assert.Equal("needs-action", checklist["feedbackSubmitted"]);
            Assert.Equal("needs-action", checklist["needsRecovery"]);
            Assert.Equal("optional", checklist["offerRedemptions"]);
            Assert.Equal("optional", checklist["unsubscribes"]);
        }

        [Fact]
        public void Build_DefaultsEmptyActivityToOptionalRows()
        {
            var checklist = LocationDetailGuestActivityChecklistBuilder.Build(
                guestsCaptured: 0,
                optIns: 0,
                feedback: 0,
                offersClaimed: 0,
                offersRedeemed: 0,
                pendingRecoveryCount: 0,
                pendingFeedbackActionCount: 0
            );

            Assert.Equal("optional", checklist["guestProfilesCreated"]);
            Assert.Equal("complete", checklist["needsRecovery"]);
        }
    }
}
