using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Spoken offer quantities — word numbers and common currency words.
    /// Longest phrases first so "twenty five" wins over "twenty".
    /// </summary>
    public static class AssistantSpokenOfferQuantity
    {
        private static readonly (string Word, decimal Value)[] Table =
        [
            ("one hundred", 100m),
            ("a hundred", 100m),
            ("seventy-five", 75m),
            ("seventy five", 75m),
            ("twenty-five", 25m),
            ("twenty five", 25m),
            ("fifteen", 15m),
            ("fourteen", 14m),
            ("thirteen", 13m),
            ("twelve", 12m),
            ("eleven", 11m),
            ("hundred", 100m),
            ("ninety", 90m),
            ("eighty", 80m),
            ("seventy", 70m),
            ("sixty", 60m),
            ("fifty", 50m),
            ("forty", 40m),
            ("thirty", 30m),
            ("twenty", 20m),
            ("nineteen", 19m),
            ("eighteen", 18m),
            ("seventeen", 17m),
            ("sixteen", 16m),
            ("ten", 10m),
            ("nine", 9m),
            ("eight", 8m),
            ("seven", 7m),
            ("six", 6m),
            ("five", 5m),
            ("four", 4m),
            ("three", 3m),
            ("two", 2m),
            ("one", 1m),
        ];

        public static bool TryMatchPercent(string lower, out decimal value)
        {
            foreach (var (word, amount) in Table)
            {
                if (MatchesUnit(lower, word, PercentUnitPattern()))
                {
                    value = amount;
                    return true;
                }
            }

            value = 0m;
            return false;
        }

        public static bool TryMatchMoney(string lower, out decimal value)
        {
            foreach (var (word, amount) in Table)
            {
                if (MatchesUnit(lower, word, MoneyUnitPattern()))
                {
                    value = amount;
                    return true;
                }
            }

            value = 0m;
            return false;
        }

        private static bool MatchesUnit(string lower, string word, string unitPattern)
        {
            var pattern =
                $@"\b{Regex.Escape(word)}\s*{unitPattern}";
            return Regex.IsMatch(
                lower,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
        }

        private static string PercentUnitPattern()
            => @"(?:%|percent(?:age)?s?\b)";

        private static string MoneyUnitPattern()
            => @"(?:pounds?|gbp|quid|dollars?)\b";
    }
}
