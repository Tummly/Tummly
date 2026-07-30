namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared Capture date-window validation for per-location snapshot and
    /// restaurant-wide overview / Location performance reads.
    /// </summary>
    public static class CaptureDateWindows
    {
        public const int MaxInclusiveCalendarDays = 180;

        public static (DateTime FromUtc, DateTime ToUtc) Resolve(
            DateTime? from,
            DateTime? to
        )
        {
            if (from == null || to == null)
            {
                throw new ArgumentException("from and to are required.");
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                throw new ArgumentException("from must be before to.");
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                throw new ArgumentException(
                    "Date range cannot exceed 180 days."
                );
            }

            return (fromUtc, toUtc);
        }

        public static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
