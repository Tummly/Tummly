using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Asks the Assistant must refuse before a Draft or a retrieve answer.
    /// </summary>
    public static partial class AssistantProhibitedAsk
    {
        public static string? RefusalBody(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return null;
            }

            if (lower.Contains("system prompt", StringComparison.Ordinal)
                || lower.Contains("ignore policy", StringComparison.Ordinal)
                || lower.Contains("ignore your rules", StringComparison.Ordinal))
            {
                return "I cannot reveal system instructions or ignore the product rules.";
            }

            if (lower.Contains("google review", StringComparison.Ordinal)
                && (
                    lower.Contains("discount", StringComparison.Ordinal)
                    || lower.Contains("positive", StringComparison.Ordinal)
                    || lower.Contains("offer", StringComparison.Ordinal)
                ))
            {
                return "I cannot offer a discount in exchange for a review.";
            }

            if (lower.Contains("merge", StringComparison.Ordinal)
                && lower.Contains("another restaurant", StringComparison.Ordinal))
            {
                return "Guest records stay in one restaurant. "
                    + "I cannot merge them, and I cannot describe another restaurant.";
            }

            if (lower.Contains("another restaurant", StringComparison.Ordinal))
            {
                return "I cannot show another restaurant's records.";
            }

            if (lower.Contains("delivery insert", StringComparison.Ordinal))
            {
                return "A Delivery Insert placement cannot be created from the Assistant.";
            }

            if (lower.Contains("starter kit", StringComparison.Ordinal)
                && (
                    lower.Contains("another", StringComparison.Ordinal)
                    || lower.Contains("changed owner", StringComparison.Ordinal)
                    || lower.Contains("new owner", StringComparison.Ordinal)
                ))
            {
                return "A second free Starter Kit waits until a new owner relationship is verified. Guest data is not copied.";
            }

            return null;
        }

        public static string? CreditPurchaseBody(string message)
        {
            var match = BuySmsCreditsRegex().Match(message.Trim());
            if (!match.Success
                || !int.TryParse(match.Groups["qty"].Value, out var quantity))
            {
                return null;
            }

            var pack = CreditTopUpPricebook.FindPack("sms", quantity);
            if (pack is null)
            {
                return $"A pack of {quantity} SMS credits is not in the price book. Nothing was charged.";
            }

            var net = CreditTopUpPricebook.FormatPounds(pack.NetPounds);
            return $"{quantity} SMS credits cost {net} net. "
                + "Nothing is charged until you confirm the purchase in Billing.";
        }

        public static string? CompareAccessBody(
            string message,
            IEnumerable<string> ownedLocationNames
        )
        {
            var lower = message.Trim().ToLowerInvariant();
            var owned = ownedLocationNames.ToList();
            if (lower.Contains("compare all", StringComparison.Ordinal) && owned.Count < 2)
            {
                var only = owned.FirstOrDefault() ?? "this account";
                return $"This account has one Owned location: {only}. There is no second location to compare.";
            }

            if (!lower.Contains("compare", StringComparison.Ordinal))
            {
                return null;
            }

            string[] places = ["Camden", "Shoreditch", "Brixton"];
            var missing = places
                .Where(place =>
                    lower.Contains(place, StringComparison.OrdinalIgnoreCase)
                    && !owned.Any(name =>
                        name.Contains(place, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (missing.Count == 0)
            {
                return null;
            }

            return $"Your access does not include {string.Join(", ", missing)}.";
        }

        /// <summary>
        /// "Camden only" when that place is not an Owned location.
        /// Channel phrases such as "SMS only" stay a Draft edit.
        /// </summary>
        public static string? OnlyPlaceBody(
            string message,
            string currentLocationName,
            IEnumerable<string> ownedLocationNames
        )
        {
            var match = OnlyPlaceRegex().Match(message.Trim());
            if (!match.Success)
            {
                return null;
            }

            var place = match.Groups["place"].Value.Trim();
            if (place.Length == 0
                || place.Equals("sms", StringComparison.OrdinalIgnoreCase)
                || place.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (place.Equals(currentLocationName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (ownedLocationNames.Any(name =>
                    name.Contains(place, StringComparison.OrdinalIgnoreCase)
                    || place.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return $"Your access does not include {place}.";
        }

        /// <summary>
        /// "Show me Shoreditch Feedback" when that place is not an Owned location.
        /// </summary>
        public static string? UnownedPlaceBody(
            string message,
            string currentLocationName,
            IEnumerable<string> ownedLocationNames
        )
        {
            var match = ShowPlaceFeedbackRegex().Match(message.Trim());
            if (!match.Success)
            {
                return null;
            }

            var asked = match.Groups["place"].Value.Trim();
            if (asked.Length == 0)
            {
                return null;
            }

            if (asked.Equals(currentLocationName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (ownedLocationNames.Any(name =>
                    name.Equals(asked, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return $"Your access does not include {asked}. "
                + "I did not load Feedback from that place.";
        }

        [System.Text.RegularExpressions.GeneratedRegex(
            @"^(?<place>[A-Za-z][A-Za-z' -]{1,40}) only\.?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        )]
        private static partial System.Text.RegularExpressions.Regex OnlyPlaceRegex();

        [System.Text.RegularExpressions.GeneratedRegex(
            @"\bshow me (?<place>[A-Za-z][A-Za-z' -]{1,40}?) feedback\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        )]
        private static partial System.Text.RegularExpressions.Regex ShowPlaceFeedbackRegex();

        [System.Text.RegularExpressions.GeneratedRegex(
            @"\bbuy\s+(?<qty>\d+)\s+sms\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        )]
        private static partial System.Text.RegularExpressions.Regex BuySmsCreditsRegex();
    }
}
