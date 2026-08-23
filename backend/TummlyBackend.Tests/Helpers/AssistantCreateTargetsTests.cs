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
        public void CampaignAndOfferDraft_RoutesToCombinedCreateNotTwoTargets()
        {
            var targets = AssistantCreateTargets.Detect(
                "Draft an Email Campaign and create an offer draft"
            );

            var target = Assert.Single(targets);
            Assert.Equal(AssistantCreateTargets.Campaign, target);
            Assert.Equal(
                AssistantTask.CreateCampaignWithOffer,
                AssistantTaskClassification.Classify(
                    "Draft an Email Campaign and create an offer draft"
                )
            );
            Assert.Equal(
                AssistantTask.CreateCampaignWithOffer,
                AssistantTaskClassification.Classify(
                    "Create a campaign with 10% off valid 30 days after issue"
                )
            );
            Assert.Equal(
                AssistantTask.CreateCampaignDraft,
                AssistantTaskClassification.Classify("Create a campaign")
            );
            Assert.Equal(
                AssistantTask.OfferPath,
                AssistantTaskClassification.Classify("Create an offer draft")
            );
            Assert.Equal(
                AssistantTask.Refuse,
                AssistantTaskClassification.Classify("How do I create a campaign?")
            );
            Assert.Equal(
                AssistantTask.Retrieve,
                AssistantTaskClassification.Classify("Show me Campaign drafts")
            );
        }

        [Fact]
        public void CampaignAndRecovery_ListsNamedTargets()
        {
            var targets = AssistantCreateTargets.Detect(
                "Draft an Email Campaign and draft a recovery response"
            );

            Assert.Equal(
                [
                    AssistantCreateTargets.Campaign,
                    AssistantCreateTargets.Recovery,
                ],
                targets
            );
        }

        [Fact]
        public void OfferFirstAttachToCampaign_RoutesToCombinedCreate()
        {
            Assert.Equal(
                AssistantTask.CreateCampaignWithOffer,
                AssistantTaskClassification.Classify(
                    "Create 10% off offer and attach it to Summer win-back campaign — if it is not there, create the campaign"
                )
            );
            Assert.Equal(
                AssistantTask.OfferPath,
                AssistantTaskClassification.Classify("Create a 25% Offer at Camden")
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

        public static TheoryData<string, string> OrdinalBindsFirstCampaign()
            => new()
            {
                { "1", AssistantCreateTargets.Campaign },
                { "2", AssistantCreateTargets.Offer },
                { "3", AssistantCreateTargets.Recovery },
                { "number 1", AssistantCreateTargets.Campaign },
                { "option 1", AssistantCreateTargets.Campaign },
                { "first", AssistantCreateTargets.Campaign },
                { "second", AssistantCreateTargets.Offer },
                { "third", AssistantCreateTargets.Recovery },
                { "the first one", AssistantCreateTargets.Campaign },
                { "the second one", AssistantCreateTargets.Offer },
                { "last", AssistantCreateTargets.Recovery },
                { "the last one", AssistantCreateTargets.Recovery },
                { "1.", AssistantCreateTargets.Campaign },
            };

        [Theory]
        [MemberData(nameof(OrdinalBindsFirstCampaign))]
        public void Resolve_Ordinal_BindsOptionInJoinOrder(string message, string expected)
        {
            Assert.Equal(
                expected,
                AssistantCreateTargets.Resolve(
                    AssistantCreateTargets.UnnamedOptions,
                    message
                )
            );
        }

        [Fact]
        public void Resolve_NameWinsOverOrdinalPosition()
        {
            Assert.Equal(
                AssistantCreateTargets.Campaign,
                AssistantCreateTargets.Resolve(
                    [
                        AssistantCreateTargets.Offer,
                        AssistantCreateTargets.Campaign,
                        AssistantCreateTargets.Recovery,
                    ],
                    "Campaign"
                )
            );
        }

        [Theory]
        [InlineData("14 days")]
        [InlineData("10% off")]
        [InlineData("Yes, proceed with number 1")]
        [InlineData("4")]
        [InlineData("latest")]
        [InlineData("most recent")]
        [InlineData("number 0")]
        public void Resolve_NonOrdinalOrOutOfRange_IsMiss(string message)
        {
            Assert.Null(
                AssistantCreateTargets.Resolve(
                    AssistantCreateTargets.UnnamedOptions,
                    message
                )
            );
        }
    }
}
