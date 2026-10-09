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

            if (LooksLikeAll(userMessage))
            {
                if (pool.Count == 1)
                {
                    return new Match.One(pool[0]);
                }

                return new Match.Many(pool);
            }

            if (LooksLikeConfirm(userMessage))
            {
                if (pool.Count == 1)
                {
                    return new Match.One(pool[0]);
                }

                return new Match.Many(pool);
            }

            if (pool.Count == 1)
            {
                return new Match.One(pool[0]);
            }

            return new Match.Many(pool);
        }

        /// <summary>
        /// Labels the operator can tell apart. Same guest and same calendar
        /// day get a time, then a short excerpt, then the Feedback reference.
        /// </summary>
        public static IReadOnlyList<string> ChoiceLabels(
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows,
            bool includeVenue = false
        )
        {
            var plain = rows
                .Select(row => FormatLabel(row, includeVenue))
                .ToList();
            if (AllUnique(plain))
            {
                return plain;
            }

            var withTime = rows
                .Select(row => WithTime(FormatLabel(row, includeVenue), row.CreatedAt))
                .ToList();
            if (AllUnique(withTime))
            {
                return withTime;
            }

            var withExcerpt = rows
                .Select((row, index) =>
                    AppendDetail(withTime[index], ExcerptDetail(row.Excerpt))
                )
                .ToList();
            if (AllUnique(withExcerpt))
            {
                return withExcerpt;
            }

            return rows
                .Select((row, index) =>
                    AppendDetail(withExcerpt[index], row.FeedbackReference)
                )
                .ToList();
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
            return ReplyBody(userMessage: "", rows, includeVenue);
        }

        public static string ReplyBody(
            string userMessage,
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows,
            bool includeVenue = false
        )
        {
            var question = WhichFeedbackQuestion(ChoiceLabels(rows, includeVenue));
            if (LooksLikeAll(userMessage))
            {
                return "I can recover one Feedback at a time. " + question;
            }

            if (LooksLikeConfirm(userMessage) && rows.Count > 1)
            {
                return "I still need one Feedback. " + question;
            }

            return question;
        }

        public static string RepeatGapBody(IReadOnlyList<string> options)
        {
            var distinct = options
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return AssistantGapAsk.ExplainBind(
                AssistantGapTurn.KindFeedback,
                distinct
            );
        }

        private static string WhichFeedbackQuestion(IReadOnlyList<string> labels)
            => AssistantGapAsk.ForBind(AssistantGapTurn.KindFeedback, labels);

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

        public static bool AsksForAll(string userMessage)
            => LooksLikeAll(userMessage);

        public static bool IsShortConfirm(string userMessage)
            => LooksLikeConfirm(userMessage);

        private static bool LooksLikeAll(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            return lower.Contains("all negative", StringComparison.Ordinal)
                || lower.Contains("all of them", StringComparison.Ordinal)
                || lower.Contains("all of these", StringComparison.Ordinal)
                || lower.Contains("all feedback", StringComparison.Ordinal)
                || lower.Contains("every negative", StringComparison.Ordinal)
                || lower.Contains("every feedback", StringComparison.Ordinal)
                || lower.StartsWith("all ", StringComparison.Ordinal);
        }

        private static bool LooksLikeConfirm(string userMessage)
        {
            var normalized = userMessage
                .Trim()
                .Trim('.', '!', '?')
                .ToLowerInvariant();
            return normalized is "yes"
                or "y"
                or "yeah"
                or "yep"
                or "ok"
                or "okay"
                or "sure"
                or "that one"
                or "this one"
                or "the only one"
                or "that"
                or "go ahead"
                or "do it"
                or "please"
                or "yes please";
        }

        private static bool AllUnique(IReadOnlyList<string> labels)
            => labels
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == labels.Count;

        private static string WithTime(string label, DateTime createdAt)
        {
            var time = createdAt.ToString("HH:mm", CultureInfo.InvariantCulture);
            var close = label.LastIndexOf(')');
            if (close < 0)
            {
                return $"{label} {time}";
            }

            return label.Insert(close, " " + time);
        }

        private static string ExcerptDetail(string excerpt)
        {
            var text = excerpt.Trim();
            if (text.Length == 0)
            {
                return "";
            }

            if (text.Length > 42)
            {
                text = text[..42].TrimEnd() + "…";
            }

            return $"\"{text}\"";
        }

        private static string AppendDetail(string label, string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return label;
            }

            return $"{label} — {detail}";
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
