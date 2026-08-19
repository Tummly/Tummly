using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    public readonly record struct AssistantGapLocation(
        int Id,
        string Name
    );

    public abstract record AssistantLocationGapOutcome
    {
        public sealed record Unique(int LocationId, string LocationName)
            : AssistantLocationGapOutcome;

        public sealed record Unnamed : AssistantLocationGapOutcome;

        public sealed record Gap(
            string Kind,
            IReadOnlyList<string> Options,
            string Body
        ) : AssistantLocationGapOutcome;

        public sealed record Refusal(string Body) : AssistantLocationGapOutcome;
    }

    /// <summary>
    /// Server-owned Location uniqueness for Create Campaign Draft (and later
    /// Offer path). Unnamed create uses Analysis scope. Bind by unique
    /// Owned location name.
    /// </summary>
    public static partial class AssistantCreateLocationGap
    {
        public const string KindAmbiguous = "ambiguous";
        public const string KindConflict = "conflict";
        public const string KindTwoNamed = "two-named";
        public const string KindAll = "all";

        public static AssistantLocationGapOutcome Resolve(
            string userMessage,
            int analysisScopeLocationId,
            string analysisScopeLocationName,
            IReadOnlyList<AssistantGapLocation> ownedLocations,
            bool uniqueNameIsChoice = false
        )
        {
            var text = userMessage.Trim();
            if (text.Length == 0)
            {
                return new AssistantLocationGapOutcome.Unnamed();
            }

            var lower = text.ToLowerInvariant();
            if (LooksLikeAllLocations(lower))
            {
                return new AssistantLocationGapOutcome.Gap(
                    KindAll,
                    [],
                    "Which Owned location should this Campaign Draft use? Name one."
                );
            }

            var matches = CollapseSubstringMatches(
                FindNamedMatches(text, ownedLocations),
                text
            );
            if (matches.Count >= 2)
            {
                var names = matches
                    .Select(location => location.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var explicitNames = matches
                    .Where(location => ContainsName(text, location.Name))
                    .Select(location => location.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var kind = explicitNames.Count >= 2
                    ? KindTwoNamed
                    : KindAmbiguous;
                return new AssistantLocationGapOutcome.Gap(
                    kind,
                    names,
                    kind == KindAmbiguous
                        ? AmbiguousBody(text, names)
                        : TwoNamedBody(names)
                );
            }

            if (matches.Count == 1)
            {
                var named = matches[0];
                if (uniqueNameIsChoice || named.Id == analysisScopeLocationId)
                {
                    return new AssistantLocationGapOutcome.Unique(named.Id, named.Name);
                }

                var options = DistinctNames(
                    analysisScopeLocationName,
                    named.Name
                );
                return new AssistantLocationGapOutcome.Gap(
                    KindConflict,
                    options,
                    ConflictBody(analysisScopeLocationName, named.Name)
                );
            }

            if (TryUnknownLocationName(text, ownedLocations, out var unknownName))
            {
                return new AssistantLocationGapOutcome.Refusal(
                    $"{unknownName} is not an Owned location."
                );
            }

            return new AssistantLocationGapOutcome.Unnamed();
        }

        public static string AmbiguousBody(
            string userMessage,
            IReadOnlyList<string> names
        )
        {
            var token = FirstMatchingToken(userMessage, names) ?? names[0];
            return $"More than one Owned location matches {token}. Which one: {Join(names)}?";
        }

        public static string ConflictBody(string analysisScopeName, string namedName)
            => $"Analysis scope is {analysisScopeName}. This Campaign Draft names {namedName}. Which Owned location should I use: {Join([analysisScopeName, namedName])}?";

        public static string TwoNamedBody(IReadOnlyList<string> names)
            => $"This Campaign Draft names {Join(names)}. Which Owned location should I use: {Join(names)}?";

        public static string Join(IReadOnlyList<string> names)
        {
            if (names.Count <= 1)
            {
                return names.Count == 0 ? string.Empty : names[0];
            }

            if (names.Count == 2)
            {
                return $"{names[0]}, {names[1]}";
            }

            return string.Join(", ", names);
        }

        private static List<AssistantGapLocation> FindNamedMatches(
            string text,
            IReadOnlyList<AssistantGapLocation> ownedLocations
        )
        {
            var matches = new List<AssistantGapLocation>();
            void Add(AssistantGapLocation location)
            {
                if (matches.TrueForAll(existing => existing.Id != location.Id))
                {
                    matches.Add(location);
                }
            }

            foreach (var location in ownedLocations)
            {
                if (location.Name.Length > 0 && ContainsName(text, location.Name))
                {
                    Add(location);
                }
            }

            foreach (Match match in LocationCueRegex().Matches(text))
            {
                var token = match.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ':', '?', '!');
                if (token.Length == 0 || IsLocationStopPhrase(token))
                {
                    continue;
                }

                var exact = ownedLocations
                    .Where(location =>
                        location.Name.Equals(token, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var location in exact)
                {
                    Add(location);
                }

                foreach (var location in ownedLocations)
                {
                    if (location.Name.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        Add(location);
                    }
                }
            }

            return matches;
        }

        private static List<AssistantGapLocation> CollapseSubstringMatches(
            List<AssistantGapLocation> matches,
            string text
        )
        {
            if (matches.Count < 2)
            {
                return matches;
            }

            return matches
                .Where(candidate =>
                    matches.TrueForAll(other =>
                    {
                        if (other.Id == candidate.Id
                            || other.Name.Length <= candidate.Name.Length
                            || !other.Name.Contains(
                                candidate.Name,
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            return true;
                        }

                        return !ContainsName(text, other.Name);
                    }))
                .ToList();
        }

        private static List<string> DistinctNames(params string[] names)
            => names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static string? FirstMatchingToken(
            string userMessage,
            IReadOnlyList<string> names
        )
        {
            foreach (var name in names.OrderByDescending(item => item.Length))
            {
                if (ContainsName(userMessage, name))
                {
                    return name;
                }
            }

            return null;
        }

        private static bool ContainsName(string text, string name)
        {
            var index = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var after = index + name.Length;
            if (index > 0 && char.IsLetterOrDigit(text[index - 1]))
            {
                return false;
            }

            if (after < text.Length && char.IsLetterOrDigit(text[after]))
            {
                return false;
            }

            return true;
        }

        private static bool LooksLikeAllLocations(string lower)
            => AllLocationRegex().IsMatch(lower);

        private static bool TryUnknownLocationName(
            string text,
            IReadOnlyList<AssistantGapLocation> ownedLocations,
            out string unknownName
        )
        {
            unknownName = string.Empty;
            foreach (Match match in LocationCueRegex().Matches(text))
            {
                var phrase = match.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ':', '?', '!');
                if (phrase.Length == 0 || IsLocationStopPhrase(phrase))
                {
                    continue;
                }

                if (ownedLocations.Any(location => ContainsName(phrase, location.Name)
                    || ContainsName(location.Name, phrase)))
                {
                    continue;
                }

                unknownName = phrase;
                return true;
            }

            return false;
        }

        private static bool IsLocationStopPhrase(string phrase)
        {
            var lower = phrase.Trim().ToLowerInvariant();
            return StopPhrases.Contains(lower)
                || lower.StartsWith("all ", StringComparison.Ordinal)
                || lower.StartsWith("every ", StringComparison.Ordinal)
                || lower.Contains("guest", StringComparison.Ordinal)
                || lower.Contains("email", StringComparison.Ordinal)
                || lower.Contains("eligible", StringComparison.Ordinal)
                || lower.Contains("campaign", StringComparison.Ordinal)
                || lower.Contains("offer", StringComparison.Ordinal);
        }

        private static readonly HashSet<string> StopPhrases = new(StringComparer.OrdinalIgnoreCase)
        {
            "the",
            "this",
            "that",
            "these",
            "those",
            "my",
            "our",
            "a",
            "an",
            "least",
            "this location",
            "that location",
            "this venue",
        };

        [GeneratedRegex(
            @"\b(?<!compare\s)(?:all locations|every location|all my locations|all of my locations|all owned locations|all venues|every venue|everywhere)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex AllLocationRegex();

        [GeneratedRegex(
            @"\b(?:at|for)\s+(?:the\s+)?([A-Za-z][A-Za-z'’\-]*(?:\s+[A-Za-z][A-Za-z'’\-]*){0,3})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex LocationCueRegex();
    }
}
