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
            if (TryPickByOptionNumber(userMessage, rows, out var numberedEarly))
            {
                return new Match.One(numberedEarly);
            }

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
            else if (named.Count == 0 && LooksLikeBareGuestNameReply(userMessage))
            {
                // Gap reply is often just "Mia Khan" — not "for Mia Khan".
                named = rows
                    .Where(row => MentionsWho(row, userMessage.Trim()))
                    .ToList();
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
                return new Match.One(Newest(pool));
            }

            if (TryPickByOptionNumber(userMessage, pool, out var numbered))
            {
                return new Match.One(numbered);
            }

            if (LooksLikeAll(userMessage))
            {
                if (pool.Count == 1)
                {
                    return new Match.One(pool[0]);
                }

                return new Match.Many(pool);
            }

            if (LooksLikeConfirm(userMessage) || LooksLikeProceedRecovery(userMessage))
            {
                // Natural "yes" / "make it" / "prepare recovery" must bind, not re-ask.
                return new Match.One(Newest(pool));
            }

            if (pool.Count == 1)
            {
                return new Match.One(pool[0]);
            }

            return new Match.Many(pool);
        }

        /// <summary>
        /// Narrow evidence to the Feedback rows offered in the open Gap.
        /// Confirm / "both" must not re-expand to the whole retrieve set.
        /// </summary>
        public static IReadOnlyList<AssistantFeedbackEvidenceRow> RowsForGapOptions(
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows,
            IReadOnlyList<string> options,
            bool includeVenue = false
        )
        {
            if (rows.Count == 0 || options.Count == 0)
            {
                return rows;
            }

            var optionSet = new HashSet<string>(
                options.Where(option => !string.IsNullOrWhiteSpace(option)),
                StringComparer.OrdinalIgnoreCase
            );
            if (optionSet.Count == 0)
            {
                return rows;
            }

            var labels = ChoiceLabels(rows, includeVenue);
            var matched = new List<AssistantFeedbackEvidenceRow>();
            for (var i = 0; i < rows.Count; i++)
            {
                if (OptionMatchesLabel(optionSet, labels[i]))
                {
                    matched.Add(rows[i]);
                }
            }

            return matched.Count > 0 ? matched : rows;
        }

        private static bool OptionMatchesLabel(
            HashSet<string> options,
            string label
        )
        {
            if (options.Contains(label))
            {
                return true;
            }

            // Gap options may predate #N disambiguators (or Distinct strips them).
            var stripped = OptionNumberSuffix.Replace(label, string.Empty).Trim();
            if (stripped.Length > 0 && options.Contains(stripped))
            {
                return true;
            }

            foreach (var option in options)
            {
                var optionStripped = OptionNumberSuffix.Replace(option, string.Empty).Trim();
                if (optionStripped.Length > 0
                    && (
                        string.Equals(optionStripped, stripped, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(optionStripped, label, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(option, stripped, StringComparison.OrdinalIgnoreCase)
                    ))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Labels the operator can tell apart. Stamp is day + month + HH:mm
        /// (no year, no seconds). Colliding stamps get a short excerpt.
        /// Never append FDB- codes — operators pick by guest, time, or quote.
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

            var withExcerpt = rows
                .Select((row, index) =>
                    AppendDetail(plain[index], ExcerptDetail(row.Excerpt))
                )
                .ToList();
            if (AllUnique(withExcerpt))
            {
                return withExcerpt;
            }

            // Seed dupes share stamp + excerpt — number them (no FDB-).
            return withExcerpt
                .Select((label, index) => AppendDetail(label, $"#{index + 1}"))
                .ToList();
        }

        public static string FormatLabel(
            AssistantFeedbackEvidenceRow row,
            bool includeVenue = false
        )
        {
            var stamp = row.CreatedAt.ToString("d MMM HH:mm", CultureInfo.InvariantCulture);
            var label = $"{row.GuestName} ({stamp})";
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
                return "I can recover one Feedback at a time.\n\n" + question;
            }

            if (LooksLikeConfirm(userMessage) && rows.Count > 1)
            {
                return "I still need one Feedback.\n\n" + question;
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
                || userMessage.Contains(venueLabel, StringComparison.OrdinalIgnoreCase)
                || (
                    !string.IsNullOrWhiteSpace(row.FeedbackReference)
                    && userMessage.Contains(
                        row.FeedbackReference,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
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

        /// <summary>
        /// Short operator reply that names a guest (Gap picker), not a new ask.
        /// </summary>
        private static bool LooksLikeBareGuestNameReply(string userMessage)
        {
            var trimmed = userMessage.Trim().Trim('.', '!', '?');
            if (trimmed.Length < 2 || trimmed.Length > 80)
            {
                return false;
            }

            if (LooksLikeAll(trimmed)
                || LooksLikeConfirm(trimmed)
                || AsksLast(trimmed)
                || AsksNegative(trimmed)
                || Regex.IsMatch(trimmed, @"^#?\d+$", RegexOptions.CultureInvariant))
            {
                return false;
            }

            if (trimmed.Contains('\n', StringComparison.Ordinal))
            {
                return false;
            }

            // Multi-word create / recover prompts are not bare names.
            if (trimmed.Contains(" recover", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("prepare ", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("feedback", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("campaign", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains(" offer", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsGenericWho(string who)
        {
            var lower = Regex.Replace(who.Trim(), @"\s+", " ").ToLowerInvariant();
            if (lower is "this"
                or "the"
                or "a"
                or "an"
                or "both"
                or "all"
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
            => LooksLikeConfirm(userMessage) || LooksLikeProceedRecovery(userMessage);

        private static bool LooksLikeAll(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            return lower.Contains("all negative", StringComparison.Ordinal)
                || lower.Contains("all of them", StringComparison.Ordinal)
                || lower.Contains("all of these", StringComparison.Ordinal)
                || lower.Contains("all feedback", StringComparison.Ordinal)
                || lower.Contains("every negative", StringComparison.Ordinal)
                || lower.Contains("every feedback", StringComparison.Ordinal)
                || lower.Contains("for both", StringComparison.Ordinal)
                || lower.Contains("both of them", StringComparison.Ordinal)
                || lower == "both"
                || lower.StartsWith("all ", StringComparison.Ordinal);
        }

        private static bool LabelsAreIndistinct(
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows
        )
        {
            if (rows.Count <= 1)
            {
                return false;
            }

            // Compare guest + stamp + excerpt only (ignore #N disambiguators).
            var keys = rows
                .Select(row =>
                    $"{FormatLabel(row)}|{ExcerptDetail(row.Excerpt)}"
                )
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            return keys == 1;
        }

        private static AssistantFeedbackEvidenceRow Newest(
            IReadOnlyList<AssistantFeedbackEvidenceRow> rows
        )
            => rows
                .OrderByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .First();

        private static readonly Regex OptionNumberSuffix = new(
            @"\s*—\s*#\d+\s*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static readonly Regex OptionNumberInMessage = new(
            @"#\s*(?<n>\d+)\b|(?:^|\b)(?:number|option|item)\s+(?<n>\d+)\b|^(?<n>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

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
                or "yes please"
                or "make it"
                or "make one"
                or "recover"
                or "recover it"
                or "recover now"
                or "start recovery"
                or "prepare it"
                or "prepare recovery";
        }

        /// <summary>
        /// Natural-language proceed on an open Feedback Gap (no guest named).
        /// Does not match the initial bulk prepare ask (that must stay a Gap).
        /// </summary>
        private static bool LooksLikeProceedRecovery(string userMessage)
        {
            if (LooksLikeConfirm(userMessage))
            {
                return true;
            }

            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return false;
            }

            // CTA / first-turn wording — keep as Many when several rows match.
            if (lower.Contains("responses", StringComparison.Ordinal)
                || lower.Contains("feedback set", StringComparison.Ordinal)
                || lower.Contains("need attention", StringComparison.Ordinal)
                || lower.Contains("guests that", StringComparison.Ordinal)
                || Regex.IsMatch(
                    lower,
                    @"prepare\s+a\s+recovery\s+response",
                    RegexOptions.CultureInvariant
                )
                || Regex.IsMatch(
                    lower,
                    @"prepare\s+recovery\s+responses",
                    RegexOptions.CultureInvariant
                ))
            {
                return false;
            }

            return lower.Contains("make a recovery", StringComparison.Ordinal)
                || lower.Contains("make the recovery", StringComparison.Ordinal)
                || lower.Contains("prepare a recovery", StringComparison.Ordinal)
                || lower.Contains("prepare the recovery", StringComparison.Ordinal)
                || lower.Contains("start a recovery", StringComparison.Ordinal)
                || lower.Contains("start the recovery", StringComparison.Ordinal)
                || lower.Contains("do a recovery", StringComparison.Ordinal)
                || lower.Contains("do the recovery", StringComparison.Ordinal)
                || lower.Contains("go ahead and recover", StringComparison.Ordinal)
                || lower.Contains("just recover", StringComparison.Ordinal)
                || lower.Contains("please recover", StringComparison.Ordinal)
                || Regex.IsMatch(
                    lower,
                    @"^(please\s+)?(just\s+)?recover(\s+(it|one|now|please))?$",
                    RegexOptions.CultureInvariant
                );
        }

        private static bool TryPickByOptionNumber(
            string userMessage,
            IReadOnlyList<AssistantFeedbackEvidenceRow> pool,
            out AssistantFeedbackEvidenceRow picked
        )
        {
            picked = pool.Count > 0 ? pool[0] : null!;
            if (pool.Count == 0)
            {
                return false;
            }

            var match = OptionNumberInMessage.Match(userMessage.Trim());
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(
                    match.Groups["n"].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var number
                )
                || number < 1
                || number > pool.Count)
            {
                return false;
            }

            // ChoiceLabels numbers #1..#N in pool order when disambiguating.
            picked = pool[number - 1];
            return true;
        }

        private static bool AllUnique(IReadOnlyList<string> labels)
            => labels
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == labels.Count;

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
