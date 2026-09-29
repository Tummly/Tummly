using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantScopeOverrideTests
    {
        [Theory]
        [InlineData("Summarise feedback from the last 30 days", "last30")]
        [InlineData("last thirty days please", "last30")]
        [InlineData("for last7", "last7")]
        [InlineData("this month overview", "thisMonth")]
        public void TryDetectPeriodPreset_KnownPresets(string message, string expected)
        {
            Assert.Equal(expected, AssistantScopeOverride.TryDetectPeriodPreset(message));
        }

        [Fact]
        public void TryDetectPeriodPreset_NoCue_ReturnsNull()
        {
            Assert.Null(AssistantScopeOverride.TryDetectPeriodPreset("Summarise recent feedback"));
        }

        [Theory]
        [InlineData("How many guests have we got across all our location?")]
        [InlineData("across all our locations")]
        [InlineData("compare all locations")]
        [InlineData("every venue")]
        public void TryDetectCompareAllIntent_Matches(string message)
        {
            Assert.True(AssistantScopeOverride.TryDetectCompareAllIntent(message));
        }

        [Fact]
        public void TryDetectCompareAllIntent_SingleLocationAsk_False()
        {
            Assert.False(
                AssistantScopeOverride.TryDetectCompareAllIntent("Summarise recent feedback")
            );
        }

        [Fact]
        public void Apply_PeriodOnly_UpdatesScopeAndNotice()
        {
            var scope = new AssistantAnalysisScopeDto
            {
                ScopeKind = "single",
                OwnedLocationId = 1,
                OwnedLocationName = "Camden",
                ReportingPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last7",
                },
            };

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback from the last 30 days",
                scope,
                out var periodChanged,
                out var locationsChanged
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.False(locationsChanged);
            Assert.Equal("last30", scope.ReportingPeriod.PresetId);
            Assert.Contains("period", notice!.Kinds);
            Assert.Equal("Last 7 days", notice.PreviousPeriodLabel);
            Assert.Equal("Last 30 days", notice.NextPeriodLabel);
        }

        [Fact]
        public void Apply_SamePeriod_NoChange()
        {
            var scope = new AssistantAnalysisScopeDto
            {
                ScopeKind = "single",
                OwnedLocationId = 1,
                OwnedLocationName = "Camden",
                ReportingPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last30",
                },
            };

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback from the last 30 days",
                scope,
                out var periodChanged,
                out var locationsChanged
            );

            Assert.Null(notice);
            Assert.False(periodChanged);
            Assert.False(locationsChanged);
        }

        [Fact]
        public void Apply_CompareAllIntent_PromotesToAll()
        {
            var scope = new AssistantAnalysisScopeDto
            {
                ScopeKind = "single",
                OwnedLocationId = 1,
                OwnedLocationName = "Camden",
                ReportingPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last7",
                },
            };

            var notice = AssistantScopeOverride.Apply(
                "How many guests have we got across all our locations?",
                scope,
                out var periodChanged,
                out var locationsChanged
            );

            Assert.NotNull(notice);
            Assert.False(periodChanged);
            Assert.True(locationsChanged);
            Assert.Equal(AssistantAnalysisScope.ScopeKindAll, scope.ScopeKind);
            Assert.Null(scope.OwnedLocationId);
            Assert.Equal(AssistantAnalysisScope.AllLocationsChromeName, scope.OwnedLocationName);
            Assert.Contains("locations", notice!.Kinds);
        }
    }
}
