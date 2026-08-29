using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class PlanMigrationMathTests
    {
        [Fact]
        public void RemainingPeriodRatio_Aug2026_Day16Of31_IsSixteenOverThirtyOne()
        {
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var expiresAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

            var ratio = PlanMigrationMath.RemainingPeriodRatio(periodStart, expiresAt, now);

            Assert.Equal(16m / 31m, ratio);
        }

        [Fact]
        public void FloorGrant_StarterToGrowth_Aug16_MatchesLockExample()
        {
            var ratio = 16m / 31m;

            Assert.Equal(206, PlanMigrationMath.FloorGrant(400, ratio));
            Assert.Equal(3_870, PlanMigrationMath.FloorGrant(7_500, ratio));
            Assert.Equal(129, PlanMigrationMath.FloorGrant(250, ratio));
        }

        [Fact]
        public void RemainingPeriodRatio_WhenNowAtOrAfterExpiry_IsZero()
        {
            var periodStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var expiresAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.Equal(
                0m,
                PlanMigrationMath.RemainingPeriodRatio(periodStart, expiresAt, expiresAt)
            );
            Assert.Equal(
                0m,
                PlanMigrationMath.RemainingPeriodRatio(
                    periodStart,
                    expiresAt,
                    expiresAt.AddSeconds(1)
                )
            );
        }
    }
}
