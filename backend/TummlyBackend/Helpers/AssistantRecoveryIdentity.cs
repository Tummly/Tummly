using System.Globalization;
using System.Text.RegularExpressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Bind Feedback identity for Recovery path. One match binds. Two or more
    /// is a Gap turn of colliding Name + date rows. Zero matches explain
    /// without a candidate dump.
    /// </summary>
    public static class AssistantRecoveryIdentity
    {
        public const string ReasonEmpty = "empty";
        public const string ReasonNoNegative = "no-negative";
        public const string ReasonNamedMiss = "named-miss";

        public abstract record Match
        {
            public sealed record One(AssistantFeedbackEvidenceRow Row) : Match;

            public sealed record None(string Reason) : Match;

            public sealed record Many(
                IReadOnlyList<AssistantFeedbackEvidenceRow> Rows
            ) : Match;
        }

        private static readonly Regex ForWhoRegex = new(
            @"\bfor\s+(?<who>[^.,;?!]+)",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        public static Match Resolve(
            string userMessage,
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows
        )
        {
            var named = rows
                .Where(row => MentionsRow(userMessage, row))
                .ToList();
            var namedGuest = false;
            if (named.Count == 0 && TrySpecificWho(userMessage, out var who))
            {
                named = rows.Where(row => MentionsWho(row, who)).ToList();
                namedGuest = true;
                if (named.Count == 0)
                {
                    return new Match.None(ReasonNamedMiss);
                }
            }

            var pool = named.Count > 0 ? named : rows.ToList();
            var asksNegative = AsksNegative(userMessage);
            if (asksNegative)
            {
                pool = pool.Where(IsNegative).ToList();
            }

            if (pool.Count == 0)
            {
                if (namedGuest && !asksNegative)
                {
                    return new Match.None(ReasonNamedMiss);
                }

                if (asksNegative && rows.Count > 0)
                {
                    return new Match.None(ReasonNoNegative);
                }

                return new Match.None(
                    rows.Count == 0 ? ReasonEmpty : ReasonNamedMiss
                );
            }

            if (AsksLast(userMessage))
            {
                var latest = pool
                    .OrderByDescending(row => row.CreatedAt)
                    .ThenByDescending(row => row.Id)
                    .First();
                return new Match.One(latest);
            }

            if (pool.Count == 1)
            {
                return new Match.One(pool[0]);
            }

            return new Match.Many(pool);
        }

        public static string FormatLabel(
            AssistantFeedbackEvidenceRow row,
            bool includeVenue = false
        )
        {
            var date = row.CreatedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            var label = $"{row.GuestName} ({date})";
            if (includeVenue && !string.IsNullOrWhiteSpace(row.LocationName))
            {
                return $"{label} · {row.LocationName}";
            }

            return label;
        }

        public static string GapBody(
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows,
            bool includeVenue = false
        )
        {
            var labels = rows
                .Select(row => FormatLabel(row, includeVenue))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return WhichFeedbackQuestion(labels);
        }

        public static string RepeatGapBody(IReadOnlyList<string> options)
            => WhichFeedbackQuestion(options)
                + " Reply with one guest Name and date.";

        private static string WhichFeedbackQuestion(IReadOnlyList<string> labels)
            => "Which Feedback should I recover: "
                + AssistantCreateLocationGap.Join(labels)
                + "?";

        private static bool MentionsRow(string userMessage, AssistantFeedbackEvidenceRow row)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return false;
            }

            var label = FormatLabel(row);
            var venueLabel = FormatLabel(row, includeVenue: true);
            return userMessage.Contains(row.GuestName, StringComparison.OrdinalIgnoreCase)
                || userMessage.Contains(label, StringComparison.OrdinalIgnoreCase)
                || userMessage.Contains(venueLabel, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySpecificWho(string userMessage, out string who)
        {
            who = "";
            var match = ForWhoRegex.Match(userMessage);
            if (!match.Success)
            {
                return false;
            }

            who = match.Groups["who"].Value.Trim();
            return who.Length > 0 && !IsGenericWho(who);
        }

        private static bool IsGenericWho(string who)
        {
            var lower = Regex.Replace(who.Trim(), @"\s+", " ").ToLowerInvariant();
            if (lower is "this"
                or "the"
                or "a"
                or "an"
                or "me"
                or "us"
                or "them"
                or "it"
                or "guest"
                or "guests"
                or "this guest"
                or "the guest"
                or "that guest"
                or "these guests"
                or "those guests"
                or "feedback"
                or "recovery")
            {
                return true;
            }

            if (lower.Contains("feedback", StringComparison.Ordinal)
                || lower.Contains("location", StringComparison.Ordinal)
                || lower.Contains("negative", StringComparison.Ordinal)
                || lower.Contains("recovery", StringComparison.Ordinal))
            {
                return true;
            }

            return lower.StartsWith("this ", StringComparison.Ordinal)
                || lower.StartsWith("the last", StringComparison.Ordinal)
                || lower.StartsWith("the latest", StringComparison.Ordinal)
                || lower.StartsWith("the guest", StringComparison.Ordinal)
                || lower.StartsWith("that guest", StringComparison.Ordinal)
                || lower.StartsWith("these ", StringComparison.Ordinal)
                || lower.StartsWith("those ", StringComparison.Ordinal)
                || lower.StartsWith("last ", StringComparison.Ordinal)
                || lower.StartsWith("latest ", StringComparison.Ordinal);
        }

        private static bool MentionsWho(AssistantFeedbackEvidenceRow row, string who)
        {
            if (row.GuestName.Contains(who, StringComparison.OrdinalIgnoreCase)
                || who.Contains(row.GuestName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var first = row.GuestName.Split(
                ' ',
                2,
                StringSplitOptions.RemoveEmptyEntries
            );
            if (first.Length == 0 || first[0].Length < 2)
            {
                return false;
            }

            return Regex.IsMatch(
                who,
                $@"\b{Regex.Escape(first[0])}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
        }

        private static bool AsksNegative(string userMessage)
            => userMessage.Contains("negative", StringComparison.OrdinalIgnoreCase);

        private static bool AsksLast(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            var lastish = lower.Contains("last ", StringComparison.Ordinal)
                || lower.Contains("latest", StringComparison.Ordinal)
                || lower.Contains("most recent", StringComparison.Ordinal)
                || lower.Contains("newest", StringComparison.Ordinal);
            if (!lastish)
            {
                return false;
            }

            return lower.Contains("feedback", StringComparison.Ordinal)
                || lower.Contains("negative", StringComparison.Ordinal)
                || lower.Contains("guest", StringComparison.Ordinal);
        }

        private static bool IsNegative(AssistantFeedbackEvidenceRow row)
            => string.Equals(row.Sentiment, "negative", StringComparison.OrdinalIgnoreCase);
    }
}
