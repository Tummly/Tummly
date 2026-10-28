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

        [Theory]
        [InlineData(LocationGuestPermissionState.Granted, "email-marketing-v1", "Yes, email me.", "email-marketing-v1", "Yes, email me.", true)]
        [InlineData(LocationGuestPermissionState.Granted, "email-marketing-v0", "Yes, email me.", "email-marketing-v1", "Yes, email me.", false)]
        [InlineData(LocationGuestPermissionState.Granted, "email-marketing-v1", "Old custom wording.", "email-marketing-v1", "Yes, email me.", false)]
        [InlineData(LocationGuestPermissionState.Withdrawn, "email-marketing-v1", "Yes, email me.", "email-marketing-v1", "Yes, email me.", false)]
        [InlineData(LocationGuestPermissionState.NotRecorded, "email-marketing-v1", "Yes, email me.", "email-marketing-v1", "Yes, email me.", false)]
        public void IsGrantAlreadyRecorded_OnlyWhenGrantedUnderSameWording(
            LocationGuestPermissionState currentState,
            string currentVersion,
            string currentSnapshot,
            string proposedVersion,
            string proposedSnapshot,
            bool expected
        )
        {
            var current = Evidence(currentVersion, currentSnapshot);
            var proposed = Evidence(proposedVersion, proposedSnapshot);

            Assert.Equal(
                expected,
                LocationGuestChannelPermissionGate.IsGrantAlreadyRecorded(
                    currentState,
                    current,
                    proposed
                )
            );
        }

        [Fact]
        public void IsGrantAlreadyRecorded_GrantedWithoutGrantRow_ReturnsFalse()
        {
            // Legacy marketing preference: state is Granted but no ledger row
            // carries wording evidence, so a Grant is still written.
            var proposed = Evidence("email-marketing-v1", "Yes, email me.");

            Assert.False(
                LocationGuestChannelPermissionGate.IsGrantAlreadyRecorded(
                    LocationGuestPermissionState.Granted,
                    currentGrant: null,
                    proposed
                )
            );
        }

        private static PermissionLedgerEvidence Evidence(
            string wordingVersion,
            string wordingSnapshot
        ) =>
            new(
                Basis: LocationGuestPermissionBases.Consent,
                GuestFormVersion: GuestFormPermissionEvidence.GuestFormVersion,
                WordingVersion: wordingVersion,
                PrivacyNoticeVersion: GuestFormPermissionEvidence.PrivacyNoticeVersion,
                WordingSnapshot: wordingSnapshot
            );

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
