namespace TummlyBackend.Services
{
    internal static class IncludedPeriodCalculator
    {
        public static (DateTime PeriodStartUtc, DateTime ExpiresAtUtc)? ResolveMonthlyPeriod(
            DateTime cycleStartUtc,
            DateTime? nextCycleStartUtc,
            DateTime? cycleEndUtc
        )
        {
            var expiresAtUtc = nextCycleStartUtc ?? cycleEndUtc;
            if (expiresAtUtc == null || expiresAtUtc.Value <= cycleStartUtc)
            {
                return null;
            }

            return (cycleStartUtc, expiresAtUtc.Value);
        }

        public static (DateTime PeriodStartUtc, DateTime ExpiresAtUtc)? ResolveAnnualSlice(
            DateTime yearStartUtc,
            DateTime yearEndUtc,
            int sliceIndex
        )
        {
            if (sliceIndex is < 0 or > 11)
            {
                return null;
            }

            var periodStartUtc = yearStartUtc.AddMonths(sliceIndex);
            var expiresAtUtc = sliceIndex < 11
                ? yearStartUtc.AddMonths(sliceIndex + 1)
                : yearEndUtc;
            if (expiresAtUtc <= periodStartUtc)
            {
                return null;
            }

            return (periodStartUtc, expiresAtUtc);
        }

        public static int? CurrentAnnualSliceIndex(
            DateTime yearStartUtc,
            DateTime yearEndUtc,
            DateTime nowUtc
        )
        {
            for (var sliceIndex = 0; sliceIndex < 12; sliceIndex++)
            {
                var slice = ResolveAnnualSlice(yearStartUtc, yearEndUtc, sliceIndex);
                if (slice == null)
                {
                    continue;
                }

                if (
                    slice.Value.PeriodStartUtc <= nowUtc
                    && nowUtc < slice.Value.ExpiresAtUtc
                )
                {
                    return sliceIndex;
                }
            }

            return null;
        }

        public static DateTime? InferAnnualYearStart(
            IEnumerable<DateTime> periodStartsUtc,
            DateTime nowUtc
        )
        {
            foreach (var candidate in periodStartsUtc.OrderByDescending(start => start))
            {
                var yearEndUtc = candidate.AddMonths(12);
                if (nowUtc >= candidate && nowUtc < yearEndUtc)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
