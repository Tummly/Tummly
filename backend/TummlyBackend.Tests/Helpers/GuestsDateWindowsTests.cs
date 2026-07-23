using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestsDateWindowsTests
    {
        [Fact]
        public void ResolvePreset_Last7_UsesLocalCalendarWithOffset()
        {
            // 2026-07-22 02:00 UTC with UTC+5 → local 07:00 on Jul 22
            var utcNow = new DateTime(2026, 7, 22, 2, 0, 0, DateTimeKind.Utc);
            var (fromUtc, toUtc) = GuestsDateWindows.ResolvePreset(
                "last-7",
                utcNow,
                utcOffsetMinutes: 300
            );

            // Local today start Jul 22 00:00 +5 → Jul 21 19:00 UTC
            // Last-7 from = local Jul 16 00:00 +5 → Jul 15 19:00 UTC
            Assert.Equal(
                new DateTime(2026, 7, 15, 19, 0, 0, DateTimeKind.Utc),
                fromUtc
            );
            Assert.Equal(utcNow, toUtc);
        }

        [Fact]
        public void ResolvePreset_Today_IsExclusiveEndOfLocalDay()
        {
            var utcNow = new DateTime(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc);
            var (fromUtc, toUtc) = GuestsDateWindows.ResolvePreset(
                "today",
                utcNow,
                utcOffsetMinutes: -240
            );

            // Local now = Jul 22 06:00 UTC-4; local today = Jul 22 00:00-4 → Jul 22 04:00 UTC
            Assert.Equal(
                new DateTime(2026, 7, 22, 4, 0, 0, DateTimeKind.Utc),
                fromUtc
            );
            Assert.Equal(
                new DateTime(2026, 7, 23, 4, 0, 0, DateTimeKind.Utc),
                toUtc
            );
        }
    }
}
