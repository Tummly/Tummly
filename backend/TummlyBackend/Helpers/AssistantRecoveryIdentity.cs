using System.Globalization;
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

        public static Match Resolve(
            string userMessage,
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows
        )
        {
            var named = rows
                .Where(row => MentionsRow(userMessage, row))
                .ToList();
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
            return
                "Which Feedback should I recover: "
                + AssistantCreateLocationGap.Join(labels)
                + "?";
        }

        public static string RepeatGapBody(IReadOnlyList<string> options)
            => "Which Feedback should I recover: "
                + AssistantCreateLocationGap.Join(options)
                + "? Reply with one guest Name and date.";

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
    }
}
