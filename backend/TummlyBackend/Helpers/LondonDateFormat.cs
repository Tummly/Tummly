using System.Globalization;

namespace TummlyBackend.Helpers
{
    public static class LondonDateFormat
    {
        public static string DMmmYyyy(DateTime utc)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                tz
            );
            return local.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        }
    }
}
