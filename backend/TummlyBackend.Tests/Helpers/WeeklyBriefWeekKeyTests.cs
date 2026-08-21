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

            Assert.Equal("2026-W33", closed.WeekKey);
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

            Assert.Equal("2026-W32", closed.WeekKey);
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

            Assert.Equal("2026-W33", closed.WeekKey);
        }

        [Fact]
        public void ForClosedPriorWeek_IsoYearBoundary_UsesIsoWeekYear()
        {
            // Monday 2026-01-05 00:00 GMT → closed week Mon 2025-12-29 (ISO 2026-W01)
            var utcNow = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(London, utcNow);

            Assert.Equal("2026-W01", closed.WeekKey);
        }

        [Fact]
        public void ForClosedPriorWeek_LocalKind_ConvertsToUtcBeforeResolving()
        {
            // Same instant as AtMondayLocalMidnight test: 2026-08-16 23:00 UTC
            var localNow = new DateTime(2026, 8, 16, 23, 0, 0, DateTimeKind.Utc)
                .ToLocalTime();

            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(London, localNow);

            Assert.Equal("2026-W33", closed.WeekKey);
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
        [InlineData("2026-W33", true)]
        [InlineData("2026-W01", true)]
        [InlineData(" 2026-W33 ", true)]
        [InlineData("2026-W3", false)]
        [InlineData("2026-w33", false)]
        [InlineData("26-W33", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("not-a-week", false)]
        public void IsValidWeekKey_MatchesIsoYyyyWwwForm(
            string? candidate,
            bool expected
        )
        {
            Assert.Equal(expected, WeeklyBriefWeekKey.IsValidWeekKey(candidate));
        }

        [Fact]
        public void TryNormalizeWeekKey_TrimsValidKey()
        {
            Assert.True(
                WeeklyBriefWeekKey.TryNormalizeWeekKey(" 2026-W33 ", out var weekKey)
            );
            Assert.Equal("2026-W33", weekKey);
        }

        [Fact]
        public void TryNormalizeWeekKey_RejectsInvalidKey()
        {
            Assert.False(
                WeeklyBriefWeekKey.TryNormalizeWeekKey("2026-W3", out var weekKey)
            );
            Assert.Equal(string.Empty, weekKey);
        }
    }
}
