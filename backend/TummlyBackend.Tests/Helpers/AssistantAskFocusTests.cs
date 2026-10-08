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
            "What are there? Have you got any scans on QR code today?",
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
            "Can you creaete a camapgin",
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
        [InlineData(
            "What's billing Does my account have at the moment?",
            AssistantAskFocusKind.Billing
        )]
        [InlineData(
            "How many AI credits do I have?",
            AssistantAskFocusKind.Billing
        )]
        [InlineData(
            "How many credits have we used this cycle?",
            AssistantAskFocusKind.Billing
        )]
        [InlineData(
            "Show me message usage",
            AssistantAskFocusKind.Billing
        )]
        [InlineData(
            "Explain performance",
            AssistantAskFocusKind.Performance
        )]
        [InlineData(
            "explain our performance for the last 30 days",
            AssistantAskFocusKind.Performance
        )]
        public void Detect_MapsTesterPhrases_ToFocus(
            string message,
            AssistantAskFocusKind expected
        )
        {
            Assert.Equal(expected, AssistantAskFocus.Detect(message));
        }

        [Fact]
        public void Detect_ExplainPerformance_IncludesHomeDomainOnly()
        {
            var focus = AssistantAskFocus.Detect("Explain performance");
            Assert.Equal(AssistantAskFocusKind.Performance, focus);
            Assert.True(
                AssistantAskFocus.IncludesDomain(
                    focus,
                    AssistantEvidenceDomain.Home
                )
            );
            Assert.False(
                AssistantAskFocus.IncludesDomain(
                    focus,
                    AssistantEvidenceDomain.Offers
                )
            );
            Assert.Equal(
                new[] { AssistantRetrieveToolCatalog.ReadHomeKpis },
                AssistantRetrieveToolCatalog.DomainReadsForFocus(focus)
            );
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
