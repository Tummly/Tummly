using System.Text.Json;
using System.Text.RegularExpressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public readonly record struct AssistantOwnedLocationRef(
        int Id,
        string Name,
        string Address,
        CaptureLocationStatus CaptureStatus
    );

    public abstract record AssistantCompareOutcome
    {
        public sealed record NotCompare : AssistantCompareOutcome;

        public sealed record MentionCaveat(string MentionedLocationName)
            : AssistantCompareOutcome;

        public sealed record TwoPeriodCaveat : AssistantCompareOutcome;

        public sealed record SingleCaveat : AssistantCompareOutcome;

        public sealed record Clarify(string Body) : AssistantCompareOutcome;

        public sealed record Compare(
            IReadOnlyList<int> LocationIds,
            string? DroppedUnknownSentence
        ) : AssistantCompareOutcome;
    }

    public static class AssistantCompareTurn
    {
        public const int MaxLocations = 3;

        private static readonly JsonSerializerOptions JsonOptions = new();

        private static readonly string[] FollowUpPhrases =
        [
            "which one",
            "which of those",
            "which of them",
            "those locations",
            "these locations",
            "the same locations",
            "among those",
            "of those",
            "that comparison",
        ];

        private static readonly string[] ComparePhrases =
        [
            "compare",
            "versus",
            "contrast",
            "which is better",
            "which location",
            "rank ",
            "rank my",
            "side by side",
            "difference between",
        ];

        private static readonly string[] AllPhrases =
        [
            "all locations",
            "every location",
            "all my locations",
            "every location",
            "all of them",
            "compare all",
            "all owned locations",
        ];

        private static readonly string[] PeriodPhrases =
        [
            "last week",
            "last 7 days",
            "last seven days",
            "last month",
            "last 30 days",
            "last thirty days",
            "this month",
            "this week",
            "yesterday",
            "last7",
            "last30",
            "thismonth",
        ];

        private static readonly HashSet<string> StopTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "compare",
            "compared",
            "comparing",
            "contrast",
            "contrasted",
            "rank",
            "ranking",
            "versus",
            "vs",
            "to",
            "and",
            "or",
            "the",
            "my",
            "our",
            "a",
            "an",
            "please",
            "location",
            "locations",
            "owned",
            "feedback",
            "how",
            "is",
            "are",
            "doing",
            "between",
            "better",
            "which",
            "one",
            "of",
            "them",
            "those",
            "these",
            "here",
            "this",
            "that",
            "with",
            "for",
            "in",
            "over",
            "recent",
            "recently",
            "summarise",
            "summarize",
            "all",
            "every",
            "both",
        };

        public static AssistantCompareOutcome Resolve(
            string userMessage,
            int savedLocationId,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations,
            IReadOnlyList<int>? lastCompareLocationIds,
            bool isSingleMode
        )
        {
            var text = userMessage.Trim();
            if (text.Length == 0)
            {
                return new AssistantCompareOutcome.NotCompare();
            }

            var lower = text.ToLowerInvariant();
            var lastSet = DistinctOwnedIds(lastCompareLocationIds, ownedLocations);

            if (lastSet.Count >= 2 && IsFollowUpReuse(lower))
            {
                return new AssistantCompareOutcome.Compare(lastSet, null);
            }

            if (LooksLikeTwoPeriodAsk(lower))
            {
                return new AssistantCompareOutcome.TwoPeriodCaveat();
            }

            var isCompare = LooksLikeCompare(lower);
            if (!isCompare)
            {
                var mentioned = FindMentionedOtherLocation(
                    text,
                    savedLocationId,
                    ownedLocations
                );
                return mentioned is null
                    ? new AssistantCompareOutcome.NotCompare()
                    : new AssistantCompareOutcome.MentionCaveat(mentioned.Value.Name);
            }

            if (isSingleMode || ownedLocations.Count < 2)
            {
                return new AssistantCompareOutcome.SingleCaveat();
            }

            if (LooksLikeAllLocations(lower) || IsUnnamedCompare(lower, text, ownedLocations))
            {
                return new AssistantCompareOutcome.Clarify(
                    ClarifyUnnamedBody(ownedLocations)
                );
            }

            var includeBaseline = LooksLikeBaseline(lower);
            var tokens = ExtractCandidateTokens(text);
            var validIds = new List<int>();
            var unknownNames = new List<string>();

            foreach (var token in tokens)
            {
                if (IsBaselineToken(token))
                {
                    includeBaseline = true;
                    continue;
                }

                var match = MatchToken(token, ownedLocations);
                if (match.Kind == TokenMatchKind.Ambiguous)
                {
                    return new AssistantCompareOutcome.Clarify(
                        ClarifyAmbiguousBody(token, match.Matches)
                    );
                }

                if (match.Kind == TokenMatchKind.Unknown)
                {
                    unknownNames.Add(token.Trim());
                    continue;
                }

                if (match.Kind == TokenMatchKind.Found
                    && !validIds.Contains(match.Matches[0].Id))
                {
                    validIds.Add(match.Matches[0].Id);
                }
            }

            var dropped = unknownNames.Count == 0
                ? null
                : DroppedUnknownSentence(unknownNames);

            var explicitSet = validIds.Count >= 2;
            if (!explicitSet && includeBaseline)
            {
                AddSavedIfMissing(validIds, savedLocationId, ownedLocations);
            }

            if (!explicitSet && validIds.Count < 2)
            {
                AddSavedIfMissing(validIds, savedLocationId, ownedLocations);
            }

            if (validIds.Count > MaxLocations)
            {
                return new AssistantCompareOutcome.Clarify(
                    ClarifyCapBody(ownedLocations)
                );
            }

            if (validIds.Count < 2)
            {
                return new AssistantCompareOutcome.Clarify(
                    ClarifyTooFewBody(ownedLocations, dropped)
                );
            }

            return new AssistantCompareOutcome.Compare(validIds, dropped);
        }

        public static string? SerializeLocationIds(IReadOnlyList<int>? ids)
        {
            if (ids is null || ids.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(ids, JsonOptions);
        }

        public static IReadOnlyList<int> ParseLocationIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json, JsonOptions) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        public static string FormatLocationLabel(AssistantOwnedLocationRef location)
            => location.CaptureStatus == CaptureLocationStatus.Paused
                ? $"{location.Name} (Capture-Paused)"
                : location.Name;

        public static string FormatLocationList(
            IReadOnlyList<AssistantOwnedLocationRef> locations
        )
            => string.Join(", ", locations.Select(FormatLocationLabel));

        public static string SingleCaveatSentence(string savedLocationName)
            => $"There is no other Owned location to compare. This answer is for {savedLocationName} only.";

        public static string MentionCaveatSentence(
            string savedLocationName,
            string mentionedLocationName
        )
            => $"I used saved Analysis scope ({savedLocationName}). Naming {mentionedLocationName} is not a Compare turn. Ask to compare locations if you want both.";

        public static string TwoPeriodCaveatSentence(string periodPhrase)
            => $"I can compare locations in one Reporting period. This answer uses {periodPhrase}. Change Reporting period to use a different window.";

        public static string CapturePausedSentence(string locationName)
            => $"{locationName} is Capture-Paused.";

        public static string DroppedUnknownSentence(IReadOnlyList<string> names)
        {
            if (names.Count == 1)
            {
                return $"{names[0]} is not an Owned location, so I did not include it.";
            }

            return $"{string.Join(", ", names)} are not Owned locations, so I did not include them.";
        }

        public static string ClarifyUnnamedBody(
            IReadOnlyList<AssistantOwnedLocationRef> locations
        )
            => $"Which Owned locations should I compare? Name up to {MaxLocations}. Your locations: {FormatLocationList(locations)}.";

        public static string ClarifyCapBody(
            IReadOnlyList<AssistantOwnedLocationRef> locations
        )
            => $"I can compare up to {MaxLocations} Owned locations. Name which ones. Your locations: {FormatLocationList(locations)}.";

        public static string ClarifyTooFewBody(
            IReadOnlyList<AssistantOwnedLocationRef> locations,
            string? droppedUnknownSentence
        )
        {
            var body =
                $"Name at least two Owned locations to compare. Your locations: {FormatLocationList(locations)}.";
            return string.IsNullOrWhiteSpace(droppedUnknownSentence)
                ? body
                : $"{droppedUnknownSentence} {body}";
        }

        public static string ClarifyAmbiguousBody(
            string token,
            IReadOnlyList<AssistantOwnedLocationRef> matches
        )
            => $"More than one Owned location matches \"{token.Trim()}\". Which one: {FormatLocationList(matches)}?";

        private static bool IsFollowUpReuse(string lower)
        {
            foreach (var phrase in FollowUpPhrases)
            {
                if (lower.Contains(phrase, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeCompare(string lower)
        {
            if (LooksLikeAllLocations(lower))
            {
                return true;
            }

            if (Regex.IsMatch(lower, @"\bvs\b"))
            {
                return true;
            }

            foreach (var phrase in ComparePhrases)
            {
                if (lower.Contains(phrase, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeAllLocations(string lower)
        {
            foreach (var phrase in AllPhrases)
            {
                if (lower.Contains(phrase, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeBaseline(string lower)
            => lower.Contains("compare to", StringComparison.Ordinal)
                || lower.Contains("compared to", StringComparison.Ordinal)
                || Regex.IsMatch(lower, @"\bhere\b")
                || lower.Contains("this location", StringComparison.Ordinal);

        private static bool LooksLikeTwoPeriodAsk(string lower)
        {
            var hits = new List<string>();
            foreach (var phrase in PeriodPhrases)
            {
                if (lower.Contains(phrase, StringComparison.Ordinal)
                    && !hits.Contains(phrase, StringComparer.Ordinal))
                {
                    hits.Add(phrase);
                }
            }

            return hits.Count >= 2 && LooksLikeCompare(lower);
        }

        private static bool IsUnnamedCompare(
            string lower,
            string text,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            if (LooksLikeAllLocations(lower))
            {
                return true;
            }

            var tokens = ExtractCandidateTokens(text);
            foreach (var token in tokens)
            {
                if (IsBaselineToken(token))
                {
                    continue;
                }

                return false;
            }

            foreach (var location in ownedLocations)
            {
                if (ContainsName(text, location.Name))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBaselineToken(string token)
        {
            var t = token.Trim();
            return t.Equals("here", StringComparison.OrdinalIgnoreCase)
                || t.Equals("this location", StringComparison.OrdinalIgnoreCase)
                || t.Equals("this", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ExtractCandidateTokens(string text)
        {
            var stripped = Regex.Replace(
                text,
                @"\b(compare|compared|comparing|contrast|contrasted|rank|ranking|versus)\b",
                " ",
                RegexOptions.IgnoreCase
            );
            var parts = Regex.Split(
                stripped,
                @"\s*(?:,|/|\band\b|\bor\b|\bvs\b|\bversus\b|\bto\b)\s*",
                RegexOptions.IgnoreCase
            );
            var tokens = new List<string>();
            foreach (var part in parts)
            {
                var cleaned = Regex.Replace(part, @"[?!.]+$", "").Trim();
                if (cleaned.Length == 0)
                {
                    continue;
                }

                var words = cleaned
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(word => !StopTokens.Contains(word.Trim().Trim('?', '.', ',', '"', '\'')))
                    .Select(word => word.Trim().Trim('?', '.', ',', '"', '\''))
                    .Where(word => word.Length > 0)
                    .ToArray();
                if (words.Length == 0)
                {
                    continue;
                }

                tokens.Add(string.Join(' ', words));
            }

            return tokens;
        }

        private enum TokenMatchKind
        {
            Found,
            Ambiguous,
            Unknown,
        }

        private readonly record struct TokenMatch(
            TokenMatchKind Kind,
            IReadOnlyList<AssistantOwnedLocationRef> Matches
        );

        private static TokenMatch MatchToken(
            string token,
            IReadOnlyList<AssistantOwnedLocationRef> locations
        )
        {
            var t = token.Trim();
            if (t.Length == 0)
            {
                return new TokenMatch(TokenMatchKind.Unknown, []);
            }

            var exactName = locations
                .Where(location => location.Name.Equals(t, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactName.Count == 1)
            {
                return new TokenMatch(TokenMatchKind.Found, exactName);
            }

            if (exactName.Count > 1)
            {
                return new TokenMatch(TokenMatchKind.Ambiguous, exactName);
            }

            var substrName = locations
                .Where(location =>
                    location.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            if (substrName.Count == 1)
            {
                return new TokenMatch(TokenMatchKind.Found, substrName);
            }

            if (substrName.Count > 1)
            {
                return new TokenMatch(TokenMatchKind.Ambiguous, substrName);
            }

            var exactAddr = locations
                .Where(location =>
                    location.Address.Length > 0
                    && location.Address.Equals(t, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            if (exactAddr.Count == 1)
            {
                return new TokenMatch(TokenMatchKind.Found, exactAddr);
            }

            if (exactAddr.Count > 1)
            {
                return new TokenMatch(TokenMatchKind.Ambiguous, exactAddr);
            }

            var substrAddr = locations
                .Where(location =>
                    location.Address.Length > 0
                    && location.Address.Contains(t, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            if (substrAddr.Count == 1)
            {
                return new TokenMatch(TokenMatchKind.Found, substrAddr);
            }

            if (substrAddr.Count > 1)
            {
                return new TokenMatch(TokenMatchKind.Ambiguous, substrAddr);
            }

            return new TokenMatch(TokenMatchKind.Unknown, []);
        }

        private static AssistantOwnedLocationRef? FindMentionedOtherLocation(
            string text,
            int savedLocationId,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            foreach (var location in ownedLocations.OrderByDescending(item => item.Name.Length))
            {
                if (location.Id == savedLocationId)
                {
                    continue;
                }

                if (ContainsName(text, location.Name)
                    || (location.Address.Length > 0 && ContainsName(text, location.Address)))
                {
                    return location;
                }
            }

            return null;
        }

        private static bool ContainsName(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                return false;
            }

            return Regex.IsMatch(
                text,
                $@"\b{Regex.Escape(name)}\b",
                RegexOptions.IgnoreCase
            );
        }

        private static void AddSavedIfMissing(
            List<int> validIds,
            int savedLocationId,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            if (validIds.Contains(savedLocationId))
            {
                return;
            }

            if (ownedLocations.Any(location => location.Id == savedLocationId))
            {
                validIds.Insert(0, savedLocationId);
            }
        }

        private static List<int> DistinctOwnedIds(
            IReadOnlyList<int>? ids,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            if (ids is null || ids.Count == 0)
            {
                return [];
            }

            var owned = ownedLocations.Select(location => location.Id).ToHashSet();
            return ids.Where(owned.Contains).Distinct().ToList();
        }
    }
}
