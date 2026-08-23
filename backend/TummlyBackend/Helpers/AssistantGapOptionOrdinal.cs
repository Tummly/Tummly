using System.Globalization;
using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Whole-message ordinal bind against a Gap Options list (1-based Join order).
    /// Fallback only — callers run name / Detect resolve first.
    /// </summary>
    public static partial class AssistantGapOptionOrdinal
    {
        private static readonly Dictionary<string, int> WordOrdinals =
            new(StringComparer.Ordinal)
            {
                ["first"] = 1,
                ["second"] = 2,
                ["third"] = 3,
                ["fourth"] = 4,
                ["fifth"] = 5,
                ["sixth"] = 6,
                ["seventh"] = 7,
                ["eighth"] = 8,
                ["ninth"] = 9,
                ["tenth"] = 10,
                ["eleventh"] = 11,
                ["twelfth"] = 12,
            };

        /// <summary>
        /// <paramref name="cleanedLower"/> must already be trimmed, end-punctuation
        /// cleaned, and lowercased the same way as the caller’s Resolve path.
        /// </summary>
        public static string? TryBind(
            IReadOnlyList<string> options,
            string cleanedLower
        )
        {
            if (options.Count == 0 || cleanedLower.Length == 0)
            {
                return null;
            }

            var oneBased = ParseOneBasedIndex(cleanedLower, options.Count);
            if (oneBased is null
                || oneBased < 1
                || oneBased > options.Count)
            {
                return null;
            }

            return options[oneBased.Value - 1];
        }

        private static int? ParseOneBasedIndex(string cleanedLower, int optionCount)
        {
            if (cleanedLower is "last" or "the last one")
            {
                return optionCount;
            }

            var digit = BareDigitRegex().Match(cleanedLower);
            if (digit.Success)
            {
                return ParsePositiveInt(digit.Groups[1].Value);
            }

            var prefixed = PrefixedDigitRegex().Match(cleanedLower);
            if (prefixed.Success)
            {
                return ParsePositiveInt(prefixed.Groups[1].Value);
            }

            var theOne = TheWordOneRegex().Match(cleanedLower);
            if (theOne.Success
                && WordOrdinals.TryGetValue(theOne.Groups[1].Value, out var theIndex))
            {
                return theIndex;
            }

            if (WordOrdinals.TryGetValue(cleanedLower, out var wordIndex))
            {
                return wordIndex;
            }

            return null;
        }

        private static int? ParsePositiveInt(string raw)
        {
            if (!int.TryParse(
                    raw,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var value)
                || value < 1)
            {
                return null;
            }

            return value;
        }

        [GeneratedRegex(@"^(\d+)$", RegexOptions.CultureInvariant)]
        private static partial Regex BareDigitRegex();

        [GeneratedRegex(
            @"^(?:number|option)\s+(\d+)$",
            RegexOptions.CultureInvariant
        )]
        private static partial Regex PrefixedDigitRegex();

        [GeneratedRegex(
            @"^the\s+([a-z]+)\s+one$",
            RegexOptions.CultureInvariant
        )]
        private static partial Regex TheWordOneRegex();
    }
}
