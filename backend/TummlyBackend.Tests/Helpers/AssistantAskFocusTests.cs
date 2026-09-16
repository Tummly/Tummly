using TummlyBackend.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAskFocusTests
    {
        [Theory]
        [InlineData(
            "Are there any active campaigns for this Location?",
            AssistantAskFocusKind.CampaignsActive
        )]
        [InlineData(
            "Have we had any QR scans today?",
            AssistantAskFocusKind.CaptureQr
        )]
        [InlineData(
            "Are there any Offer Redemptions today?",
            AssistantAskFocusKind.OffersRedemptions
        )]
        [InlineData(
            "Can you create a Campaign?",
            AssistantAskFocusKind.CreateCampaign
        )]
        [InlineData(
            "Any Campaigns live?",
            AssistantAskFocusKind.CampaignsActive
        )]
        [InlineData(
            "Did anyone redeem an Offer today?",
            AssistantAskFocusKind.OffersRedemptions
        )]
        [InlineData(
            "Anyone redeem today?",
            AssistantAskFocusKind.OffersRedemptions
        )]
        [InlineData(
            "Create a Campaign for recent guests.",
            AssistantAskFocusKind.CreateCampaign
        )]
        [InlineData(
            "Any Campaigns sending?",
            AssistantAskFocusKind.CampaignsActive
        )]
        [InlineData(
            "create campaign",
            AssistantAskFocusKind.CreateCampaign
        )]
        [InlineData(
            "start a campaign",
            AssistantAskFocusKind.CreateCampaign
        )]
        [InlineData(
            "help me create a campaign",
            AssistantAskFocusKind.CreateCampaign
        )]
        [InlineData(
            "Feedback this week",
            AssistantAskFocusKind.Feedback
        )]
        public void Detect_MapsTesterPhrases_ToFocus(
            string message,
            AssistantAskFocusKind expected
        )
        {
            Assert.Equal(expected, AssistantAskFocus.Detect(message));
        }

        [Theory]
        [InlineData(
            AssistantAskFocusKind.CampaignsActive,
            AssistantEvidenceDomain.Campaigns,
            true
        )]
        [InlineData(
            AssistantAskFocusKind.CampaignsActive,
            AssistantEvidenceDomain.Feedback,
            false
        )]
        [InlineData(
            AssistantAskFocusKind.CampaignsActive,
            AssistantEvidenceDomain.Capture,
            false
        )]
        [InlineData(
            AssistantAskFocusKind.CaptureQr,
            AssistantEvidenceDomain.Capture,
            true
        )]
        [InlineData(
            AssistantAskFocusKind.CaptureQr,
            AssistantEvidenceDomain.Feedback,
            false
        )]
        [InlineData(
            AssistantAskFocusKind.OffersRedemptions,
            AssistantEvidenceDomain.Offers,
            true
        )]
        [InlineData(
            AssistantAskFocusKind.OffersRedemptions,
            AssistantEvidenceDomain.Feedback,
            false
        )]
        [InlineData(
            AssistantAskFocusKind.CreateCampaign,
            AssistantEvidenceDomain.Campaigns,
            false
        )]
        [InlineData(
            AssistantAskFocusKind.MixedSummary,
            AssistantEvidenceDomain.Feedback,
            true
        )]
        [InlineData(
            AssistantAskFocusKind.Unknown,
            AssistantEvidenceDomain.Offers,
            true
        )]
        public void IncludesDomain_FollowsFocusRules(
            AssistantAskFocusKind focus,
            AssistantEvidenceDomain domain,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                AssistantAskFocus.IncludesDomain(focus, domain)
            );
        }
    }
}
