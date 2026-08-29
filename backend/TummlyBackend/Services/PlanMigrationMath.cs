namespace TummlyBackend.Services
{
    /// <summary>
    /// Floor increment credits for same-cadence upgrade and Additional Group Location add
    /// on the open Included period (ADR 0042 / lock 07).
    /// </summary>
    public static class PlanMigrationMath
    {
        public static decimal RemainingPeriodRatio(
            DateTime periodStartUtc,
            DateTime expiresAtUtc,
            DateTime nowUtc
        )
        {
            if (nowUtc >= expiresAtUtc)
            {
                return 0m;
            }

            var totalTicks = (expiresAtUtc - periodStartUtc).Ticks;
            if (totalTicks <= 0)
            {
                return 0m;
            }

            var remainingTicks = (expiresAtUtc - nowUtc).Ticks;
            if (remainingTicks <= 0)
            {
                return 0m;
            }

            return (decimal)remainingTicks / totalTicks;
        }

        public static int FloorGrant(int monthlyIncrement, decimal ratio)
        {
            if (monthlyIncrement <= 0 || ratio <= 0m)
            {
                return 0;
            }

            return (int)decimal.Floor(monthlyIncrement * ratio);
        }
    }
}
