using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    public static partial class GuestTagNaming
    {
        /// <summary>
        /// Display form: trim and collapse internal whitespace (preserve case).
        /// </summary>
        public static string FormatDisplayName(string name)
            => CollapseWhitespace().Replace(name.Trim(), " ");

        /// <summary>
        /// Catalog identity key: display form lower-cased.
        /// </summary>
        public static string NormalizeName(string name)
            => FormatDisplayName(name).ToLowerInvariant();

        [GeneratedRegex(@"\s+")]
        private static partial Regex CollapseWhitespace();
    }
}
