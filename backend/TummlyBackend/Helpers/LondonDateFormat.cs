using System.Globalization;

namespace TummlyBackend.Helpers
{
    public static class LondonDateFormat
    {
        public static TimeZoneInfo LondonTimeZone
        {
            get
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
                }
                catch (TimeZoneNotFoundException)
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                }
            }
        }

        public static DateTime ToLondonLocal(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                LondonTimeZone
            );
        }

        /// <summary>UK calendar year of a UTC instant (sequence mint year).</summary>
        public static int UkCalendarYear(DateTime utc) => ToLondonLocal(utc).Year;

        public static string DMmmYyyy(DateTime utc)
        {
            return ToLondonLocal(utc)
                .ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        }
    }
}
