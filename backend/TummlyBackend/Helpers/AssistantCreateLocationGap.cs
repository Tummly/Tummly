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
    /// Offer path). Bind only when the ask names a full Owned location name.
    /// Unnamed create uses Analysis scope. Do not invent venue refusals from
    /// purpose language after <c>for</c> / <c>at</c>.
    /// </summary>
    public static partial class AssistantCreateLocationGap
    {
        public const string KindAmbiguous = "ambiguous";
        public const string KindConflict = "conflict";
        public const string KindTwoNamed = "two-named";
        public const string KindAll = "all";

        public static AssistantLocationGapOutcome Resolve(
            string userMessage,
            int? analysisScopeLocationId,
            string analysisScopeLocationName,
            IReadOnlyList<AssistantGapLocation> ownedLocations,
            bool uniqueNameIsChoice = false,
            string draftNoun = "Campaign Draft"
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
                    AllLocationsBody(draftNoun)
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
                        : TwoNamedBody(names, draftNoun)
                );
            }

            if (matches.Count == 1)
            {
                var named = matches[0];
                if (
                    uniqueNameIsChoice
                    || analysisScopeLocationId is not int savedId
                    || named.Id == savedId
                )
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
                    ConflictBody(analysisScopeLocationName, named.Name, draftNoun)
                );
            }

            if (analysisScopeLocationId is null)
            {
                return new AssistantLocationGapOutcome.Gap(
                    KindAll,
                    [],
                    AllLocationsBody(draftNoun)
                );
            }

            return new AssistantLocationGapOutcome.Unnamed();
        }

        private static string AmbiguousBody(
            string userMessage,
            IReadOnlyList<string> names
        )
        {
            var token = FirstMatchingToken(userMessage, names) ?? names[0];
            return $"More than one venue matches {token}. Which venue: {Join(names)}?";
        }

        private static string ConflictBody(
            string analysisScopeName,
            string namedName,
            string draftNoun = "Campaign Draft"
        )
            => $"Analysis scope is {analysisScopeName}. This {draftNoun} names {namedName}. Which venue should I use: {Join([analysisScopeName, namedName])}?";

        private static string TwoNamedBody(
            IReadOnlyList<string> names,
            string draftNoun = "Campaign Draft"
        )
            => $"This {draftNoun} names {Join(names)}. Which venue should I use: {Join(names)}?";

        public static string AllLocationsBody(string draftNoun)
            => AssistantGapAsk.ForLocation(draftNoun);

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

        public static string RepeatBody(
            string? locationKind,
            IReadOnlyList<string> options,
            string draftNoun = "Campaign Draft"
        )
            => locationKind switch
            {
                KindAmbiguous => AmbiguousRepeat(options),
                KindConflict when options.Count >= 2 => ConflictBody(
                    options[0],
                    options[1],
                    draftNoun
                ),
                KindTwoNamed => TwoNamedBody(options, draftNoun),
                _ => AllLocationsBody(draftNoun),
            };

        private static string AmbiguousRepeat(IReadOnlyList<string> options)
        {
            var token = options.Count == 0 ? "that name" : options[0];
            return $"More than one venue matches {token}. Which venue: {Join(options)}?";
        }

        /// <summary>
        /// Match only full Owned location names in the ask. No free-text
        /// <c>at</c>/<c>for</c> cue parse and no substring token expand.
        /// </summary>
        private static List<AssistantGapLocation> FindNamedMatches(
            string text,
            IReadOnlyList<AssistantGapLocation> ownedLocations
        )
        {
            var matches = new List<AssistantGapLocation>();
            foreach (var location in ownedLocations)
            {
                if (location.Name.Length > 0 && ContainsName(text, location.Name))
                {
                    if (matches.TrueForAll(existing => existing.Id != location.Id))
                    {
                        matches.Add(location);
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

        [GeneratedRegex(
            @"\b(?<!compare\s)(?:all locations|every location|all my locations|all of my locations|all owned locations|all venues|every venue|everywhere)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex AllLocationRegex();
    }
}
