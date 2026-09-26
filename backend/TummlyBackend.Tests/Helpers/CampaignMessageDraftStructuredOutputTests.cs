using System.Text.Json.Nodes;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class CampaignMessageDraftStructuredOutputTests
    {
        [Fact]
        public void BuildRequestJson_IncludesConfirmedOfferFacts()
        {
            var input = new CampaignMessageDraftInput(
                LocationName: "Camden",
                Channel: "email",
                GoalId: "thank-recent-guests",
                AudienceKey: "all-eligible-guests",
                OfferStance: "existing-offer",
                CampaignName: null,
                Tone: "friendly_and_clear",
                IncludeNotes: null,
                Mode: "prepare",
                CurrentBody: null,
                CurrentSubject: null,
                ConfirmedOffer: new CampaignMessageDraftConfirmedOffer(
                    OfferType: "percentage_discount",
                    Title: "15% off your next visit",
                    Description: "Enjoy 15% off your next meal with us.",
                    Validity: "30_days_after_issue",
                    ExpiryDate: null,
                    DiscountPercentage: 15m,
                    DiscountAmount: null,
                    FreeItemText: null,
                    PurchaseRequirement: null,
                    MinimumSpend: null,
                    AdditionalExclusions: null,
                    ReplacementItemText: null
                )
            );

            var json = CampaignMessageDraftStructuredOutput.BuildRequestJson(
                "test-deployment",
                input,
                "2026-08-08"
            );

            var root = JsonNode.Parse(json)!.AsObject();
            var userContent = root["messages"]![1]!["content"]!.GetValue<string>();
            var payload = JsonNode.Parse(userContent)!.AsObject();

            Assert.Equal("existing-offer", payload["offerStance"]!.GetValue<string>());
            var offer = payload["confirmedOffer"]!.AsObject();
            Assert.Equal("percentage_discount", offer["offerType"]!.GetValue<string>());
            Assert.Equal("15% off your next visit", offer["title"]!.GetValue<string>());
            Assert.Equal(15m, offer["discountPercentage"]!.GetValue<decimal>());
            Assert.Equal(
                "Enjoy 15% off your next meal with us.",
                offer["description"]!.GetValue<string>()
            );
        }

        [Fact]
        public void BuildRequestJson_OmitsConfirmedOfferWhenNull()
        {
            var input = new CampaignMessageDraftInput(
                LocationName: "Camden",
                Channel: "email",
                GoalId: "thank-recent-guests",
                AudienceKey: "all-eligible-guests",
                OfferStance: "no-offer",
                CampaignName: null,
                Tone: "friendly_and_clear",
                IncludeNotes: null,
                Mode: "prepare",
                CurrentBody: null,
                CurrentSubject: null
            );

            var json = CampaignMessageDraftStructuredOutput.BuildRequestJson(
                "test-deployment",
                input,
                "2026-08-08"
            );

            var root = JsonNode.Parse(json)!.AsObject();
            var userContent = root["messages"]![1]!["content"]!.GetValue<string>();
            var payload = JsonNode.Parse(userContent)!.AsObject();

            Assert.False(payload.ContainsKey("confirmedOffer"));
        }

        [Fact]
        public void BuildSystemPrompt_GroundsOfferFactsAndStrengthensRewrite()
        {
            var prompt = CampaignMessageDraftStructuredOutput.BuildSystemPrompt(
                "2026-08-08"
            );

            Assert.Contains(
                CampaignMessageDraftStructuredOutput.OfferGroundingRevision,
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains("confirmedOffer", prompt, StringComparison.Ordinal);
            Assert.Contains(
                "Do not invent offer terms",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "redemption code",
                prompt,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.Contains(
                "Do not only paraphrase",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "preserve exact offer facts",
                prompt,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
