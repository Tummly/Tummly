using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    public static class AssistantContactRedaction
    {
        private static readonly Regex EmailPattern = new(
            @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        public static string? RedactTitle(string? title, IReadOnlyList<string> tokens)
            => title is null ? null : RedactBody(title, tokens);

        public static string RedactBody(string body, IReadOnlyList<string> tokens)
        {
            var redacted = body;
            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                redacted = redacted.Replace(
                    token,
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase
                );
            }

            return EmailPattern.Replace(redacted, string.Empty);
        }
    }
}
