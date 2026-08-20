using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantCreateTargetsTests
    {
        [Fact]
        public void CanonicalCamdenAsk_IsCampaignOnly()
        {
            var targets = AssistantCreateTargets.Detect(
                "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
            );

            var target = Assert.Single(targets);
            Assert.Equal(AssistantCreateTargets.Campaign, target);
        }

        [Fact]
        public void ShowMeCampaignDrafts_IsNotACreateTarget()
        {
            Assert.Empty(AssistantCreateTargets.Detect("Show me Campaign drafts"));
        }

        [Fact]
        public void CampaignAndOfferDraft_ListsNamedTargetsOnly()
        {
            var targets = AssistantCreateTargets.Detect(
                "Draft an Email Campaign and create an offer draft"
            );

            Assert.Equal(
                [
                    AssistantCreateTargets.Campaign,
                    AssistantCreateTargets.Offer,
                ],
                targets
            );
        }

        [Fact]
        public void UnnamedCreate_ListsCampaignOfferAndRecovery()
        {
            Assert.Equal(
                AssistantCreateTargets.UnnamedOptions,
                AssistantCreateTargets.Detect("help me draft something")
            );
        }

        [Fact]
        public void OfferAndRecovery_ListsNamedTargets()
        {
            var targets = AssistantCreateTargets.Detect(
                "draft an offer, and draft a recovery response"
            );

            Assert.Equal(
                [
                    AssistantCreateTargets.Offer,
                    AssistantCreateTargets.Recovery,
                ],
                targets
            );
        }

        [Fact]
        public void RecoveryOfferAsk_IsRecoveryOnly()
        {
            var targets = AssistantCreateTargets.Detect(
                "Prepare a recovery response with a recovery offer"
            );

            var target = Assert.Single(targets);
            Assert.Equal(AssistantCreateTargets.Recovery, target);
            Assert.Equal(
                AssistantTask.RecoveryPath,
                AssistantTaskClassification.Classify(
                    "Prepare a recovery response with a recovery offer. Weekend brunch"
                )
            );
            Assert.Equal(
                AssistantTask.RecoveryPath,
                AssistantTaskClassification.Classify(
                    "Recover feedback of the last negative feedback recieved"
                )
            );
        }

        [Fact]
        public void Resolve_BindsUniqueTargetName()
        {
            Assert.Equal(
                AssistantCreateTargets.Campaign,
                AssistantCreateTargets.Resolve(
                    AssistantCreateTargets.UnnamedOptions,
                    "Campaign"
                )
            );
            Assert.Null(
                AssistantCreateTargets.Resolve(
                    AssistantCreateTargets.UnnamedOptions,
                    "both"
                )
            );
        }

        [Fact]
        public void Resolve_HelpCentreHowTo_DoesNotBindCampaign()
        {
            Assert.Null(
                AssistantCreateTargets.Resolve(
                    AssistantCreateTargets.UnnamedOptions,
                    "How do I create a campaign?"
                )
            );
        }
    }
}
