using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Display and uniqueness helpers for Digital guest link Link names
    /// (same trim / collapse / case-fold pattern as Guest tags).
    /// </summary>
    public static partial class DigitalGuestLinkNaming
    {
        public const int LinkNameMaxLength = 100;

        public const int InternalDescriptionMaxLength = 500;

        /// <summary>
        /// Display form: trim and collapse internal whitespace (preserve case).
        /// </summary>
        public static string FormatLinkName(string name)
            => CollapseWhitespace().Replace(name.Trim(), " ");

        /// <summary>
        /// Uniqueness key among non-archived Digital guest links at a location.
        /// </summary>
        public static string NormalizeLinkName(string name)
            => FormatLinkName(name).ToLowerInvariant();

        /// <summary>
        /// Trim; whitespace-only becomes null.
        /// </summary>
        public static string? FormatInternalDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            return description.Trim();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex CollapseWhitespace();
    }
}
