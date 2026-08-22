using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantExplainWhyFollowUpTests
    {
        [Theory]
        [InlineData("Why are you recommending this?")]
        [InlineData("why are you recommending")]
        [InlineData("Why did you recommend that")]
        [InlineData("explain this recommendation")]
        public void Detect_RecommendationNeedles(string message)
        {
            Assert.Equal(
                AssistantExplainWhyKind.Recommendation,
                AssistantExplainWhyFollowUp.Detect(message)
            );
        }

        [Theory]
        [InlineData("Explain these results")]
        [InlineData("explain these results")]
        public void Detect_ResultsNeedles(string message)
        {
            Assert.Equal(
                AssistantExplainWhyKind.Results,
                AssistantExplainWhyFollowUp.Detect(message)
            );
        }

        [Theory]
        [InlineData("Why does this need attention?")]
        [InlineData("why does this need attention")]
        [InlineData("Explain what needs attention")]
        [InlineData("explain what needs attention")]
        public void Detect_NeedsAttentionNeedles(string message)
        {
            Assert.Equal(
                AssistantExplainWhyKind.NeedsAttention,
                AssistantExplainWhyFollowUp.Detect(message)
            );
        }

        [Theory]
        [InlineData("What needs attention?")]
        [InlineData("What should I do today?")]
        [InlineData("What can you do")]
        [InlineData("Summarise recent feedback")]
        [InlineData("How do I create a campaign?")]
        public void Detect_OtherAsks_AreNotExplainWhy(string message)
        {
            Assert.Equal(
                AssistantExplainWhyKind.None,
                AssistantExplainWhyFollowUp.Detect(message)
            );
        }

        [Fact]
        public void Detect_NeedsAttentionNeedle_WinsOverRecommendationSubstring()
        {
            Assert.Equal(
                AssistantExplainWhyKind.NeedsAttention,
                AssistantExplainWhyFollowUp.Detect(
                    "why does this need attention if you are recommending"
                )
            );
        }

        [Fact]
        public void InferPriorPath_NeedsAttentionAsk()
        {
            Assert.Equal(
                AssistantExplainWhyPriorPath.NeedsAttention,
                AssistantExplainWhyFollowUp.InferPriorPath(
                    "What needs attention?",
                    "Nothing needs attention at Camden",
                    "## Data\nNothing needs attention right now."
                )
            );
        }

        [Fact]
        public void InferPriorPath_RecommendedNextStepAsk()
        {
            Assert.Equal(
                AssistantExplainWhyPriorPath.RecommendedNextStep,
                AssistantExplainWhyFollowUp.InferPriorPath(
                    "What should I do today?",
                    "Thank recent guests",
                    "## Data\nfields\n\n## Recommendation\ncard"
                )
            );
        }

        [Fact]
        public void InferPriorPath_GroundedBodyWithoutData_IsNone()
        {
            Assert.Equal(
                AssistantExplainWhyPriorPath.None,
                AssistantExplainWhyFollowUp.InferPriorPath(
                    "Cancel that",
                    "Cancelled",
                    "The draft was not saved."
                )
            );
        }

        [Fact]
        public void InferPriorPath_SummariseAsk_WithoutDataHeading_IsDomainRetrieve()
        {
            Assert.Equal(
                AssistantExplainWhyPriorPath.DomainRetrieve,
                AssistantExplainWhyFollowUp.InferPriorPath(
                    "Summarise recent feedback",
                    "Feedback at Camden",
                    "3 comments in the last 7 days."
                )
            );
        }

        [Fact]
        public void InferPriorPath_ProductExpert()
        {
            Assert.Equal(
                AssistantExplainWhyPriorPath.ProductExpert,
                AssistantExplainWhyFollowUp.InferPriorPath(
                    "What can you do",
                    AssistantProductExpertCopy.CapabilitiesTitle,
                    AssistantProductExpertCopy.CapabilitiesBody
                )
            );
        }

        [Fact]
        public void MatchesPrior_RecommendationNeedle_OnNeedsAttention_StillMatches()
        {
            Assert.True(
                AssistantExplainWhyFollowUp.MatchesPrior(
                    AssistantExplainWhyKind.Recommendation,
                    AssistantExplainWhyPriorPath.NeedsAttention,
                    "## Data\nqueue"
                )
            );
        }

        [Fact]
        public void MatchesPrior_NeedsAttentionNeedle_OnMix_DoesNotMatch()
        {
            Assert.False(
                AssistantExplainWhyFollowUp.MatchesPrior(
                    AssistantExplainWhyKind.NeedsAttention,
                    AssistantExplainWhyPriorPath.Mix,
                    "## Data\nqueue\n\n## Recommendation\ncard"
                )
            );
        }

        [Fact]
        public void MatchesPrior_ResultsNeedle_WithoutDataOrRetrievePath_DoesNotMatch()
        {
            Assert.False(
                AssistantExplainWhyFollowUp.MatchesPrior(
                    AssistantExplainWhyKind.Results,
                    AssistantExplainWhyPriorPath.None,
                    "The draft was not saved."
                )
            );
            Assert.True(
                AssistantExplainWhyFollowUp.MatchesPrior(
                    AssistantExplainWhyKind.Results,
                    AssistantExplainWhyPriorPath.DomainRetrieve,
                    "3 comments in the last 7 days."
                )
            );
        }

        [Fact]
        public void NamesNewPeriodOrLocation_Last30_WhenSavedIsLast7()
        {
            var scope = new AssistantAnalysisScopeDto
            {
                OwnedLocationId = 1,
                ReportingPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last7",
                },
            };

            Assert.True(
                AssistantExplainWhyFollowUp.NamesNewPeriodOrLocation(
                    "explain these results for the last 30 days",
                    scope,
                    [new AssistantOwnedLocationRef(1, "Camden", "addr", default)]
                )
            );
            Assert.False(
                AssistantExplainWhyFollowUp.NamesNewPeriodOrLocation(
                    "explain these results for the last 7 days",
                    scope,
                    [new AssistantOwnedLocationRef(1, "Camden", "addr", default)]
                )
            );
        }

        [Fact]
        public void NamesNewPeriodOrLocation_OtherOwnedLocation()
        {
            var scope = new AssistantAnalysisScopeDto
            {
                OwnedLocationId = 1,
                ReportingPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last7",
                },
            };
            var owned = new AssistantOwnedLocationRef[]
            {
                new(1, "Camden", "c", default),
                new(2, "Soho", "s", default),
            };

            Assert.True(
                AssistantExplainWhyFollowUp.NamesNewPeriodOrLocation(
                    "explain these results at Soho",
                    scope,
                    owned
                )
            );
            Assert.False(
                AssistantExplainWhyFollowUp.NamesNewPeriodOrLocation(
                    "explain these results at Camden",
                    scope,
                    owned
                )
            );
        }
    }

    public class AssistantExplainWhyCopyTests
    {
        [Fact]
        public void Expand_NeedsAttention_AddsInterpretation_NoRecommendation()
        {
            var expanded = AssistantExplainWhyCopy.Expand(
                AssistantExplainWhyKind.NeedsAttention,
                AssistantExplainWhyPriorPath.NeedsAttention,
                "2 items need attention at Camden",
                "Needs attention is the now-queue at Camden. It is not the Reporting period.\n\n## Data\n2 feedback items need attention",
                []
            );

            Assert.Equal("2 items need attention at Camden", expanded.Title);
            Assert.Contains("## Interpretation", expanded.Body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantExplainWhyCopy.NeedsAttentionInterpretation,
                expanded.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", expanded.Body, StringComparison.Ordinal);
            Assert.Contains(
                "2 feedback items need attention",
                expanded.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("## Recommendation", expanded.Body, StringComparison.Ordinal);
            Assert.Empty(expanded.Actions);
        }

        [Fact]
        public void Expand_RecommendationNeedle_OnNeedsAttention_StillOmitsRecommendation()
        {
            var expanded = AssistantExplainWhyCopy.Expand(
                AssistantExplainWhyKind.Recommendation,
                AssistantExplainWhyPriorPath.NeedsAttention,
                "1 item needs attention at Camden",
                "## Data\nqueue",
                [new AssistantActionDto { Type = "view-feedback-set", Label = "View feedback" }]
            );

            Assert.DoesNotContain("## Recommendation", expanded.Body, StringComparison.Ordinal);
            Assert.Contains("## Interpretation", expanded.Body, StringComparison.Ordinal);
            var action = Assert.Single(expanded.Actions);
            Assert.Equal("view-feedback-set", action.Type);
        }

        [Fact]
        public void Expand_ProductExpert_AddsInterpretation_KeepsCannedData()
        {
            var expanded = AssistantExplainWhyCopy.Expand(
                AssistantExplainWhyKind.Results,
                AssistantExplainWhyPriorPath.ProductExpert,
                AssistantProductExpertCopy.CapabilitiesTitle,
                AssistantProductExpertCopy.CapabilitiesBody,
                [],
                [AssistantProductExpertTopic.Capabilities]
            );

            Assert.Contains("## Interpretation", expanded.Body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantExplainWhyCopy.CapabilitiesInterpretation,
                expanded.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Read", expanded.Body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantProductExpertCopy.CapabilitiesBody,
                expanded.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "\n## Data\n",
                expanded.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(expanded.Actions);
        }

        [Fact]
        public void Expand_RecommendedNextStep_KeepsRecommendationAndActions()
        {
            var expanded = AssistantExplainWhyCopy.Expand(
                AssistantExplainWhyKind.Recommendation,
                AssistantExplainWhyPriorPath.RecommendedNextStep,
                "Thank recent guests",
                "## Data\n12 Email-eligible guests\n\n## Recommendation\nThank recent guests",
                []
            );

            Assert.Contains("## Interpretation", expanded.Body, StringComparison.Ordinal);
            Assert.Contains("## Data", expanded.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", expanded.Body, StringComparison.Ordinal);
            Assert.Contains("12 Email-eligible guests", expanded.Body, StringComparison.Ordinal);
            Assert.Empty(expanded.Actions);
        }

        [Fact]
        public void Expand_CombinedCreate_KeepsExistingInterpretationAndActions()
        {
            var actions = new[]
            {
                new AssistantActionDto { Type = "review-campaign", Label = "Review campaign draft" },
                new AssistantActionDto { Type = "change-audience", Label = "Change audience" },
                new AssistantActionDto { Type = "review-offer", Label = "Review offer draft" },
            };
            var priorBody =
                "## Interpretation\n\nI saved a Campaign Draft with an attached Offer for Camden.\n\n"
                + "## Data\n- **Location:** Camden";
            var expanded = AssistantExplainWhyCopy.Expand(
                AssistantExplainWhyKind.Results,
                AssistantExplainWhyPriorPath.CombinedCreate,
                "Campaign Draft saved with Offer",
                priorBody,
                actions
            );

            Assert.Equal("Campaign Draft saved with Offer", expanded.Title);
            Assert.Contains(
                "I saved a Campaign Draft with an attached Offer for Camden.",
                expanded.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("## Recommendation", expanded.Body, StringComparison.Ordinal);
            Assert.Equal(3, expanded.Actions.Count);
        }
    }
}
