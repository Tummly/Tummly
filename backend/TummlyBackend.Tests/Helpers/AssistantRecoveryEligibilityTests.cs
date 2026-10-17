using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantRecoveryEligibilityTests
    {
        [Fact]
        public void Email_WithoutMarketingAllowed_BlocksRespondEarly()
        {
            var feedback = Feedback(
                ContactType.Email,
                LocationGuestMarketingPreference.OptedOut
            );

            var outcome = AssistantRecoveryEligibility.Evaluate(
                feedback,
                AssistantRecoveryEligibility.IntentRespondToGuest
            );

            var blocked = Assert.IsType<AssistantRecoveryEligibility.Outcome.Blocked>(
                outcome
            );
            Assert.Equal("email-marketing", blocked.Kind);
            Assert.Contains("Email", blocked.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("stored a draft", blocked.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Sms_WithMarketingOptedOut_AllowsRespondToGuest()
        {
            var feedback = Feedback(
                ContactType.Phone,
                LocationGuestMarketingPreference.OptedOut
            );

            var outcome = AssistantRecoveryEligibility.Evaluate(
                feedback,
                AssistantRecoveryEligibility.IntentRespondToGuest
            );

            var allowed = Assert.IsType<AssistantRecoveryEligibility.Outcome.Allowed>(
                outcome
            );
            Assert.Equal("sms", allowed.Channel);
        }

        [Fact]
        public void Email_WithMarketingAllowed_AllowsRespondToGuest()
        {
            var feedback = Feedback(
                ContactType.Email,
                LocationGuestMarketingPreference.Allowed
            );

            var outcome = AssistantRecoveryEligibility.Evaluate(
                feedback,
                AssistantRecoveryEligibility.IntentRespondToGuest
            );

            Assert.IsType<AssistantRecoveryEligibility.Outcome.Allowed>(outcome);
        }

        [Fact]
        public void RecoveryOffer_WithoutMarketing_IsOfferRefused()
        {
            var feedback = Feedback(
                ContactType.Email,
                LocationGuestMarketingPreference.OptedOut
            );

            var outcome = AssistantRecoveryEligibility.Evaluate(
                feedback,
                AssistantRecoveryEligibility.IntentRecoveryOffer
            );

            var blocked = Assert.IsType<AssistantRecoveryEligibility.Outcome.Blocked>(
                outcome
            );
            Assert.Equal("offer-refused", blocked.Kind);
        }

        private static Feedback Feedback(
            ContactType contactType,
            LocationGuestMarketingPreference marketing
        )
            => new()
            {
                Id = 1,
                GuestName = "Mia Khan",
                GuestContact = contactType == ContactType.Email
                    ? "mia@example.com"
                    : "+15551212",
                ContactType = contactType,
                WorkflowStatus = FeedbackWorkflowStatus.New,
                RestaurantLocationId = 1,
                LocationGuest = new LocationGuest
                {
                    Id = 1,
                    MarketingPreference = marketing,
                },
            };
    }
}
