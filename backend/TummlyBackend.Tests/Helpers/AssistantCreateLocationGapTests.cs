using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantCreateLocationGapTests
    {
        private static readonly AssistantGapLocation Camden = new(1, "Camden");
        private static readonly AssistantGapLocation Soho = new(2, "Soho");
        private static readonly AssistantGapLocation CamdenEast = new(3, "Camden East");

        private static IReadOnlyList<AssistantGapLocation> Two()
            => [Camden, Soho];

        private const string CanonicalAtCamden =
            "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden";

        [Fact]
        public void UnnamedCreate_UsesAnalysisScope_EvenWithSeveralOwnedLocations()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign to bring back all currently Email-eligible guests",
                Camden.Id,
                Camden.Name,
                Two()
            );

            Assert.IsType<AssistantLocationGapOutcome.Unnamed>(outcome);
        }

        [Fact]
        public void UniqueNameMatchingAnalysisScope_IsUnique()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                CanonicalAtCamden,
                Camden.Id,
                Camden.Name,
                Two()
            );

            var unique = Assert.IsType<AssistantLocationGapOutcome.Unique>(outcome);
            Assert.Equal(Camden.Id, unique.LocationId);
            Assert.Equal("Camden", unique.LocationName);
        }

        [Fact]
        public void UniqueNameNotAnalysisScope_IsConflictGap()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                CanonicalAtCamden,
                Soho.Id,
                Soho.Name,
                Two()
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindConflict, gap.Kind);
            Assert.Equal(["Soho", "Camden"], gap.Options);
            Assert.Contains("Soho", gap.Body, StringComparison.Ordinal);
            Assert.Contains("Camden", gap.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Camden East", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void TwoNamedLocations_ListsThoseTwo()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign at Camden and Soho",
                Camden.Id,
                Camden.Name,
                Two()
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindTwoNamed, gap.Kind);
            Assert.Equal(["Camden", "Soho"], gap.Options);
            Assert.DoesNotContain("Camden East", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void CollidingName_ListsCollidingOwnedLocationNamesOnly()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign at Camden",
                Soho.Id,
                Soho.Name,
                [Camden, CamdenEast, Soho]
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindAmbiguous, gap.Kind);
            Assert.Equal(["Camden", "Camden East"], gap.Options);
            Assert.DoesNotContain("Soho", string.Join(' ', gap.Options), StringComparison.Ordinal);
        }

        [Fact]
        public void AllLocations_AsksToNameOne_WithoutListingEveryOwnedLocation()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign for all locations",
                Camden.Id,
                Camden.Name,
                Two()
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindAll, gap.Kind);
            Assert.Empty(gap.Options);
            Assert.Contains("Name one", gap.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Soho", gap.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Camden East", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void UnknownName_IsRefusalNotCandidateList()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign at Paris",
                Camden.Id,
                Camden.Name,
                Two()
            );

            var refusal = Assert.IsType<AssistantLocationGapOutcome.Refusal>(outcome);
            Assert.Contains("Paris", refusal.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Camden", refusal.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Soho", refusal.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void CompareAllLocationsPlusCreate_IsUnnamedNotAllLocationsGap()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Compare all locations and create a campaign draft",
                Camden.Id,
                Camden.Name,
                Two()
            );

            Assert.IsType<AssistantLocationGapOutcome.Unnamed>(outcome);
        }

        [Fact]
        public void UnnamedCreate_WhenAnalysisScopeIdIsNull_IsLocationGapWithoutListingVenues()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign to bring back all currently Email-eligible guests",
                analysisScopeLocationId: null,
                "All Locations",
                Two()
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindAll, gap.Kind);
            Assert.Empty(gap.Options);
            Assert.Equal(
                "Which Owned location should this Campaign Draft use? Name one.",
                gap.Body
            );
            Assert.DoesNotContain("Camden", gap.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Soho", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void UniqueName_WhenAnalysisScopeIdIsNull_BindsAndDoesNotConflict()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                CanonicalAtCamden,
                analysisScopeLocationId: null,
                "All Locations",
                Two()
            );

            var unique = Assert.IsType<AssistantLocationGapOutcome.Unique>(outcome);
            Assert.Equal(Camden.Id, unique.LocationId);
            Assert.Equal("Camden", unique.LocationName);
        }

        [Fact]
        public void EverywhereCreate_WhenAnalysisScopeIdIsNull_IsLocationGap()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Create a campaign everywhere",
                analysisScopeLocationId: null,
                "All Locations",
                Two()
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindAll, gap.Kind);
            Assert.Empty(gap.Options);
            Assert.DoesNotContain("Soho", gap.Body, StringComparison.Ordinal);
        }

        [Fact]
        public void UniqueNameAsChoice_BindsEvenWhenNotAnalysisScope()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Camden",
                Soho.Id,
                Soho.Name,
                Two(),
                uniqueNameIsChoice: true
            );

            var unique = Assert.IsType<AssistantLocationGapOutcome.Unique>(outcome);
            Assert.Equal(Camden.Id, unique.LocationId);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("number 1")]
        [InlineData("first")]
        public void OrdinalMessage_DoesNotBindUniqueLocation(string message)
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                message,
                Soho.Id,
                Soho.Name,
                Two(),
                uniqueNameIsChoice: true
            );

            Assert.IsType<AssistantLocationGapOutcome.Unnamed>(outcome);
        }

        [Fact]
        public void LongerUniqueName_WinsOverSubstring()
        {
            var outcome = AssistantCreateLocationGap.Resolve(
                "Draft an Email Campaign at Camden East",
                Soho.Id,
                Soho.Name,
                [Camden, CamdenEast, Soho]
            );

            var gap = Assert.IsType<AssistantLocationGapOutcome.Gap>(outcome);
            Assert.Equal(AssistantCreateLocationGap.KindConflict, gap.Kind);
            Assert.Equal(["Soho", "Camden East"], gap.Options);
        }
    }
}
