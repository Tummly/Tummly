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
    }
}
