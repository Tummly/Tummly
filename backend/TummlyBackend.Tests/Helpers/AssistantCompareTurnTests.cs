using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantCompareTurnTests
    {
        private static readonly AssistantOwnedLocationRef Camden = new(
            1,
            "Camden",
            "1 Camden High Street",
            CaptureLocationStatus.Active
        );

        private static readonly AssistantOwnedLocationRef Soho = new(
            2,
            "Soho",
            "12 Soho Square",
            CaptureLocationStatus.Active
        );

        private static readonly AssistantOwnedLocationRef Shoreditch = new(
            3,
            "Shoreditch",
            "20 Shoreditch High Street",
            CaptureLocationStatus.Active
        );

        private static readonly AssistantOwnedLocationRef Brixton = new(
            4,
            "Brixton",
            "5 Brixton Road",
            CaptureLocationStatus.Paused
        );

        private static IReadOnlyList<AssistantOwnedLocationRef> Four()
            => [Camden, Soho, Shoreditch, Brixton];

        private static IReadOnlyList<AssistantOwnedLocationRef> Three()
            => [Camden, Soho, Shoreditch];

        [Fact]
        public void UnnamedAndAll_Clarify_AndListPaused()
        {
            foreach (var message in new[]
            {
                "Compare my locations",
                "compare all locations",
                "every location",
            })
            {
                var outcome = AssistantCompareTurn.Resolve(
                    message,
                    Camden.Id,
                    Four(),
                    null,
                    isSingleMode: false
                );
                var clarify = Assert.IsType<AssistantCompareOutcome.Clarify>(outcome);
                Assert.Contains("Camden", clarify.Body);
                Assert.Contains("Soho", clarify.Body);
                Assert.Contains("Shoreditch", clarify.Body);
                Assert.Contains("Brixton (Capture-Paused)", clarify.Body);
            }
        }

        [Fact]
        public void CapOverThree_Clarify_DoesNotPickSubset()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare Camden, Soho, Shoreditch and Brixton",
                Camden.Id,
                Four(),
                null,
                isSingleMode: false
            );
            var clarify = Assert.IsType<AssistantCompareOutcome.Clarify>(outcome);
            Assert.Contains("up to 3", clarify.Body);
        }

        [Fact]
        public void Baseline_CompareTo_IncludesSaved()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare to Soho",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            var compare = Assert.IsType<AssistantCompareOutcome.Compare>(outcome);
            Assert.Equal([Camden.Id, Soho.Id], compare.LocationIds);
        }

        [Fact]
        public void Baseline_HereAndThisLocation_IncludesSaved()
        {
            var here = Assert.IsType<AssistantCompareOutcome.Compare>(
                AssistantCompareTurn.Resolve(
                    "Compare here and Soho",
                    Camden.Id,
                    Three(),
                    null,
                    isSingleMode: false
                )
            );
            Assert.Equal([Camden.Id, Soho.Id], here.LocationIds);

            var thisLocation = Assert.IsType<AssistantCompareOutcome.Compare>(
                AssistantCompareTurn.Resolve(
                    "this location vs Soho",
                    Camden.Id,
                    Three(),
                    null,
                    isSingleMode: false
                )
            );
            Assert.Equal([Camden.Id, Soho.Id], thisLocation.LocationIds);
        }

        [Fact]
        public void ExplicitSet_DoesNotAddSaved()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare Soho and Shoreditch",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            var compare = Assert.IsType<AssistantCompareOutcome.Compare>(outcome);
            Assert.Equal([Soho.Id, Shoreditch.Id], compare.LocationIds);
        }

        [Fact]
        public void AmbiguousName_ClarifyWhich()
        {
            var sohoKitchen = new AssistantOwnedLocationRef(
                10,
                "Soho Kitchen",
                "1 Kitchen Street",
                CaptureLocationStatus.Active
            );
            var sohoBar = new AssistantOwnedLocationRef(
                11,
                "Soho Bar",
                "2 Bar Street",
                CaptureLocationStatus.Active
            );
            var outcome = AssistantCompareTurn.Resolve(
                "Compare Soho and Camden",
                Camden.Id,
                [Camden, sohoKitchen, sohoBar],
                null,
                isSingleMode: false
            );
            var clarify = Assert.IsType<AssistantCompareOutcome.Clarify>(outcome);
            Assert.Contains("Soho Kitchen", clarify.Body);
            Assert.Contains("Soho Bar", clarify.Body);
        }

        [Fact]
        public void UnknownName_Dropped_ContinuesWithBaseline()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare Soho and Atlantis",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            var compare = Assert.IsType<AssistantCompareOutcome.Compare>(outcome);
            Assert.Equal(2, compare.LocationIds.Count);
            Assert.Contains(Camden.Id, compare.LocationIds);
            Assert.Contains(Soho.Id, compare.LocationIds);
            Assert.Contains("Atlantis", compare.DroppedUnknownSentence);
        }

        [Fact]
        public void UnknownOnly_ClarifyTooFew()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare Atlantis",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            Assert.IsType<AssistantCompareOutcome.Clarify>(outcome);
        }

        [Fact]
        public void UniqueSubstring_Matches()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare Shore and Cam",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            var compare = Assert.IsType<AssistantCompareOutcome.Compare>(outcome);
            Assert.Contains(Shoreditch.Id, compare.LocationIds);
            Assert.Contains(Camden.Id, compare.LocationIds);
        }

        [Fact]
        public void AddressMatch_WhenNameDoesNot()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare 12 Soho Square and Camden",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            var compare = Assert.IsType<AssistantCompareOutcome.Compare>(outcome);
            Assert.Contains(Soho.Id, compare.LocationIds);
            Assert.Contains(Camden.Id, compare.LocationIds);
        }

        [Fact]
        public void MentionWithoutCompare_IsCaveat()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "How is Soho doing?",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            var mention = Assert.IsType<AssistantCompareOutcome.MentionCaveat>(outcome);
            Assert.Equal("Soho", mention.MentionedLocationName);
        }

        [Fact]
        public void SingleMode_TypedCompare_IsSingleCaveat()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare to Soho",
                Camden.Id,
                [Camden],
                null,
                isSingleMode: true
            );
            Assert.IsType<AssistantCompareOutcome.SingleCaveat>(outcome);
        }

        [Fact]
        public void TwoPeriodAsk_IsCaveat()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Compare last week to last month",
                Camden.Id,
                Three(),
                null,
                isSingleMode: false
            );
            Assert.IsType<AssistantCompareOutcome.TwoPeriodCaveat>(outcome);
        }

        [Fact]
        public void FollowUp_ReusesLastSet()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "which one had more complaints?",
                Camden.Id,
                Three(),
                [Soho.Id, Shoreditch.Id],
                isSingleMode: false
            );
            var compare = Assert.IsType<AssistantCompareOutcome.Compare>(outcome);
            Assert.Equal([Soho.Id, Shoreditch.Id], compare.LocationIds);
        }

        [Fact]
        public void OtherQuestion_IsNotCompare()
        {
            var outcome = AssistantCompareTurn.Resolve(
                "Summarise recent feedback",
                Camden.Id,
                Three(),
                [Soho.Id, Shoreditch.Id],
                isSingleMode: false
            );
            Assert.IsType<AssistantCompareOutcome.NotCompare>(outcome);
        }
    }
}
