using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    public static partial class UkPostcode
    {
        [GeneratedRegex(
            @"^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex PostcodePattern();

        public static bool IsValidFormat(string? postcode)
        {
            if (string.IsNullOrWhiteSpace(postcode))
            {
                return false;
            }

            return PostcodePattern().IsMatch(postcode.Trim());
        }

        public static string NormalizeForLookup(string postcode)
        {
            return postcode
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", string.Empty);
        }

        public static string FormatForDisplay(string postcode)
        {
            var normalized = NormalizeForLookup(postcode);

            if (normalized.Length < 5)
            {
                return normalized;
            }

            return $"{normalized[..^3]} {normalized[^3..]}";
        }
    }
}
