using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantRetrieveToolCatalogTests
    {
        [Fact]
        public void BuildToolsArray_IncludesAllFirstCutTools()
        {
            var tools = AssistantRetrieveToolCatalog.BuildToolsArray();
            var names = tools
                .Select(node => node!["function"]!["name"]!.GetValue<string>())
                .ToHashSet(StringComparer.Ordinal);

            foreach (var name in AssistantRetrieveToolCatalog.All)
            {
                Assert.Contains(name, names);
            }
        }

        [Fact]
        public void BuildToolsArray_CampaignsAllowsIncludeCampaignCopy()
        {
            var tools = AssistantRetrieveToolCatalog.BuildToolsArray();
            var campaigns = tools.First(
                node => node!["function"]!["name"]!.GetValue<string>()
                    == AssistantRetrieveToolCatalog.ReadCampaigns
            );
            var properties = campaigns["function"]!["parameters"]!["properties"]!
                .AsObject();
            Assert.True(properties.ContainsKey("includeCampaignCopy"));
        }

        [Fact]
        public void IsKnown_RejectsUnknown()
        {
            Assert.False(AssistantRetrieveToolCatalog.IsKnown("mutate_campaign"));
            Assert.True(
                AssistantRetrieveToolCatalog.IsKnown(
                    AssistantRetrieveToolCatalog.ReadFeedbackSummary
                )
            );
            Assert.True(
                AssistantRetrieveToolCatalog.IsKnown(
                    AssistantRetrieveToolCatalog.CompareAllLocations
                )
            );
            Assert.True(
                AssistantRetrieveToolCatalog.IsCompareTool(
                    AssistantRetrieveToolCatalog.CompareAllLocations
                )
            );
        }
    }
}
