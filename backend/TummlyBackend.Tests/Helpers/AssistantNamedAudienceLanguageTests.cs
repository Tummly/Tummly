using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers;

public class AssistantNamedAudienceLanguageTests
{
    [Theory]
    [InlineData("make capaign for mia khan and emily khan")]
    [InlineData("Make a campaign for Mia Khan and Emily Khan.")]
    [InlineData("Please create an Email Campaign for Mia Khan and Emily Khan only.")]
    public void NaturalCreate_PreservesNamedRecipients(string message)
    {
        var result = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(
            AssistantCampaignDraftBind.Resolve(message, "Main", [], []));
        Assert.StartsWith("Named guests:", result.Fields.AudienceLabel);
        Assert.Contains("mia khan", result.Fields.AudienceLabel.ToLowerInvariant());
        Assert.Contains("emily khan", result.Fields.AudienceLabel.ToLowerInvariant());
    }

    [Fact]
    public void UnclearNamedSelection_AsksInsteadOfDefaultingToAll()
    {
        var result = AssistantCampaignDraftBind.Resolve(
            "make campaign for Mia Khan and Emily Khan from yesterday", "Main", [], []);
        Assert.IsType<AssistantCampaignDraftBindOutcome.Gap>(result);
    }
    [Theory]
    [InlineData("Create a campaign for Mary O’Brien")]
    [InlineData("Change the audience to Mary O’Brien only.")]
    public void ApostropheName_IsNamedAudience(string message)
    {
        var bound = Assert.IsType<AssistantCampaignDraftBindOutcome.Bound>(AssistantCampaignDraftBind.Resolve(message, "Main", [], []));
        Assert.StartsWith("Named guests:", bound.Fields.AudienceLabel);
    }

    [Theory]
    [InlineData("Create a campaign for all eligible guests except Sophie.")]
    [InlineData("Create a campaign for all eligible guests except Mary O’Brien.")]
    public void ExclusionWithPunctuation_IsPreserved(string message)
    {
        Assert.True(AssistantCampaignDraftBind.TryExtractExclusions(message, out var names));
        Assert.Single(names);
    }

    [Fact]
    public void PresetAndLimitWithoutOrder_AsksForSelectionRule()
    {
        var gap = Assert.IsType<AssistantCampaignDraftBindOutcome.Gap>(AssistantCampaignDraftBind.Resolve("Create an email campaign for 10 negative feedback guests", "Main", [], []));
        Assert.Equal(AssistantGapTurn.KindAudience, gap.Kind);
        Assert.Equal(2, gap.Options.Count);
    }
}
