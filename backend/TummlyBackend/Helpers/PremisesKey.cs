namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Normalised premises identity for Location import duplicate warnings.
    /// Built from address, city and postcode only. The Location name is not
    /// part of the key: the same name at another address is allowed, and a
    /// different name at the same premises is a potential duplicate. Unit and
    /// flat numbers stay in the key, so distinct units at one postcode differ.
    /// </summary>
    public static class PremisesKey
    {
        /// <summary>Column width of <c>RestaurantLocation.PremisesKey</c>.</summary>
        public const int MaxStoredLength = 450;

        /// <summary>
        /// Key as persisted and compared in SQL. Cut to the column width, so a
        /// very long address still matches itself.
        /// </summary>
        public static string? ForStorage(string? key) =>
            key is { Length: > MaxStoredLength } ? key[..MaxStoredLength] : key;

        private static readonly IReadOnlyDictionary<string, string> TokenAliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["road"] = "rd",
                ["street"] = "st",
                ["saint"] = "st",
                ["avenue"] = "ave",
                ["av"] = "ave",
                ["lane"] = "ln",
                ["drive"] = "dr",
                ["court"] = "ct",
                ["place"] = "pl",
                ["square"] = "sq",
                ["terrace"] = "ter",
                ["apartment"] = "flat",
                ["apt"] = "flat",
            };

        /// <summary>
        /// Returns null when the address or postcode is blank, so callers skip
        /// the duplicate check instead of matching on an empty key.
        /// </summary>
        public static string? Build(string? address, string? city, string? postcode)
        {
            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(postcode))
            {
                return null;
            }

            var addressTokens = Tokenize(address).ToList();
            RemoveTrailing(addressTokens, Tokenize(postcode));
            RemoveTrailing(addressTokens, Tokenize(city));

            if (addressTokens.Count == 0)
            {
                return null;
            }

            return $"{UkPostcode.NormalizeForLookup(postcode)}|{string.Join(' ', addressTokens)}";
        }

        private static IReadOnlyList<string> Tokenize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            var lettersAndDigits = new string(
                value
                    .ToLowerInvariant()
                    .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
                    .ToArray()
            );

            return lettersAndDigits
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => TokenAliases.GetValueOrDefault(token, token))
                .ToList();
        }

        /// <summary>
        /// Drops a suffix such as the city or postcode when the address repeats
        /// it at the end, so "10 High St, Leeds" and "10 High St" match.
        /// </summary>
        private static void RemoveTrailing(List<string> tokens, IReadOnlyList<string> suffix)
        {
            if (suffix.Count == 0 || tokens.Count <= suffix.Count)
            {
                return;
            }

            var start = tokens.Count - suffix.Count;
            for (var index = 0; index < suffix.Count; index++)
            {
                if (tokens[start + index] != suffix[index])
                {
                    return;
                }
            }

            tokens.RemoveRange(start, suffix.Count);
        }
    }
}
