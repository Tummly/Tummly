using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Operator-facing live answer text. Field names and action schema
    /// stay out of the title and body. The server shows Actions.
    /// </summary>
    public static partial class AssistantOperatorProse
    {
        private static readonly Dictionary<string, string> PlainNames =
            new(StringComparer.Ordinal)
            {
                ["feedbackTotalCount"] = "the feedback count",
                ["feedbackSampleCount"] = "the feedback sample",
                ["guestsTotalCount"] = "the guest count",
                ["guestRows"] = "the guest list",
                ["placeholder4GuestRows"] = "the guest list",
            };

        public static string AlignPeriod(string? text, string? periodPhrase)
        {
            var body = text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(body)
                || string.IsNullOrWhiteSpace(periodPhrase)
                || periodPhrase.Contains("last 7 days", StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }

            body = LastSevenDays().Replace(body, periodPhrase.Trim());
            if (body.Contains("credit", StringComparison.OrdinalIgnoreCase)
                || body.Contains("plan", StringComparison.OrdinalIgnoreCase))
            {
                body = CreditPeriodClause().Replace(body, "$1");
            }

            return body;
        }

        public static string Scrub(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text?.Trim() ?? string.Empty;
            }

            var cleaned = ActionDumpLine().Replace(text, string.Empty);
            cleaned = ActionsHeading().Replace(cleaned, string.Empty);
            cleaned = CamelIdentifier().Replace(
                cleaned,
                match => PlainNames.TryGetValue(match.Value, out var plain)
                    ? plain
                    : SplitCamel(match.Value)
            );
            cleaned = ExtraBlankLines().Replace(cleaned, "\n\n");
            return cleaned.Trim();
        }

        private static string SplitCamel(string value)
        {
            var spaced = CamelBoundary().Replace(value, " $1");
            return spaced.Trim().ToLowerInvariant();
        }

        [GeneratedRegex(
            @"^[ \t]*(?:[-*][ \t]*)?[a-z0-9]+(?:-[a-z0-9]+)+[ \t]*\([ \t]*tab[ \t]*:[ \t]*[^)]*\)[ \t]*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )]
        private static partial Regex ActionDumpLine();

        [GeneratedRegex(
            @"^[ \t]*#{0,3}[ \t]*Actions[ \t]*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant
        )]
        private static partial Regex ActionsHeading();

        [GeneratedRegex(
            @"\b[a-z]+(?:[A-Z][A-Za-z0-9]*)+\b",
            RegexOptions.CultureInvariant
        )]
        private static partial Regex CamelIdentifier();

        [GeneratedRegex(@"([A-Z])", RegexOptions.CultureInvariant)]
        private static partial Regex CamelBoundary();

        [GeneratedRegex(@"\n{3,}", RegexOptions.CultureInvariant)]
        private static partial Regex ExtraBlankLines();

        [GeneratedRegex(
            @"\b(?:the )?last 7 days\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex LastSevenDays();

        [GeneratedRegex(
            @"\b(credit\w*|plan\w*)\b[^.]{0,80}\b(?:in|over|during) the last \d+ days",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex CreditPeriodClause();
    }
}
