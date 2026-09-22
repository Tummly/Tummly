using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="GuestFormPermissionEvidence"/> — Guest Form ledger
    /// evidence (basis, versions, wording snapshot).
    /// </summary>
    public class GuestFormPermissionEvidenceTests
    {
        [Fact]
        public void ForMarketingGrant_PrefersRestaurantCustomWording()
        {
            var evidence = GuestFormPermissionEvidence.ForMarketingGrant(
                ContactType.Email,
                "Cafe",
                "Custom email wording from operator"
            );
            Assert.Equal(LocationGuestPermissionBases.Consent, evidence.Basis);
            Assert.Equal(
                "Custom email wording from operator",
                evidence.WordingSnapshot
            );
            Assert.Equal(
                GuestFormPermissionEvidence.EmailMarketingWordingVersion,
                evidence.WordingVersion
            );
        }

        [Fact]
        public void ForMarketingWithdraw_HasNullBasis()
        {
            var evidence = GuestFormPermissionEvidence.ForMarketingWithdraw(
                ContactType.Phone,
                "Cafe",
                null
            );
            Assert.Null(evidence.Basis);
            Assert.Contains(
                "text me occasional offers",
                evidence.WordingSnapshot
            );
        }

        [Fact]
        public void ForFeedbackFollowUpGrant_UsesServiceFollowUpNoticeBasis()
        {
            var evidence = GuestFormPermissionEvidence.ForFeedbackFollowUpGrant(
                "Cafe",
                "Camden High Street"
            );
            Assert.Equal(
                LocationGuestPermissionBases.ServiceFollowUpNotice,
                evidence.Basis
            );
            Assert.Equal(
                GuestFormPermissionEvidence.FeedbackFollowUpWordingVersion,
                evidence.WordingVersion
            );
            Assert.Equal(
                "Your feedback is shared privately with the team at Cafe, Camden High Street. "
                    + "They may follow up using the contact details you provide.",
                evidence.WordingSnapshot
            );
            Assert.Equal(
                GuestFormPermissionEvidence.GuestFormVersion,
                evidence.GuestFormVersion
            );
            Assert.Equal(
                GuestFormPermissionEvidence.PrivacyNoticeVersion,
                evidence.PrivacyNoticeVersion
            );
        }

        [Fact]
        public void ForMarketingGrant_UsesLaunchEmailLabelWhenCustomBlank()
        {
            var evidence = GuestFormPermissionEvidence.ForMarketingGrant(
                ContactType.Email,
                "Cafe",
                "   "
            );
            Assert.Equal(LocationGuestPermissionBases.Consent, evidence.Basis);
            Assert.Equal(
                GuestFormPermissionEvidence.EmailMarketingWordingVersion,
                evidence.WordingVersion
            );
            Assert.Equal(
                "Yes, email me occasional offers and updates from Cafe. "
                    + "You can unsubscribe at any time.",
                evidence.WordingSnapshot
            );
        }

        [Fact]
        public void ForFeedbackFollowUpGrant_FallsBackWhenRestaurantOrLocationEmpty()
        {
            var evidence = GuestFormPermissionEvidence.ForFeedbackFollowUpGrant(
                "  ",
                "  "
            );
            Assert.Contains("this restaurant", evidence.WordingSnapshot);
            Assert.Contains("this location", evidence.WordingSnapshot);
        }
    }
}
