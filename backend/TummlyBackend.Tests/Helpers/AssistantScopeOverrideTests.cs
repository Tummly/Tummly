using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

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
        [InlineData("Summarise feedback for the last 50 days", 50)]
        [InlineData("last 1 day", 1)]
        [InlineData("over the last 180 days", 180)]
        public void TryDetectRollingDayCount_ValidRange(string message, int expected)
        {
            Assert.Equal(expected, AssistantScopeOverride.TryDetectRollingDayCount(message));
        }

        [Theory]
        [InlineData("Summarise feedback from the last 30 days")]
        [InlineData("last 0 days")]
        [InlineData("last 181 days")]
        [InlineData("Summarise recent feedback")]
        public void TryDetectRollingDayCount_PresetOrOutOfRange_ReturnsNull(string message)
        {
            Assert.Null(AssistantScopeOverride.TryDetectRollingDayCount(message));
        }

        [Fact]
        public void Apply_Last50Days_SetsCustomRollingWindow()
        {
            var utcNow = new DateTime(2026, 10, 7, 15, 0, 0, DateTimeKind.Utc);
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
                "Summarise feedback for the last 50 days",
                scope,
                utcNow,
                out var periodChanged,
                out var locationsChanged
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.False(locationsChanged);
            Assert.Equal("custom", scope.ReportingPeriod.Kind);
            Assert.Equal("2026-08-19", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-10-07", scope.ReportingPeriod.EndDate);
            Assert.Contains("period", notice!.Kinds);
        }

        [Fact]
        public void Apply_Last30DaySingular_MapsToLast30Preset()
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
                "Summarise feedback for the last 30 day",
                scope,
                new DateTime(2026, 10, 7, 15, 0, 0, DateTimeKind.Utc),
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("preset", scope.ReportingPeriod.Kind);
            Assert.Equal("last30", scope.ReportingPeriod.PresetId);
        }

        [Fact]
        public void RollingDaysPeriod_30_MatchesLast30InclusiveCalendarSpan()
        {
            var utcNow = new DateTime(2026, 10, 7, 15, 0, 0, DateTimeKind.Utc);
            var rolling = AssistantScopeOverride.RollingDaysPeriod(30, utcNow);
            var (fromUtc, _) = AssistantReportingPeriodWindow.Resolve(
                new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last30",
                },
                utcNow
            );

            Assert.Equal("custom", rolling.Kind);
            Assert.Equal(fromUtc.ToString("yyyy-MM-dd"), rolling.StartDate);
            Assert.Equal("2026-10-07", rolling.EndDate);
        }

        [Fact]
        public void PeriodNeedlesForExplainWhy_IncludesRollingExamples()
        {
            var needles = AssistantScopeOverride.PeriodNeedlesForExplainWhy();
            Assert.Contains(needles, item => item.Needle == "last 14 days" && item.PresetId is null);
            Assert.Contains(needles, item => item.Needle == "last 45 days" && item.PresetId is null);
            Assert.Contains(needles, item => item.Needle == "last 30 days" && item.PresetId == "last30");
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

        [Fact]
        public void Apply_Yesterday_SetsCustomUtcDay()
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback from yesterday",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("custom", scope.ReportingPeriod.Kind);
            Assert.Equal("2026-10-07", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-10-07", scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_ThisWeek_SetsMondayThroughToday()
        {
            // Thursday 8 Oct 2026 → week Mon 5 Oct–Thu 8 Oct.
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback this week",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("2026-10-05", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-10-08", scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_LastWeek_SetsPreviousMonSun()
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback last week",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("2026-09-28", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-10-04", scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_LastMonth_SetsPreviousCalendarMonth()
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback last month",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("2026-09-01", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-09-30", scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_Last7Days_WinsOverLastWeek()
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();
            scope.ReportingPeriod.PresetId = "last30";

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback last week for the last 7 days",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("preset", scope.ReportingPeriod.Kind);
            Assert.Equal("last7", scope.ReportingPeriod.PresetId);
        }

        [Fact]
        public void Apply_NamedLocation_SwitchesToSingle()
        {
            var scope = BaseScope();
            var owned = new[]
            {
                new AssistantOwnedLocationRef(1, "Camden", "", CaptureLocationStatus.Active),
                new AssistantOwnedLocationRef(2, "Soho", "", CaptureLocationStatus.Active),
            };

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback at Soho",
                scope,
                new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc),
                owned,
                out var periodChanged,
                out var locationsChanged
            );

            Assert.NotNull(notice);
            Assert.False(periodChanged);
            Assert.True(locationsChanged);
            Assert.Equal(AssistantAnalysisScope.ScopeKindSingle, scope.ScopeKind);
            Assert.Equal(2, scope.OwnedLocationId);
            Assert.Equal("Soho", scope.OwnedLocationName);
            Assert.Equal("Soho", notice!.NextLocationLabel);
        }

        [Fact]
        public void Apply_CompareAll_WinsOverNamedLocation()
        {
            var scope = BaseScope();
            var owned = new[]
            {
                new AssistantOwnedLocationRef(1, "Camden", "", CaptureLocationStatus.Active),
                new AssistantOwnedLocationRef(2, "Soho", "", CaptureLocationStatus.Active),
            };

            var notice = AssistantScopeOverride.Apply(
                "Summarise feedback at Soho across all locations",
                scope,
                new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc),
                owned,
                out _,
                out var locationsChanged
            );

            Assert.NotNull(notice);
            Assert.True(locationsChanged);
            Assert.Equal(AssistantAnalysisScope.ScopeKindAll, scope.ScopeKind);
            Assert.Null(scope.OwnedLocationId);
            Assert.Equal(AssistantAnalysisScope.AllLocationsChromeName, scope.OwnedLocationName);
        }

        [Fact]
        public void Apply_Today_SetsCustomUtcDay()
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "How many feedbacks received today",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("custom", scope.ReportingPeriod.Kind);
            Assert.Equal("2026-10-08", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-10-08", scope.ReportingPeriod.EndDate);
            var window = AssistantReportingPeriodWindow.Resolve(
                scope.ReportingPeriod,
                utcNow
            );
            Assert.Equal(utcNow.Date, window.FromUtc);
            Assert.Equal(utcNow.Date.AddDays(1), window.ToUtc);
        }

        [Fact]
        public void Apply_Today_DoesNotNarrowAttentionAsk()
        {
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "What should I do today?",
                scope,
                new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc),
                out var periodChanged,
                out _
            );

            Assert.Null(notice);
            Assert.False(periodChanged);
            Assert.Equal("last7", scope.ReportingPeriod.PresetId);
        }

        [Theory]
        [InlineData("How many feedbacks received on 3 October", "2026-10-03")]
        [InlineData("How many feedbacks received on 3rd of October 2026", "2026-10-03")]
        [InlineData("How many QR scans on October 3rd", "2026-10-03")]
        [InlineData("How many QR scans on 2026-10-03", "2026-10-03")]
        [InlineData("How many QR scans on 03/10/2026", "2026-10-03")]
        public void Apply_NamedDay_SetsThatUtcDay(string message, string expected)
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                message,
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal(expected, scope.ReportingPeriod.StartDate);
            Assert.Equal(expected, scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_Weekday_SetsMostRecentOnOrBeforeToday()
        {
            // Thursday 8 Oct 2026.
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "How many feedbacks received on Monday",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("2026-10-05", scope.ReportingPeriod.StartDate);
            Assert.Equal("2026-10-05", scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_NamedDayWithoutYear_UsesTheLatestPastOccurrence()
        {
            var utcNow = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "How many feedbacks received on 20 October",
                scope,
                utcNow,
                out var periodChanged,
                out _
            );

            Assert.NotNull(notice);
            Assert.True(periodChanged);
            Assert.Equal("2025-10-20", scope.ReportingPeriod.StartDate);
            Assert.Equal("2025-10-20", scope.ReportingPeriod.EndDate);
        }

        [Fact]
        public void Apply_NamedDay_DoesNotStealOfferExpiry()
        {
            var scope = BaseScope();

            var notice = AssistantScopeOverride.Apply(
                "Create a 25% off lunch offer ending 16 October 2026",
                scope,
                new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc),
                out var periodChanged,
                out _
            );

            Assert.Null(notice);
            Assert.False(periodChanged);
            Assert.Equal("last7", scope.ReportingPeriod.PresetId);
        }

        private static AssistantAnalysisScopeDto BaseScope()
            => new()
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
    }
}
