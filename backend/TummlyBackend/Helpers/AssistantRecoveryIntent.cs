using System.Text.RegularExpressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Bind Recovery path intent, purpose, and tone from the ask.
    /// Defaults: Respond to the guest, apologise_and_confirm_follow_up,
    /// warm_and_apologetic, empty include-notes.
    /// </summary>
    public static class AssistantRecoveryIntent
    {
        public const string DefaultPurpose = "apologise_and_confirm_follow_up";
        public const string DefaultTone = "warm_and_apologetic";
        public const string OfferPurpose = "include_a_recovery_offer";

        private static readonly (string Id, string[] Needles)[] Purposes =
        [
            ("acknowledge_feedback", ["acknowledge"]),
            (
                "apologise_and_confirm_follow_up",
                ["say sorry", "apologize", "apologise", "sorry"]
            ),
            ("ask_for_more_information", ["more information", "ask for more"]),
            ("confirm_operational_action", ["confirm an operational", "operational action"]),
            ("create_custom_response", ["custom response"]),
        ];

        private static readonly (string Id, string[] Needles)[] Tones =
        [
            ("warm_and_apologetic", ["warm and apologetic", "warm"]),
            ("direct_and_practical", ["direct and practical", "direct"]),
            ("appreciative", ["appreciative"]),
            ("use_restaurant_tone", ["restaurant tone"]),
        ];

        private static readonly (string Id, string[] Needles)[] Categories =
        [
            ("team_briefed", ["team briefed"]),
            (
                "order_or_service_process_reviewed",
                ["order or service process reviewed", "service process reviewed"]
            ),
            ("delivery_issue_investigated", ["delivery issue investigated"]),
            ("product_quality_checked", ["product quality checked"]),
            ("cleaning_issue_addressed", ["cleaning issue addressed"]),
            ("staff_follow_up_completed", ["staff follow-up completed"]),
            ("other_action", ["other action"]),
        ];

        private static readonly Regex IncludeNotesRegex = new(
            @"include[- ]notes\s*:\s*(?<notes>[^\n;]+)",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        public static string Bind(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (LooksLikeInternalOnly(lower))
            {
                return AssistantRecoveryEligibility.IntentInternalOnly;
            }

            if (LooksLikeRespondAndRecord(lower))
            {
                return AssistantRecoveryEligibility.IntentRespondAndRecord;
            }

            if (LooksLikeRecoveryOffer(lower))
            {
                return AssistantRecoveryEligibility.IntentRecoveryOffer;
            }

            return AssistantRecoveryEligibility.IntentRespondToGuest;
        }

        public static string BindPurpose(string userMessage, string intent)
        {
            if (intent == AssistantRecoveryEligibility.IntentRecoveryOffer)
            {
                return OfferPurpose;
            }

            var lower = userMessage.Trim().ToLowerInvariant();
            foreach (var (id, needles) in Purposes)
            {
                if (ContainsAny(lower, needles))
                {
                    return id;
                }
            }

            return DefaultPurpose;
        }

        public static string BindTone(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            foreach (var (id, needles) in Tones)
            {
                if (ContainsAny(lower, needles))
                {
                    return id;
                }
            }

            return DefaultTone;
        }

        public static bool LooksLikeRecoveryAsk(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (LooksLikeInternalOnly(lower) || LooksLikeRespondAndRecord(lower))
            {
                return true;
            }

            var directResponseAsk =
                lower.Contains("respond to the guest", StringComparison.Ordinal)
                || lower.Contains("respond to these guests", StringComparison.Ordinal)
                || lower.Contains("respond to those guests", StringComparison.Ordinal)
                || lower.Contains("respond to feedback", StringComparison.Ordinal)
                || lower.Contains("reply to the guest", StringComparison.Ordinal)
                || lower.Contains("reply to these guests", StringComparison.Ordinal)
                || lower.Contains("reply to those guests", StringComparison.Ordinal)
                || lower.Contains("reply to feedback", StringComparison.Ordinal);
            var recoverVerb = Regex.IsMatch(
                lower,
                @"\brecover\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            var recoveryish = lower.Contains("recovery", StringComparison.Ordinal)
                || recoverVerb
                || directResponseAsk
                || LooksLikeRespondAndRecord(lower)
                || lower.Contains("internal action", StringComparison.Ordinal)
                || LooksLikeRecoveryOffer(lower);
            var draftish = lower.Contains("draft", StringComparison.Ordinal)
                || lower.Contains("review", StringComparison.Ordinal)
                || lower.Contains("prepare", StringComparison.Ordinal)
                || lower.Contains("create", StringComparison.Ordinal)
                || lower.Contains("start", StringComparison.Ordinal)
                || lower.Contains("help", StringComparison.Ordinal)
                || recoverVerb;
            return directResponseAsk || recoverVerb || (recoveryish && draftish);
        }

        public static bool LooksLikeInternalOnly(string lower)
            => ContainsAny(
                lower,
                "internal only",
                "record an internal action only",
                "do not contact",
                "don't contact",
                "no guest reply",
                "just record"
            );

        public static bool LooksLikeRecoveryOffer(string lower)
            => ContainsAny(
                lower,
                "recovery offer",
                "send an offer",
                "give them an offer",
                "offer as recovery",
                "compensate them",
                "voucher"
            );

        public static bool LooksLikeRespondAndRecord(string lower)
            => ContainsAny(
                lower,
                "respond and record",
                "reply and record",
                "respond and log",
                "reply and log"
            );

        public static string BindIncludeNotes(string userMessage)
        {
            var match = IncludeNotesRegex.Match(userMessage);
            if (!match.Success)
            {
                return "";
            }

            return match.Groups["notes"].Value.Trim();
        }

        public static int? BindOfferId(
            string userMessage,
            IReadOnlyList<AssistantOfferCatalogRow> catalog
        )
        {
            var named = catalog
                .Where(row =>
                    string.Equals(
                        row.Status,
                        CatalogOfferStatus.Active,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && !string.IsNullOrWhiteSpace(row.Title)
                    && userMessage.Contains(
                        row.Title,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();
            return named.Count == 1 ? named[0].Id : null;
        }

        public static (string? Category, string? Note) BindInternalFields(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            string? category = null;
            foreach (var (id, needles) in Categories)
            {
                if (ContainsAny(lower, needles))
                {
                    category = id;
                    break;
                }
            }

            var note = BindNote(userMessage);
            return (category, note);
        }

        private static string? BindNote(string userMessage)
        {
            var marker = "note:";
            var index = userMessage.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return null;
            }

            var note = userMessage[(index + marker.Length)..].Trim();
            return string.IsNullOrWhiteSpace(note) ? null : note;
        }

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
