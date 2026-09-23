using TummlyBackend.Helpers;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantCombinedCreateCampaignResolveTests
    {
        [Fact]
        public void ExtractNamedCampaignTitle_AttachNamedOfferToNamedCampaign()
        {
            Assert.Equal(
                "Summer win-back",
                AssistantCombinedCreateCampaignResolve.ExtractNamedCampaignTitle(
                    "Attach Happy Hour to Summer win-back campaign"
                )
            );
        }

        [Fact]
        public void ExtractNamedCampaignTitle_AttachItToNamedCampaign()
        {
            Assert.Equal(
                "Summer win-back",
                AssistantCombinedCreateCampaignResolve.ExtractNamedCampaignTitle(
                    "Create 10% off and attach it to Summer win-back campaign"
                )
            );
        }

        [Fact]
        public void ExtractNamedCampaignTitle_RemoveOfferFromNamedCampaign()
        {
            Assert.Equal(
                "Summer win-back",
                AssistantCombinedCreateCampaignResolve.ExtractNamedCampaignTitle(
                    "Remove the offer from Summer win-back campaign"
                )
            );
        }

        [Fact]
        public void Resolve_UniqueDraft_UpdatesExisting()
        {
            var campaigns = new[]
            {
                new AssistantCombinedCreateCampaignRef(
                    1,
                    "Summer win-back",
                    CampaignDraftService.DraftStatus
                ),
            };

            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Create a campaign with 10% off and attach to Summer win-back campaign at Camden",
                campaigns
            );

            var update = Assert.IsType<AssistantCombinedCreateCampaignOutcome.UpdateExisting>(
                outcome
            );
            Assert.Equal(1, update.CampaignId);
        }

        [Fact]
        public void Resolve_TwoDraftMatches_IsGap()
        {
            var campaigns = new[]
            {
                new AssistantCombinedCreateCampaignRef(
                    1,
                    "Summer win-back",
                    CampaignDraftService.DraftStatus
                ),
                new AssistantCombinedCreateCampaignRef(
                    2,
                    "Summer win-back special",
                    CampaignDraftService.DraftStatus
                ),
            };

            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Create a campaign with 10% off and attach to Summer win-back campaign at Camden",
                campaigns
            );

            var gap = Assert.IsType<AssistantCombinedCreateCampaignOutcome.Gap>(outcome);
            Assert.Equal(2, gap.Options.Count);
        }

        [Fact]
        public void Resolve_ScheduledCampaign_RefusesInFlight()
        {
            var campaigns = new[]
            {
                new AssistantCombinedCreateCampaignRef(
                    1,
                    "Summer win-back",
                    CampaignsListService.ScheduledStatus
                ),
            };

            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Create a campaign with 10% off and attach to Summer win-back campaign",
                campaigns
            );

            var refuse = Assert.IsType<AssistantCombinedCreateCampaignOutcome.RefuseInFlight>(
                outcome
            );
            Assert.Contains("Summer win-back", refuse.Body, StringComparison.Ordinal);
            Assert.Contains("Campaigns UI", refuse.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_ZeroDraftMatch_CreatesNewWithNamedTitle()
        {
            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Create 10% off and attach it to New launch campaign",
                []
            );

            var create = Assert.IsType<AssistantCombinedCreateCampaignOutcome.CreateNew>(outcome);
            Assert.Equal("New launch", create.NamedTitle);
        }

        [Fact]
        public void Resolve_AttachOnly_NoName_ListsAllDraftsWithMarks()
        {
            var campaigns = new[]
            {
                new AssistantCombinedCreateCampaignRef(
                    1,
                    "Summer win-back",
                    CampaignDraftService.DraftStatus,
                    OfferId: 9,
                    AttachedOfferTitle: "10% Off Lunch"
                ),
                new AssistantCombinedCreateCampaignRef(
                    2,
                    "Quiet Tuesday",
                    CampaignDraftService.DraftStatus
                ),
            };

            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Attach it to a campaign",
                campaigns,
                attachOnly: true
            );

            var gap = Assert.IsType<AssistantCombinedCreateCampaignOutcome.Gap>(outcome);
            Assert.Equal(2, gap.Options.Count);
            Assert.Contains("Quiet Tuesday", gap.Options);
            Assert.Contains("Summer win-back", gap.Options);
            Assert.Contains("has 10% Off Lunch", gap.Body, StringComparison.Ordinal);
            Assert.Contains("Quiet Tuesday", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_AttachOnly_ZeroMatch_ListsDraftsInsteadOfCreateNew()
        {
            var campaigns = new[]
            {
                new AssistantCombinedCreateCampaignRef(
                    1,
                    "Quiet Tuesday",
                    CampaignDraftService.DraftStatus
                ),
            };

            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Attach Happy Hour to Missing name campaign",
                campaigns,
                attachOnly: true
            );

            var gap = Assert.IsType<AssistantCombinedCreateCampaignOutcome.Gap>(outcome);
            Assert.Equal(["Quiet Tuesday"], gap.Options);
        }

        [Fact]
        public void Resolve_AttachOnly_NoDrafts_EmptyGap()
        {
            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Attach it to a campaign",
                [],
                attachOnly: true
            );

            var gap = Assert.IsType<AssistantCombinedCreateCampaignOutcome.Gap>(outcome);
            Assert.Empty(gap.Options);
            Assert.Equal(AssistantGapAsk.NoCampaignDraftsForAttachBody, gap.Body);
        }

        [Fact]
        public void Resolve_AttachOnly_ScheduledCampaign_RefusesInFlight()
        {
            var campaigns = new[]
            {
                new AssistantCombinedCreateCampaignRef(
                    1,
                    "Summer win-back",
                    CampaignsListService.ScheduledStatus
                ),
            };

            var outcome = AssistantCombinedCreateCampaignResolve.Resolve(
                "Attach Happy Hour to Summer win-back campaign",
                campaigns,
                attachOnly: true
            );

            var refuse = Assert.IsType<AssistantCombinedCreateCampaignOutcome.RefuseInFlight>(
                outcome
            );
            Assert.Contains("Summer win-back", refuse.Body, StringComparison.Ordinal);
        }
    }
}
