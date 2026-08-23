using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared post-capture cleanup for Offer path and retired draft interview
    /// item fields. Returns null when the text is not a safe item name.
    /// </summary>
    public static partial class AssistantCapturedItemText
    {
        private static readonly HashSet<string> Fillers = new(StringComparer.OrdinalIgnoreCase)
        {
            "item",
            "offer",
            "item offer",
            "offer item",
            "replacement",
            "replacement item",
        };

        /// <summary>
        /// Truncate ops tails, strip one leading article, discard filler, then
        /// punctuation-clean. Returns null when the remainder is not a safe item.
        /// </summary>
        public static string? TryClean(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var text = raw.Trim();
            if (StartsWithOpsVerb(text))
            {
                return null;
            }

            text = TruncateOpsTail(text).Trim();
            if (text.Length == 0 || StartsWithOpsVerb(text))
            {
                return null;
            }

            text = StripLeadingArticle(text).Trim();
            if (text.Length == 0 || Fillers.Contains(text))
            {
                return null;
            }

            text = CleanPunctuation(text);
            if (text.Length == 0 || Fillers.Contains(text))
            {
                return null;
            }

            return text;
        }

        /// <summary>
        /// After Apply: null → clear; cleaned → store; whitespace → leave.
        /// </summary>
        public static void CleanupStoredField(ref string? field)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                return;
            }

            field = TryClean(field);
        }

        private static string TruncateOpsTail(string text)
        {
            var match = OpsTailRegex().Match(text);
            return match.Success ? text[..match.Index] : text;
        }

        private static string StripLeadingArticle(string text)
        {
            var match = LeadingArticleRegex().Match(text);
            return match.Success ? text[match.Length..] : text;
        }

        private static bool StartsWithOpsVerb(string text)
            => OpsVerbPrefixRegex().IsMatch(text);

        private static string CleanPunctuation(string value)
            => value.Trim().TrimEnd('.', ',', ';');

        [GeneratedRegex(
            @"(?:^|\s+)(?:and|then)\s+(?:attach|create|put|place|add)\b.*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex OpsTailRegex();

        [GeneratedRegex(
            @"^(?:the|a|an)\s+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex LeadingArticleRegex();

        [GeneratedRegex(
            @"^(?:attach|create|put|place|add)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex OpsVerbPrefixRegex();
    }
}
