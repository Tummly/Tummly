namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared Guests list/export filter option validation and normalization.
    /// </summary>
    public static class GuestsFilterOptions
    {
        public static readonly HashSet<string> Marketing =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "eligible",
                "not-opted-in",
            };

        public static readonly HashSet<string> Contact =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "email",
                "mobile",
            };

        public static readonly HashSet<string> Sentiment =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "positive",
                "neutral",
                "negative",
            };

        public static void Validate(
            IReadOnlyList<string> marketing,
            IReadOnlyList<string> contact,
            IReadOnlyList<string> sentiment
        )
        {
            ValidateOptions(marketing, Marketing, "marketing");
            ValidateOptions(contact, Contact, "contact");
            ValidateOptions(sentiment, Sentiment, "sentiment");
        }

        public static List<string> Normalize(
            IReadOnlyList<string> values,
            HashSet<string> allowed
        )
        {
            return values
                .Select(value => NormalizeOption(value, allowed))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void ValidateOptions(
            IReadOnlyList<string> values,
            HashSet<string> allowed,
            string fieldName
        )
        {
            foreach (var value in values)
            {
                if (!allowed.Contains(value))
                {
                    throw new ArgumentException($"Invalid {fieldName} value.");
                }
            }
        }

        private static string NormalizeOption(string value, HashSet<string> allowed)
        {
            return allowed.Single(option =>
                option.Equals(value, StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
