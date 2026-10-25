using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="LocationGuestChannelPermissionGate.LedgerEventsForGuestFormSubmit"/>
    /// — Feedback follow-up re-grant and marketing grant/withdraw on guest form.
    /// </summary>
    public class LocationGuestChannelPermissionGateTests
    {
        [Fact]
        public void LedgerEventsForGuestFormSubmit_FeedbackFollowUpWithdrawn_DoesNotGrant()
        {
            var restaurant = RestaurantWith(
                feedbackFollowUpEnabled: true,
                emailEnabled: false,
                smsEnabled: false
            );

            var events =
                LocationGuestChannelPermissionGate.LedgerEventsForGuestFormSubmit(
                    restaurant,
                    marketingConsentGranted: true,
                    ContactType.Email,
                    currentMarketingState:
                        LocationGuestPermissionState.NotRecorded,
                    currentFeedbackFollowUpState:
                        LocationGuestPermissionState.Withdrawn
                );

            Assert.DoesNotContain(
                events,
                e =>
                    e.Kind == LocationGuestPermissionKind.FeedbackFollowUp
            );
        }

        [Theory]
        [InlineData(LocationGuestPermissionState.NotRecorded)]
        [InlineData(LocationGuestPermissionState.Granted)]
        public void LedgerEventsForGuestFormSubmit_FeedbackFollowUpNotWithdrawn_Grants(
            LocationGuestPermissionState currentFeedbackFollowUpState
        )
        {
            var restaurant = RestaurantWith(
                feedbackFollowUpEnabled: true,
                emailEnabled: false,
                smsEnabled: false
            );

            var events =
                LocationGuestChannelPermissionGate.LedgerEventsForGuestFormSubmit(
                    restaurant,
                    marketingConsentGranted: false,
                    ContactType.Email,
                    currentMarketingState:
                        LocationGuestPermissionState.NotRecorded,
                    currentFeedbackFollowUpState
                );

            Assert.Contains(
                events,
                e =>
                    e.Kind == LocationGuestPermissionKind.FeedbackFollowUp
                    && e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Grant
            );
        }

        private static Restaurant RestaurantWith(
            bool feedbackFollowUpEnabled,
            bool emailEnabled,
            bool smsEnabled
        ) =>
            new()
            {
                Name = "Gate Test Venue",
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = emailEnabled,
                SmsMarketingPermissionEnabled = smsEnabled,
                FeedbackFollowUpPermissionEnabled = feedbackFollowUpEnabled,
                CreatedAt = DateTime.UtcNow,
            };
    }
}
