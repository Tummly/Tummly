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
        public abstract record Match
        {
            public sealed record One(AssistantFeedbackEvidenceRow Row) : Match;

            public sealed record None : Match;

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
            if (named.Count == 0 && TrySpecificWho(userMessage, out var who))
            {
                named = rows.Where(row => MentionsWho(row, who)).ToList();
                if (named.Count == 0)
                {
                    return new Match.None();
                }
            }

            var pool = named.Count > 0 ? named : rows.ToList();
            if (pool.Count == 0)
            {
                return new Match.None();
            }

            if (pool.Count == 1)
            {
                return new Match.One(pool[0]);
            }

            if (named.Count == 0)
            {
                return new Match.Many(pool);
            }

            var uniqueByLabel = named
                .GroupBy(FormatLabel, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() == 1)
                .Select(group => group.First())
                .ToList();
            if (uniqueByLabel.Count == 1 && named.Count == 1)
            {
                return new Match.One(named[0]);
            }

            return new Match.Many(named);
        }

        public static string FormatLabel(AssistantFeedbackEvidenceRow row)
        {
            var date = row.CreatedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            return $"{row.GuestName} ({date})";
        }

        public static string GapBody(IReadOnlyList<AssistantFeedbackEvidenceRow> rows)
        {
            var labels = rows.Select(FormatLabel).Distinct(StringComparer.Ordinal).ToList();
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
            return userMessage.Contains(row.GuestName, StringComparison.OrdinalIgnoreCase)
                || userMessage.Contains(label, StringComparison.OrdinalIgnoreCase);
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

            return lower.StartsWith("this ", StringComparison.Ordinal)
                || lower.StartsWith("the guest", StringComparison.Ordinal)
                || lower.StartsWith("that guest", StringComparison.Ordinal)
                || lower.StartsWith("these ", StringComparison.Ordinal)
                || lower.StartsWith("those ", StringComparison.Ordinal);
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
    }
}
