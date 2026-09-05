using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefWeekKeyTests
    {
        private const string London = "Europe/London";

        [Fact]
        public void ForClosedPriorWeek_AtMondayLocalMidnight_UsesWeekThatJustEnded()
        {
            // 2026-08-17 00:00 BST (UTC+1) → 2026-08-16 23:00 UTC
            var utcNow = new DateTime(2026, 8, 16, 23, 0, 0, DateTimeKind.Utc);

            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(London, utcNow);

            Assert.Equal("monday:2026-08-10", closed.WeekKey);
            Assert.Equal(
                new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
                closed.CoverageStartUtc
            );
            Assert.Equal(
                new DateTime(2026, 8, 16, 23, 0, 0, DateTimeKind.Utc),
                closed.CoverageEndUtcExclusive
            );
        }

        [Fact]
        public void ForClosedPriorWeek_JustBeforeMondayLocalMidnight_KeepsPreviousClosedWeek()
        {
            // 2026-08-16 23:59:59 BST → 2026-08-16 22:59:59 UTC
            var utcNow = new DateTime(2026, 8, 16, 22, 59, 59, DateTimeKind.Utc);

            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(London, utcNow);

            Assert.Equal("monday:2026-08-03", closed.WeekKey);
            Assert.Equal(
                new DateTime(2026, 8, 2, 23, 0, 0, DateTimeKind.Utc),
                closed.CoverageStartUtc
            );
            Assert.Equal(
                new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
                closed.CoverageEndUtcExclusive
            );
        }

        [Fact]
        public void ForClosedPriorWeek_MidWeek_StillReportsLastClosedWeek()
        {
            // Wednesday 2026-08-19 12:00 BST → 11:00 UTC
            var utcNow = new DateTime(2026, 8, 19, 11, 0, 0, DateTimeKind.Utc);

            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(London, utcNow);

            Assert.Equal("monday:2026-08-10", closed.WeekKey);
        }

        [Fact]
        public void ForClosedPriorWeek_FridayStart_DoesNotCollideWithMondayKey()
        {
            // Friday 2026-08-21 00:00 BST → closed Fri 2026-08-14 → Fri 2026-08-21
            var utcNow = new DateTime(2026, 8, 20, 23, 0, 0, DateTimeKind.Utc);

            var friday = WeeklyBriefWeekKey.ForClosedPriorWeek(
                London,
                utcNow,
                "friday"
            );
            var monday = WeeklyBriefWeekKey.ForClosedPriorWeek(
                London,
                utcNow,
                "monday"
            );

            Assert.Equal("friday:2026-08-14", friday.WeekKey);
            Assert.Equal("monday:2026-08-10", monday.WeekKey);
            Assert.NotEqual(friday.WeekKey, monday.WeekKey);
        }

        [Fact]
        public void IsGenerateDay_MatchesConfiguredStartWeekday()
        {
            var fridayMidnight = new DateTime(2026, 8, 20, 23, 0, 0, DateTimeKind.Utc);
            Assert.True(
                WeeklyBriefWeekKey.IsGenerateDay(London, fridayMidnight, "friday")
            );
            Assert.False(
                WeeklyBriefWeekKey.IsGenerateDay(London, fridayMidnight, "monday")
            );
        }

        [Fact]
        public void ForClosedPriorWeek_LocalKind_ConvertsToUtcBeforeResolving()
        {
            var localNow = new DateTime(2026, 8, 16, 23, 0, 0, DateTimeKind.Utc)
                .ToLocalTime();

            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(London, localNow);

            Assert.Equal("monday:2026-08-10", closed.WeekKey);
        }

        [Fact]
        public void ForClosedPriorWeek_UnspecifiedKind_Throws()
        {
            var unspecified = new DateTime(
                2026,
                8,
                16,
                23,
                0,
                0,
                DateTimeKind.Unspecified
            );

            Assert.Throws<ArgumentException>(
                () => WeeklyBriefWeekKey.ForClosedPriorWeek(London, unspecified)
            );
        }

        [Theory]
        [InlineData("monday:2026-08-10", true)]
        [InlineData("friday:2026-08-14", true)]
        [InlineData("2026-W33", true)]
        [InlineData(" 2026-W33 ", true)]
        [InlineData("2026-W3", false)]
        [InlineData("2026-w33", false)]
        [InlineData("26-W33", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("not-a-week", false)]
        public void TryNormalizeWeekKey_MatchesWorkspaceOrLegacyIsoForm(
            string? candidate,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                WeeklyBriefWeekKey.TryNormalizeWeekKey(candidate, out _)
            );
        }

        [Fact]
        public void TryNormalizeWeekKey_TrimsValidKey()
        {
            Assert.True(
                WeeklyBriefWeekKey.TryNormalizeWeekKey(
                    " monday:2026-08-10 ",
                    out var weekKey
                )
            );
            Assert.Equal("monday:2026-08-10", weekKey);
        }

        [Fact]
        public void TryNormalizeWeekKey_RejectsInvalidKey()
        {
            Assert.False(
                WeeklyBriefWeekKey.TryNormalizeWeekKey("2026-W3", out var weekKey)
            );
            Assert.Equal(string.Empty, weekKey);
        }

        [Fact]
        public void TryCoverageWindow_WorkspaceWeekKey_UsesLocalMidnightSpan()
        {
            Assert.True(
                WeeklyBriefWeekKey.TryCoverageWindow(
                    "monday:2026-08-10",
                    London,
                    out var startUtc,
                    out var endUtc
                )
            );
            Assert.Equal(
                new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
                startUtc
            );
            Assert.Equal(
                new DateTime(2026, 8, 16, 23, 0, 0, DateTimeKind.Utc),
                endUtc
            );
        }

        [Fact]
        public void TryCoverageWindow_LegacyIsoWeekKey_UsesMondayStart()
        {
            // ISO 2026-W33 → Monday 2026-08-10 … Monday 2026-08-17
            Assert.True(
                WeeklyBriefWeekKey.TryCoverageWindow(
                    "2026-W33",
                    London,
                    out var startUtc,
                    out var endUtc
                )
            );
            Assert.Equal(
                new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
                startUtc
            );
            Assert.Equal(
                new DateTime(2026, 8, 16, 23, 0, 0, DateTimeKind.Utc),
                endUtc
            );
        }

        [Fact]
        public void TryCoverageWindow_RejectsInvalidKey()
        {
            Assert.False(
                WeeklyBriefWeekKey.TryCoverageWindow(
                    "not-a-week",
                    London,
                    out _,
                    out _
                )
            );
        }
    }
}
