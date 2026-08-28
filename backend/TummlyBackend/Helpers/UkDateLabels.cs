using System.Globalization;

namespace TummlyBackend.Helpers
{
    public static class UkDateLabels
    {
        public static string Format(DateTime value)
        {
            return value.ToString(
                "d MMMM yyyy",
                CultureInfo.GetCultureInfo("en-GB")
            );
        }
    }
}
