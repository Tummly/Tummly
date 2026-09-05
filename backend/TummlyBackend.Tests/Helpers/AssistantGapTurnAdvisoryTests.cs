using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantGapTurnAdvisoryTests
    {
        [Fact]
        public void Parse_MissingGapKind_DefaultsToCreation()
        {
            var json =
                """{"Target":"gap","Kind":"location","AssistantTask":"create-campaign-draft","Options":["Camden"],"SourceUserMessage":"draft a campaign"}""";

            var state = AssistantGapTurn.Parse(json);

            Assert.NotNull(state);
            Assert.Equal(AssistantGapTurn.GapKindCreation, state!.GapKind);
            Assert.Equal(AssistantGapTurn.KindLocation, state.Kind);
        }

        [Fact]
        public void CreateAdvisory_SerializeParse_RoundTrips()
        {
            var gap = new AdvisoryGap(
                AdvisoryGapReason.MetricAmbiguous,
                ["covers", "capture"],
                "Trends move in different directions.",
                "turn-1"
            );

            var created = AssistantGapTurn.CreateAdvisory(
                gap,
                "How can we grow?"
            );
            var parsed = AssistantGapTurn.Parse(AssistantGapTurn.Serialize(created));

            Assert.NotNull(parsed);
            Assert.Equal(AssistantGapTurn.GapKindAdvisory, parsed!.GapKind);
            Assert.Equal(AssistantGapTurn.KindAdvisoryMetric, parsed.Kind);
            Assert.Equal(["covers", "capture"], parsed.Options);
            Assert.Equal("Trends move in different directions.", parsed.PartialDiagnosisNote);
            Assert.Equal("turn-1", parsed.ConversationTurnId);
            Assert.Equal(nameof(AdvisoryGapReason.MetricAmbiguous), parsed.AdvisoryReason);
            Assert.Equal("How can we grow?", parsed.SourceUserMessage);
            Assert.Equal(AssistantGapTurn.GapSourcePreCheck, parsed.GapSource);
            Assert.True(AssistantGapTurn.IsAdvisoryGap(parsed));
        }

        [Fact]
        public void CreateAdvisory_ModelRequested_SetsGapSource()
        {
            var gap = AssistantAdvisoryIntent.ModelRequestedGap(
                ["covers"],
                "turn-2",
                "Which metric?"
            );
            var created = AssistantGapTurn.CreateAdvisory(
                gap,
                "How are we doing?",
                AssistantGapTurn.GapSourceModelRequested
            );

            Assert.Equal(
                AssistantGapTurn.GapSourceModelRequested,
                created.GapSource
            );
            Assert.Equal(
                nameof(AdvisoryGapReason.ModelRequested),
                created.AdvisoryReason
            );
        }

        [Fact]
        public void Parse_AdvisoryKinds_AreKnown()
        {
            foreach (var kind in new[]
            {
                AssistantGapTurn.KindAdvisoryScope,
                AssistantGapTurn.KindAdvisoryRange,
                AssistantGapTurn.KindAdvisoryMetric,
                AssistantGapTurn.KindAdvisoryData,
                AssistantGapTurn.KindAdvisoryModel,
            })
            {
                var state = new AssistantGapState
                {
                    Kind = kind,
                    GapKind = AssistantGapTurn.GapKindAdvisory,
                    AssistantTask = AssistantTask.Retrieve,
                    SourceUserMessage = "health check",
                };
                var parsed = AssistantGapTurn.Parse(AssistantGapTurn.Serialize(state));
                Assert.NotNull(parsed);
                Assert.Equal(kind, parsed!.Kind);
            }
        }
    }
}
