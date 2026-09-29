using System.Text.Json;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class SurfaceReadToolHostTests
    {
        [Fact]
        public void ExecuteCampaignDraft_ReturnsLocationAndOmitsPii()
        {
            var input = new CampaignMessageDraftInput(
                LocationName: "Camden",
                Channel: "email",
                GoalId: "thank",
                AudienceKey: "all",
                OfferStance: "no-offer",
                CampaignName: "Thanks",
                Tone: "warm",
                IncludeNotes: null,
                Mode: "prepare",
                CurrentBody: null,
                CurrentSubject: null
            );
            var results = SurfaceReadToolHost.ExecuteCampaignDraft(
                [
                    new AssistantToolCallRequest(
                        "1",
                        SurfaceReadToolCatalog.ReadLocationDisplayName,
                        "{}"
                    ),
                ],
                input
            );

            Assert.Single(results);
            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("Camden", doc.RootElement.GetProperty("locationName").GetString());
            Assert.False(doc.RootElement.TryGetProperty("email", out _));
            Assert.False(doc.RootElement.TryGetProperty("phone", out _));
        }

        [Fact]
        public void ExecuteRecoveryDraft_ReturnsFeedbackFactsWithoutContact()
        {
            var input = new FeedbackRecoveryDraftInput(
                FeedbackComment: "Slow service",
                Sentiment: "negative",
                IssueTags: ["service"],
                GuestDisplayName: "Alex",
                LocationName: "Camden",
                Channel: "email",
                Purpose: "apology",
                Tone: "warm",
                IncludeNotes: null,
                Mode: "prepare",
                CurrentBody: null,
                CurrentSubject: null
            );
            var results = SurfaceReadToolHost.ExecuteRecoveryDraft(
                [
                    new AssistantToolCallRequest(
                        "1",
                        SurfaceReadToolCatalog.ReadRecoveryFeedbackFacts,
                        "{}"
                    ),
                ],
                input
            );

            Assert.Single(results);
            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("Slow service", doc.RootElement.GetProperty("feedbackComment").GetString());
            Assert.Equal("Alex", doc.RootElement.GetProperty("guestDisplayName").GetString());
            Assert.False(doc.RootElement.TryGetProperty("email", out _));
            Assert.False(doc.RootElement.TryGetProperty("phone", out _));
        }

        [Fact]
        public void ExecuteCampaignDraft_ConfirmedOffer_OmitsRedemptionCode()
        {
            var input = new CampaignMessageDraftInput(
                LocationName: "Camden",
                Channel: "sms",
                GoalId: "promote",
                AudienceKey: "all",
                OfferStance: "with-offer",
                CampaignName: "Promo",
                Tone: "warm",
                IncludeNotes: null,
                Mode: "prepare",
                CurrentBody: null,
                CurrentSubject: null,
                ConfirmedOffer: new CampaignMessageDraftConfirmedOffer(
                    OfferType: "percentage_discount",
                    Title: "10% off",
                    Description: "Ten percent",
                    Validity: "7_days_after_issue",
                    ExpiryDate: null,
                    DiscountPercentage: 10m,
                    DiscountAmount: null,
                    FreeItemText: null,
                    PurchaseRequirement: "no_purchase_required",
                    MinimumSpend: null,
                    AdditionalExclusions: null,
                    ReplacementItemText: null
                )
            );
            var results = SurfaceReadToolHost.ExecuteCampaignDraft(
                [
                    new AssistantToolCallRequest(
                        "1",
                        SurfaceReadToolCatalog.ReadConfirmedOfferFacts,
                        "{}"
                    ),
                ],
                input
            );

            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("10% off", doc.RootElement.GetProperty("title").GetString());
            Assert.False(doc.RootElement.TryGetProperty("redemptionCode", out _));
            Assert.False(doc.RootElement.TryGetProperty("code", out _));
        }
    }
}
